using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using xivr.Structures;

namespace xivr.StructuresEx
{
    public unsafe class HandyHousing
    {
        private LayoutWorld* worldLayout = null;
        private Vector3 controllerPosition = new Vector3(0, 0, 0);
        private Vector3 controllerAngles = new Vector3(0, 0, 0);
        private float halfPI = (MathF.PI / 2.0f);

        public bool Initalize(string g_LayoutWorld)
        {
            worldLayout = *(LayoutWorld**)xivr_Ex.SigScanner!.GetStaticAddressFromSig(g_LayoutWorld);
            return true;
        }

        public bool Dispose()
        {
            return true;
        }

        public void Update(XBoxStatus xboxStatus, Matrix4x4 hmdMatrix, Matrix4x4 rhcMatrix, Matrix4x4 lhcMatrix)
        {
            PlayerCharacter? player = xivr_Ex.ClientState!.LocalPlayer;
            if (player != null && worldLayout != null && worldLayout->housing != null && worldLayout->housing->currentMode == HousingModeTypes.Rotate)
            {
                Matrix4x4 playerRot = Matrix4x4.CreateFromAxisAngle(new Vector3(0, 1, 0), player.Rotation);
                playerRot.Translation = player.Position;

                Vector3 lhcAngles = xivr_hooks.GetAngles(lhcMatrix);
                Vector3 rhcVector = Vector3.Transform(new Vector3(rhcMatrix.Translation.X * -1, rhcMatrix.Translation.Y, rhcMatrix.Translation.Z * -1), playerRot);
                if (worldLayout->housing->selectedTarget != null && xboxStatus.right_bumper.active == true)
                {
                    Vector3 frameMove = rhcVector - controllerPosition;
                    Vector3 curPos = worldLayout->housing->selectedTarget->basePosition.Translation.Convert();

                    if (xboxStatus.right_bumper.value >= 0.95f)
                        worldLayout->housing->selectedTarget->basePosition.Translation = (curPos + (frameMove * 2.0f)).Convert();
                    else
                        worldLayout->housing->selectedTarget->basePosition.Translation = (curPos + frameMove).Convert();
                }

                if (worldLayout->housing->selectedTarget != null && xboxStatus.left_bumper.active == true)
                {
                    Vector3 frameMove = lhcAngles - controllerAngles;
                    if (xboxStatus.left_bumper.value >= 0.95f)
                        frameMove *= 2;

                    Quaternion frameRot = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), -frameMove.Y);
                    Quaternion curRot = worldLayout->housing->selectedTarget->basePosition.Rotation.Convert();

                    worldLayout->housing->selectedTarget->basePosition.Rotation = (curRot * frameRot).Convert();
                }
                controllerPosition = rhcVector;
                controllerAngles = lhcAngles;
            }
        }
    }
}
