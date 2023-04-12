using System;
using System.Runtime.InteropServices;

namespace xivr.StructuresEx
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct LayoutWorld
    {
        [FieldOffset(0x00)] public UInt64 vtbl;
        [FieldOffset(0x40)] public Housing* housing;
    }
}
