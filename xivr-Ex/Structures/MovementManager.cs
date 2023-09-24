using System;
using System.Runtime.InteropServices;

namespace xivr.Structures
{
    public unsafe struct MovementManager
    {
        public MovementManagerFlying Flying;
        public MovementManagerGround Ground;
    }


    [StructLayout(LayoutKind.Explicit, Size = 0x80)]
    public unsafe struct MovementManagerFlying
    {
        [FieldOffset(0x0)] public void* vtbl;
        [FieldOffset(0x10)] public float PositionX;
        [FieldOffset(0x14)] public float PositionY;
        [FieldOffset(0x18)] public float PositionZ;
        [FieldOffset(0x48)] public float SpeedCurrent;
        [FieldOffset(0x50)] public float SpeedMax;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x80)]
    public unsafe struct MovementManagerGround
    {
        [FieldOffset(0)] public void* vtbl;
        [FieldOffset(0x10)] public float MovingMax;
        [FieldOffset(0x14)] public float AscendDecendMax;
        [FieldOffset(0x18)] public float Angle;
        [FieldOffset(0x1C)] public float AscendDecendPitch;
    }
}
