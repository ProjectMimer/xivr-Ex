using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.DrawDataContainer;

namespace xivr.Structures
{
    [StructLayout(LayoutKind.Explicit, Size = 40)]
    public unsafe struct CharEquipData
    {
        [FieldOffset(0x00)] public fixed uint Data[10];
        [FieldOffset(0x00)] public CharEquipSlotData Head;
        [FieldOffset(0x04)] public CharEquipSlotData Body;
        [FieldOffset(0x08)] public CharEquipSlotData Hands;
        [FieldOffset(0x0C)] public CharEquipSlotData Legs;
        [FieldOffset(0x10)] public CharEquipSlotData Feet;
        [FieldOffset(0x14)] public CharEquipSlotData Ears;
        [FieldOffset(0x18)] public CharEquipSlotData Neck;
        [FieldOffset(0x1C)] public CharEquipSlotData Wrist;
        [FieldOffset(0x20)] public CharEquipSlotData RRing;
        [FieldOffset(0x24)] public CharEquipSlotData LRing;

        public void Save(Character* character)
        {
            Head.Save(character->DrawData.EquipmentModelIds[(int)EquipmentSlot.Head]);
            Body.Save(character->DrawData.EquipmentModelIds[(int)EquipmentSlot.Body]);
            Hands.Save(character->DrawData.EquipmentModelIds[(int)EquipmentSlot.Hands]);
            Legs.Save(character->DrawData.EquipmentModelIds[(int)EquipmentSlot.Legs]);
            Feet.Save(character->DrawData.EquipmentModelIds[(int)EquipmentSlot.Feet]);
            Ears.Save(character->DrawData.EquipmentModelIds[(int)EquipmentSlot.Ears]);
            Neck.Save(character->DrawData.EquipmentModelIds[(int)EquipmentSlot.Neck]);
            Wrist.Save(character->DrawData.EquipmentModelIds[(int)EquipmentSlot.Wrists]);
            RRing.Save(character->DrawData.EquipmentModelIds[(int)EquipmentSlot.RFinger]);
            LRing.Save(character->DrawData.EquipmentModelIds[(int)EquipmentSlot.LFinger]);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x4)]
    public unsafe struct CharEquipSlotData
    {
        [FieldOffset(0)] public uint Data;
        [FieldOffset(0)] public ushort Id;
        [FieldOffset(2)] public byte Variant;
        [FieldOffset(3)] public byte Dye1;
        [FieldOffset(4)] public byte Dye2;
        public CharEquipSlotData(ushort id, byte variant, byte dye1, byte dye2)
        {
            Id = id;
            Variant = variant;
            Dye1 = dye1;
            Dye2 = dye2;
        }
        public void Save(EquipmentModelId data)
        {
            Id = data.Id;
            Variant = data.Variant;
            Dye1 = data.Stain0;
            Dye2 = data.Stain1;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct CharWeaponData
    {
        [FieldOffset(0x00)] public fixed UInt64 Data[3];
        [FieldOffset(0x00)] public CharWeaponSlotData MainHand;
        [FieldOffset(0x08)] public CharWeaponSlotData OffHand;
        [FieldOffset(0x0C)] public CharWeaponSlotData Uk3;

        public void Save(Character* character)
        {
            MainHand.Save(character->DrawData.Weapon(DrawDataContainer.WeaponSlot.MainHand).ModelId);
            OffHand.Save(character->DrawData.Weapon(DrawDataContainer.WeaponSlot.OffHand).ModelId);
            Uk3.Save(character->DrawData.Weapon(DrawDataContainer.WeaponSlot.Unk).ModelId);
        }

    }

    [StructLayout(LayoutKind.Explicit, Size = 0x8)]
    public unsafe struct CharWeaponSlotData
    {
        [FieldOffset(0)] public UInt64 Data;
        [FieldOffset(0)] public ushort Type;
        [FieldOffset(2)] public ushort Id;
        [FieldOffset(4)] public ushort Variant;
        [FieldOffset(6)] public byte Dye1;
        [FieldOffset(7)] public byte Dye2;

        public CharWeaponSlotData(ushort type, ushort id, byte variant, byte dye1, byte dye2)
        {
            Type = type;
            Id = id;
            Variant = variant;
            Dye1 = dye1;
            Dye2 = dye2;
        }

        public void Save(WeaponModelId data)
        {
            Type = data.Type;
            Id = data.Id;
            Variant = data.Variant;
            Dye1 = data.Stain0;
            Dye2 = data.Stain1;
        }
    }
}
