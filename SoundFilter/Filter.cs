using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Dalamud.Hooking;

namespace SoundFilter {
    internal unsafe class Filter : IDisposable {
        private static class Signatures {
            internal const string PlaySpecificSound = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 33 F6 8B DA 48 8B F9 0F BA E2 0F";

            internal const string GetResourceSync = "E8 ?? ?? 00 00 48 8D 4F ?? 48 89 87 ?? ?? 00 00";
            internal const string GetResourceAsync = "E8 ?? ?? ?? 00 48 8B D8 EB ?? F0 FF 83 ?? ?? 00 00";
        }

        private const int ResourceDataPointerOffset = 0xA8;

        #region Delegates

        private delegate void* PlaySpecificSoundDelegate(long a1, int idx);

        private delegate void* GetResourceSyncPrototype(IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown);

        private delegate void* GetResourceAsyncPrototype(IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown, bool isUnknown);

        #endregion

        #region Hooks

        private Hook<PlaySpecificSoundDelegate>? PlaySpecificSoundHook { get; set; }

        private Hook<GetResourceSyncPrototype>? GetResourceSyncHook { get; set; }

        private Hook<GetResourceAsyncPrototype>? GetResourceAsyncHook { get; set; }

        #endregion

        private SoundFilterPlugin Plugin { get; }

        private Dictionary<IntPtr, string> Scds { get; } = new();
        private Dictionary<IntPtr, string> AsyncScds { get; } = new();

        internal ConcurrentQueue<string> Recent { get; } = new();

        private IntPtr NoSoundPtr { get; }
        private IntPtr InfoPtr { get; }

        internal Filter(SoundFilterPlugin plugin) {
            this.Plugin = plugin;

            var (noSoundPtr, infoPtr) = SetUpNoSound();
            this.NoSoundPtr = noSoundPtr;
            this.InfoPtr = infoPtr;
        }

        private static byte[] GetNoSoundScd() {
            var noSound = Resourcer.Resource.AsStream("Resources/gaya_nosound.scd");

            using var memoryStream = new MemoryStream();
            noSound.CopyTo(memoryStream);

            return memoryStream.ToArray();
        }

        private static (IntPtr noSoundPtr, IntPtr infoPtr) SetUpNoSound() {
            // get the data of an empty scd
            var noSound = GetNoSoundScd();

            // allocate unmanaged memory for this data and copy the data into the memory
            var noSoundPtr = Marshal.AllocHGlobal(noSound.Length);
            Marshal.Copy(noSound, 0, noSoundPtr, noSound.Length);

            // allocate some memory for feeding into the play sound function
            var infoPtr = Marshal.AllocHGlobal(256);
            // write a pointer to the empty scd
            Marshal.WriteIntPtr(infoPtr + 8, noSoundPtr);
            // specify where the game should offset from for the sound index
            Marshal.WriteInt32(infoPtr + 0x90, 0x54);
            // specify the number of sounds in the file
            Marshal.WriteInt16(infoPtr + 0x9C, 0);

            return (noSoundPtr, infoPtr);
        }

        internal void Enable() {
            if (this.PlaySpecificSoundHook == null && this.Plugin.Interface.TargetModuleScanner.TryScanText(Signatures.PlaySpecificSound, out var playPtr)) {
                this.PlaySpecificSoundHook = new Hook<PlaySpecificSoundDelegate>(playPtr, new PlaySpecificSoundDelegate(this.PlaySpecificSoundDetour));
            }

            if (this.GetResourceSyncHook == null && this.Plugin.Interface.TargetModuleScanner.TryScanText(Signatures.GetResourceSync, out var syncPtr)) {
                this.GetResourceSyncHook = new Hook<GetResourceSyncPrototype>(syncPtr, new GetResourceSyncPrototype(this.GetResourceSyncDetour));
            }

            if (this.GetResourceAsyncHook == null && this.Plugin.Interface.TargetModuleScanner.TryScanText(Signatures.GetResourceAsync, out var asyncPtr)) {
                this.GetResourceAsyncHook = new Hook<GetResourceAsyncPrototype>(asyncPtr, new GetResourceAsyncPrototype(this.GetResourceAsyncDetour));
            }

            this.PlaySpecificSoundHook?.Enable();
            this.GetResourceSyncHook?.Enable();
            this.GetResourceAsyncHook?.Enable();
        }

