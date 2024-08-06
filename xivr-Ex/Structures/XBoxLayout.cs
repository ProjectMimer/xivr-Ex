using System;
using System.Runtime.InteropServices;

namespace xivr.Structures
{
    public struct XBoxButtonStatus
    {
        public bool active;
        public float value;
        public bool ChangedStatus;
        public bool ChangedValue;
        public void Set()
        {
            active = false;
            value = 0;
            ChangedStatus = false;
            ChangedValue = false;
        }

        public void Set(bool a, float v)
        {
            ChangedStatus = false;
            if (active != a)
            {
                active = a;
                ChangedStatus = true;
            }
            ChangedValue = false;
            if (value != v)
            {
                value = v; 
                ChangedValue = true;
            }
        }
    }

    public struct XBoxStatus
    {
        public XBoxButtonStatus dpad_up;
        public XBoxButtonStatus dpad_down;
        public XBoxButtonStatus dpad_left;
        public XBoxButtonStatus dpad_right;

        public XBoxButtonStatus left_stick_down;
        public XBoxButtonStatus left_stick_up;
        public XBoxButtonStatus left_stick_left;
        public XBoxButtonStatus left_stick_right;

        public XBoxButtonStatus right_stick_down;
        public XBoxButtonStatus right_stick_up;
        public XBoxButtonStatus right_stick_left;
        public XBoxButtonStatus right_stick_right;

        public XBoxButtonStatus button_y;
        public XBoxButtonStatus button_b;
        public XBoxButtonStatus button_a;
        public XBoxButtonStatus button_x;
        public XBoxButtonStatus left_bumper;
        public XBoxButtonStatus left_trigger;
        public XBoxButtonStatus left_stick_click;
        public XBoxButtonStatus right_bumper;
        public XBoxButtonStatus right_trigger;
        public XBoxButtonStatus right_stick_click;
        public XBoxButtonStatus start;
        public XBoxButtonStatus select;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x9C8)]
    public unsafe struct XBoxButtonOffsets
    {
        [FieldOffset(0x00)] public float select;
        [FieldOffset(0x04)] public float start;
        [FieldOffset(0x08)] public float left_stick_click;
        [FieldOffset(0x0C)] public float right_stick_click;
        [FieldOffset(0x10)] public float left_bumper;
        [FieldOffset(0x14)] public float right_bumper;
        [FieldOffset(0x18)] public float button_a;
        [FieldOffset(0x1C)] public float button_b;
        [FieldOffset(0x20)] public float button_x;
        [FieldOffset(0x24)] public float button_y;
        [FieldOffset(0x80)] public float dpad_left;
        [FieldOffset(0x84)] public float dpad_right;
        [FieldOffset(0x88)] public float dpad_up;
        [FieldOffset(0x8C)] public float dpad_down;
        [FieldOffset(0x90)] public float left_stick_left;
        [FieldOffset(0x94)] public float left_stick_right;
        [FieldOffset(0x98)] public float left_stick_up;
        [FieldOffset(0x9C)] public float left_stick_down;
        [FieldOffset(0xA0)] public float right_stick_left;
        [FieldOffset(0xA4)] public float right_stick_right;
        [FieldOffset(0xA8)] public float right_stick_up;
        [FieldOffset(0xAC)] public float right_stick_down;
        [FieldOffset(0xB4)] public float left_trigger;
        [FieldOffset(0xBC)] public float right_trigger;
    }


    [StructLayout(LayoutKind.Explicit, Size = 0xD0)]
    public unsafe struct XBoxLayout
    {
        [FieldOffset(0x00)] public float start;
        [FieldOffset(0x04)] public float select;
        [FieldOffset(0x08)] public float left_stick_click;
        [FieldOffset(0x0C)] public float right_stick_click;
        [FieldOffset(0x10)] public float left_bumper;
        [FieldOffset(0x14)] public float right_bumper;
        [FieldOffset(0x18)] public float button_a;
        [FieldOffset(0x1C)] public float button_b;
        [FieldOffset(0x20)] public float button_x;
        [FieldOffset(0x24)] public float button_y;

        [FieldOffset(0x80)] public float dpad_left;
        [FieldOffset(0x84)] public float dpad_right;
        [FieldOffset(0x88)] public float dpad_up;
        [FieldOffset(0x8C)] public float dpad_down;

        [FieldOffset(0x90)] public float left_stick_left;
        [FieldOffset(0x94)] public float left_stick_right;
        [FieldOffset(0x98)] public float left_stick_up;
        [FieldOffset(0x9C)] public float left_stick_down;

        [FieldOffset(0xA0)] public float right_stick_left;
        [FieldOffset(0xA4)] public float right_stick_right;
        [FieldOffset(0xA8)] public float right_stick_up;
        [FieldOffset(0xAC)] public float right_stick_down;

        [FieldOffset(0xB4)] public float left_trigger;
        [FieldOffset(0xB8)] public float right_trigger;
    }
}
