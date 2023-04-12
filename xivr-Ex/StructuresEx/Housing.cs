using System;
using System.Runtime.InteropServices;
using xivr.Structures;

namespace xivr.StructuresEx
{
    [Flags]
    public enum HousingModeTypes
    {
        Inactive,
        Move,
        Rotate,
        Remove,
        Placement,
        uk5,
        Store
    }

    [Flags]
    public enum HousingModeMouseStatus
    {
        Inactive,
        MouseOver,
        Placement,
        Selected
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct Housing
    {
        [FieldOffset(0x00)] public HousingModeTypes currentMode;
        [FieldOffset(0x04)] public HousingModeTypes prevMode;
        [FieldOffset(0x08)] public HousingModeMouseStatus mouseStatus;
        [FieldOffset(0x0C)] public int itemStatus;
        [FieldOffset(0x10)] public Model* mouseOverTarget;
        [FieldOffset(0x18)] public Model* selectedTarget;
        [FieldOffset(0x88)] public UInt64 selectedIcon;
        [FieldOffset(0xC0)] public float mousePosX;
        [FieldOffset(0xC4)] public float mousePosY;
        [FieldOffset(0xC8)] public float mousePosZ;
    }

}