        internal void Disable() {
            this.PlaySpecificSoundHook?.Disable();
            this.GetResourceSyncHook?.Disable();
            this.GetResourceAsyncHook?.Disable();
        }

        public void Dispose() {
            this.PlaySpecificSoundHook?.Dispose();
            this.GetResourceSyncHook?.Dispose();
            this.GetResourceAsyncHook?.Dispose();

            Marshal.FreeHGlobal(this.InfoPtr);
            Marshal.FreeHGlobal(this.NoSoundPtr);
        }

        [HandleProcessCorruptedStateExceptions]
        private void* PlaySpecificSoundDetour(long a1, int idx) {
            if (a1 == 0) {
                goto Original;
            }

            var scdData = *(byte**) (a1 + 8);
            if (scdData == null) {
                goto Original;
            }

            // check cached scds for path
            this.Scds.TryGetValue((IntPtr) scdData, out var path);

            // if the scd wasn't cached, look at the async lookups
            if (path == null) {
                foreach (var entry in this.AsyncScds.ToList()) {
                    try {
                        var dataPtr = Marshal.ReadIntPtr(entry.Key + ResourceDataPointerOffset);
                        if (dataPtr != (IntPtr) scdData) {
                            continue;
                        }

                        this.Scds[dataPtr] = entry.Value;
                        this.AsyncScds.Remove(entry.Key);
                        path = entry.Value;
                    } catch (Exception) {
                        // remove any async pointers that had errors while reading
                        this.AsyncScds.Remove(entry.Key);
                    }
                }

                // if we still couldn't find a path for this pointer, give up
                if (path == null) {
                    goto Original;
                }
            }

            path = path.ToLowerInvariant();
            var specificPath = $"{path}/{idx}";

            this.Recent.Enqueue(specificPath);
            while (this.Recent.Count > this.Plugin.Config.LogEntries) {
                this.Recent.TryDequeue(out _);
            }

            var shouldFilter = this.Plugin.Config.Globs
                .Where(entry => entry.Value)
                .Any(entry => entry.Key.IsMatch(specificPath));
            if (shouldFilter) {
                return this.PlaySpecificSoundHook!.Original((long) this.InfoPtr, 0);
            }

            Original:
            return this.PlaySpecificSoundHook!.Original(a1, idx);
        }

        private void* GetResourceSyncDetour(IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown) {
            return this.ResourceDetour(true, pFileManager, pCategoryId, pResourceType, pResourceHash, pPath, pUnknown, false);
        }

        private void* GetResourceAsyncDetour(IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown, bool isUnknown) {
            return this.ResourceDetour(false, pFileManager, pCategoryId, pResourceType, pResourceHash, pPath, pUnknown, isUnknown);
        }

        private void* ResourceDetour(bool isSync, IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown, bool isUnknown) {
            var ret = this.CallOriginalResourceHandler(isSync, pFileManager, pCategoryId, pResourceType, pResourceHash, pPath, pUnknown, isUnknown);

            var path = Util.ReadTerminatedString((byte*) pPath);
            if (ret != null && path.EndsWith(".scd")) {
                var scdData = Marshal.ReadIntPtr((IntPtr) ret + ResourceDataPointerOffset);
                // if we immediately have the scd data, cache it, otherwise add it to a waiting list to hopefully be picked up at sound play time
                if (scdData != IntPtr.Zero) {
                    this.Scds[scdData] = path;
                } else if (this.Scds.All(entry => entry.Value != path)) {
                    // only add to the waiting list if we haven't resolved this path yet
                    this.AsyncScds[(IntPtr) ret] = path;
                }
            }

            return ret;
        }

        private void* CallOriginalResourceHandler(bool isSync, IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown, bool isUnknown) {
            return isSync
                ? this.GetResourceSyncHook!.Original(pFileManager, pCategoryId, pResourceType, pResourceHash, pPath, pUnknown)
                : this.GetResourceAsyncHook!.Original(pFileManager, pCategoryId, pResourceType, pResourceHash, pPath, pUnknown, isUnknown);
        }
    }
}
