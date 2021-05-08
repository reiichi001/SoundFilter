using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Configuration;
using Dalamud.Plugin;
using DotNet.Globbing;

namespace SoundFilter {
    [Serializable]
    internal class Configuration : IPluginConfiguration {
        public int Version { get; set; } = 1;

        private DalamudPluginInterface? Interface { get; set; }
        private Dictionary<string, Glob> CachedGlobs { get; } = new();

        public bool Enabled = true;
        public bool ShowLog;
        public uint LogEntries = 250;
        public Dictionary<string, bool> Filtered { get; set; } = new();

        internal IReadOnlyDictionary<Glob, bool> Globs {
            get {
                return this.Filtered.ToDictionary(
                    entry => {
                        if (this.CachedGlobs.TryGetValue(entry.Key, out var cached)) {
                            return cached;
                        }

                        var glob = Glob.Parse(entry.Key);
                        this.CachedGlobs[entry.Key] = glob;
                        return glob;
                    },
                    entry => entry.Value
                );
            }
        }

        internal void Initialise(DalamudPluginInterface @interface) {
            this.Interface = @interface;
        }

        internal void Save() {
            this.Interface?.SavePluginConfig(this);
        }
    }
}
