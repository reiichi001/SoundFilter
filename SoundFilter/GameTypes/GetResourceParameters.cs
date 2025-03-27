using System.Runtime.InteropServices;

namespace SoundFilter.GameTypes;

// https://github.com/xivdev/Penumbra/blob/master/Penumbra/Interop/Structs/GetResourceParameters.cs
[StructLayout(LayoutKind.Explicit)]
public struct GetResourceParameters
{
    [FieldOffset(16)]
    public uint SegmentOffset;

    [FieldOffset(20)]
    public uint SegmentLength;

    public bool IsPartialRead => SegmentLength != 0;
}
