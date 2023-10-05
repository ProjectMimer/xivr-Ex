using System.Diagnostics;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using xivr.Structures;

namespace xivr
{
    public static class PluginUI
    {
        public static bool isVisible = false;

        public static void Draw(uiOptionStrings lngOptions, ref bool doUpdate)
        {
            if (!isVisible)
                return;

            ImGui.SetNextWindowSize(new Vector2(750, 760), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(750, 770), new Vector2(9999));
            //if (ImGui.Begin("Configuration", ref isVisible, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            

            if (ImGui.Begin("XIVRConfiguration", ref isVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.BeginChild("Outer", new Vector2(730, 750), true);

                ShowKofi(lngOptions);

                ImGui.BeginChild("VR", new Vector2(350, 230), true);

                if (ImGui.Checkbox(lngOptions.isEnabled_Line1, ref Plugin.cfg!.data.isEnabled))
                    doUpdate = true;

                if (ImGui.Checkbox(lngOptions.isAutoEnabled_Line1, ref Plugin.cfg!.data.isAutoEnabled))
                    doUpdate = true;

                if (ImGui.Checkbox(lngOptions.autoResize_Line1, ref Plugin.cfg!.data.autoResize))
                    doUpdate = true;

                if (ImGui.Checkbox(lngOptions.autoMove_Line1, ref Plugin.cfg!.data.autoMove))
                    doUpdate = true;
                
                if (ImGui.Checkbox(lngOptions.enableOSK_Line1, ref Plugin.cfg!.data.osk))
                    doUpdate = true;

                if (ImGui.Button(lngOptions.runRecenter_Line1))
                    Plugin.cfg.data.runRecenter = true;

                if (ImGui.Checkbox(lngOptions.vLog_Line1, ref Plugin.cfg!.data.vLog))
                    doUpdate = true;

                ImGui.EndChild();

                ImGui.SameLine();

                ImGui.BeginChild("Misc", new Vector2(350, 230), true);

                if (ImGui.Checkbox(lngOptions.motioncontrol_Line1, ref Plugin.cfg!.data.motioncontrol))
                {
                    Plugin.cfg!.data.hmdPointing = !Plugin.cfg!.data.motioncontrol;
                    doUpdate = true;
                }

                if (ImGui.Checkbox(lngOptions.conloc_Line1, ref Plugin.cfg!.data.conloc))
                    doUpdate = true;

                if (ImGui.Checkbox(lngOptions.hmdloc_Line1, ref Plugin.cfg!.data.hmdloc))
                    doUpdate = true;

                if (ImGui.Checkbox(lngOptions.vertloc_Line1, ref Plugin.cfg!.data.vertloc))
                    doUpdate = true;

                if (ImGui.Checkbox(lngOptions.immersiveMovement_Line1, ref Plugin.cfg!.data.immersiveMovement))
                    doUpdate = true;

                if (ImGui.Checkbox(lngOptions.showWeaponInHand_Line1, ref Plugin.cfg!.data.showWeaponInHand))
                    doUpdate = true;

                if (ImGui.Checkbox(lngOptions.forceFloatingScreen_Line1, ref Plugin.cfg!.data.forceFloatingScreen))
                    doUpdate = true;

                if (ImGui.Checkbox(lngOptions.forceFloatingInCutscene_Line1, ref Plugin.cfg!.data.forceFloatingInCutscene))
                    doUpdate = true;

                ImGui.EndChild();

                DrawLocks(lngOptions, ref doUpdate);
                ImGui.SameLine();
                DrawUISetings(lngOptions, ref doUpdate);

                ImGui.EndChild();

                if (doUpdate == true)
                    Plugin.cfg!.Save();

                ImGui.End();
            }
        }

        public static void DrawLocks(uiOptionStrings lngOptions, ref bool doUpdate)
        {
            ImGui.BeginChild("Snap Turning", new Vector2(350, 200), true);

            if(ImGui.Checkbox(lngOptions.horizonLock_Line1, ref Plugin.cfg!.data.horizonLock))
                doUpdate = true;

            if (ImGui.Checkbox(lngOptions.snapRotateAmountX_Line1, ref Plugin.cfg!.data.horizontalLock))
                doUpdate = true;

            ImGui.Text(lngOptions.snapRotateAmountX_Line2); ImGui.SameLine();
            if (ImGui.SliderFloat("##DrawLocks:hsa", ref Plugin.cfg!.data.snapRotateAmountX, 0, 90, "%.0f"))
                doUpdate = true;

            if (ImGui.Checkbox(lngOptions.snapRotateAmountY_Line1, ref Plugin.cfg!.data.verticalLock))
                doUpdate = true;

            ImGui.Text(lngOptions.snapRotateAmountY_Line2); ImGui.SameLine();
            if (ImGui.SliderFloat("##DrawLocks:vsa", ref Plugin.cfg!.data.snapRotateAmountY, 0, 90, "%.0f"))
                doUpdate = true;

            if (ImGui.Checkbox(lngOptions.disableXboxShoulder_Line1, ref Plugin.cfg!.data.disableXboxShoulder))
                doUpdate = true;

            ImGui.EndChild();
        }



        public static void DrawUISetings(uiOptionStrings lngOptions, ref bool doUpdate)
        {
            ImGui.BeginChild("UI", new Vector2(350, 440), true);

            ImGui.Text(lngOptions.uiOffsetZ_Line1); ImGui.SameLine(); 
            if(ImGui.SliderFloat("##DrawUISetings:uizoff", ref Plugin.cfg!.data.uiOffsetZ, 0, 100, "%.0f"))
                doUpdate = true;

            ImGui.Text(lngOptions.uiOffsetScale_Line1); ImGui.SameLine();
            if (ImGui.SliderFloat("##DrawUISetings:uizscale", ref Plugin.cfg!.data.uiOffsetScale, 1, 5, "%.00f"))
                doUpdate = true;

            if (ImGui.Checkbox(lngOptions.uiDepth_Line1, ref Plugin.cfg!.data.uiDepth))
                doUpdate = true;

            ImGui.Text(lngOptions.ipdOffset_Line1); ImGui.SameLine();
            if (ImGui.SliderFloat("##DrawUISetings:ipdoff", ref Plugin.cfg!.data.ipdOffset, -10, 10, "%f"))
                doUpdate = true;

            if (ImGui.Checkbox(lngOptions.swapEyes_Line1, ref Plugin.cfg!.data.swapEyes))
                doUpdate = true;

            if (ImGui.Checkbox(lngOptions.swapEyesUI_Line1, ref Plugin.cfg!.data.swapEyesUI))
                doUpdate = true;

            ImGui.Text(lngOptions.armMultiplier_Line1); ImGui.SameLine();
            if (ImGui.SliderFloat("##DrawUISetings:armmultiplier", ref Plugin.cfg!.data.armMultiplier, 0, 200, "%.f"))
                doUpdate = true;

            ImGui.Text(lngOptions.offsetAmountX_Line1); ImGui.SameLine();
            if (ImGui.SliderFloat("##DrawUISetings:xoff", ref Plugin.cfg!.data.offsetAmountX, -100, 100, "%.f"))
                doUpdate = true;

            ImGui.Text(lngOptions.offsetAmountY_Line1); ImGui.SameLine();
            if (ImGui.SliderFloat("##DrawUISetings:yoff", ref Plugin.cfg!.data.offsetAmountY, -100, 100, "%.f"))
                doUpdate = true;

            ImGui.Text(lngOptions.offsetAmountZ_Line1); ImGui.SameLine();
            if (ImGui.SliderFloat("##DrawUISetings:zoff", ref Plugin.cfg!.data.offsetAmountZ, -100, 100, "%.f"))
                doUpdate = true;

            ImGui.Text(lngOptions.offsetAmountYFPS_Line1); ImGui.SameLine();
            if (ImGui.SliderFloat("##DrawUISetings:fpsyoff", ref Plugin.cfg!.data.offsetAmountYFPS, -100, 100, "%.f"))
                doUpdate = true;

            ImGui.Text(lngOptions.offsetAmountZFPS_Line1); ImGui.SameLine();
            if (ImGui.SliderFloat("##DrawUISetings:fpszoff", ref Plugin.cfg!.data.offsetAmountZFPS, -100, 100, "%.f"))
                doUpdate = true;

            ImGui.Text(lngOptions.offsetAmountYFPSMount_Line1); ImGui.SameLine();
            if (ImGui.SliderFloat("##DrawUISetings:mountyoff", ref Plugin.cfg!.data.offsetAmountYFPSMount, -100, 100, "%.f"))
                doUpdate = true;

            ImGui.Text(lngOptions.offsetAmountZFPSMount_Line1); ImGui.SameLine();
            if (ImGui.SliderFloat("##DrawUISetings:mountzoff", ref Plugin.cfg!.data.offsetAmountZFPSMount, -100, 100, "%.f"))
                doUpdate = true;

            ImGui.Text(lngOptions.targetCursorSize_Line1); ImGui.SameLine();
            if (ImGui.SliderInt("##DrawUISetings:targetcur", ref Plugin.cfg!.data.targetCursorSize, 25, 255))
                doUpdate = true;

            if (ImGui.Checkbox(lngOptions.ultrawideshadows_Line1, ref Plugin.cfg!.data.ultrawideshadows))
                doUpdate = true;

            ImGui.EndChild();
        }


        public static void ShowKofi(uiOptionStrings lngOptions)
        {
            ImGui.BeginChild("Support", new Vector2(350, 50), true);

            ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x005E5BFF);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | 0x005E5BFF);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x005E5BFF);
            if (ImGui.Button(lngOptions.support_Line1))
            {
                Process.Start(new ProcessStartInfo { FileName = "https://ko-fi.com/projectmimer", UseShellExecute = true });
            }
            ImGui.PopStyleColor(3);
            ImGui.EndChild();
        }
    }
}
