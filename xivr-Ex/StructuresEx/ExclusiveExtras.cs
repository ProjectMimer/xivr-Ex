using System;
using System.Numerics;
using xivr.Structures;

namespace xivr.StructuresEx
{
    public unsafe class ExclusiveExtras
    {
        HandyHousing handyHousing = new HandyHousing();

        private static class Signatures
        {
            internal const string g_LayoutWorld = "4C 8B 05 ?? ?? ?? ?? 85 C9 74 ??";
        }

        public bool Initalize()
        {
            handyHousing.Initalize(Signatures.g_LayoutWorld);
            return true;
        }

        public bool Dispose()
        {
            handyHousing.Dispose();
            return true;
        }

        public void UpdateHandyHousing(XBoxStatus xboxStatus, Matrix4x4 hmdMatrix, Matrix4x4 rhcMatrix, Matrix4x4 lhcMatrix)
        {
            handyHousing.Update(xboxStatus, hmdMatrix, rhcMatrix, lhcMatrix);
        }
    }
}
