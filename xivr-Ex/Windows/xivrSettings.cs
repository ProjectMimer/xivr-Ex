using System;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using xivr.Structures;

namespace xivr.Windows;

public class xivrSettings : Window, IDisposable
{
    private Plugin self;
    private Configuration cfg;
    private bool doUpdate = false;
    private uiOptionStrings lngOptions = Language.rawLngData[0];

    public xivrSettings(Plugin self, Configuration cfg) : base("xivr##xivrSettings")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(750, 760),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.self = self;
        this.cfg = cfg;
        lngOptions = Language.rawLngData[cfg!.data.languageType];
    }


    public void Dispose()
    {
    }

    public override void Draw()
    {
        doUpdate = false;

        ImGui.BeginChild("Outer", new Vector2(730, 750), true);

        ShowKofi();

        ImGui.BeginChild("VR", new Vector2(350, 230), true);

        if (ImGui.Checkbox(lngOptions.isEnabled_Line1, ref cfg!.data.isEnabled))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.isAutoEnabled_Line1, ref cfg!.data.isAutoEnabled))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.autoResize_Line1, ref cfg!.data.autoResize))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.autoMove_Line1, ref cfg!.data.autoMove))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.enableOSK_Line1, ref cfg!.data.osk))
            doUpdate = true;

        if (ImGui.Button(lngOptions.runRecenter_Line1))
            cfg.data.runRecenter = true;

        if (ImGui.Checkbox(lngOptions.vLog_Line1, ref cfg!.data.vLog))
            doUpdate = true;

        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("Misc", new Vector2(350, 230), true);

        if (ImGui.Checkbox(lngOptions.motioncontrol_Line1, ref cfg!.data.motioncontrol))
        {
            cfg!.data.hmdPointing = cfg!.data.motioncontrol;
            doUpdate = true;
        }

        if (ImGui.Checkbox(lngOptions.conloc_Line1, ref cfg!.data.conloc))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.hmdloc_Line1, ref cfg!.data.hmdloc))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.vertloc_Line1, ref cfg!.data.vertloc))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.standingMode_Line1, ref cfg!.data.standingMode))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.immersiveMovement_Line1, ref cfg!.data.immersiveMovement))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.showWeaponInHand_Line1, ref cfg!.data.showWeaponInHand))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.forceFloatingScreen_Line1, ref cfg!.data.forceFloatingScreen))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.forceFloatingInCutscene_Line1, ref cfg!.data.forceFloatingInCutscene))
            doUpdate = true;

        ImGui.EndChild();

        DrawLocks();
        ImGui.SameLine();
        DrawUISetings();

        ImGui.EndChild();

        if (doUpdate == true)
            cfg!.Save();

        ImGui.End();
    }

    private void DrawLocks()
    {
        string[] optionsMouseMultiplyer = { "1x", "2x", "3x" };

        ImGui.BeginChild("Snap Turning", new Vector2(350, 200), true);

        if (ImGui.Checkbox(lngOptions.horizonLock_Line1, ref cfg!.data.horizonLock))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.snapRotateAmountX_Line1, ref cfg!.data.horizontalLock))
            doUpdate = true;

        ImGui.Text(lngOptions.snapRotateAmountX_Line2); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawLocks:hsa", ref cfg!.data.snapRotateAmountX, 0, 90, "%.0f"))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.snapRotateAmountY_Line1, ref cfg!.data.verticalLock))
            doUpdate = true;

        ImGui.Text(lngOptions.snapRotateAmountY_Line2); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawLocks:vsa", ref cfg!.data.snapRotateAmountY, 0, 90, "%.0f"))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.disableXboxShoulder_Line1, ref cfg!.data.disableXboxShoulder))
            doUpdate = true;

        ImGui.Text(lngOptions.mouseMultiplyer_Line1); ImGui.SameLine(); ImGui.SetNextItemWidth(100);
        if (ImGui.BeginCombo("##DrawLocks:MouseMulti", optionsMouseMultiplyer[cfg!.data.mouseMultiplyer]))
        {
            setCombo(optionsMouseMultiplyer, false, ref cfg!.data.mouseMultiplyer);
            ImGui.EndCombo();
            doUpdate = true;
        }

        ImGui.EndChild();
    }



    private void DrawUISetings()
    {
        ImGui.BeginChild("UI", new Vector2(350, 440), true);

        ImGui.Text(lngOptions.uiOffsetZ_Line1); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawUISetings:uizoff", ref cfg!.data.uiOffsetZ, 0.0f, 100.0f, "%.f"))
            doUpdate = true;

        ImGui.Text(lngOptions.uiOffsetY_Line1); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawUISetings:uiyscale", ref cfg!.data.uiOffsetY, -100, 100, "%.f"))
            doUpdate = true;

        ImGui.Text(lngOptions.uiOffsetScale_Line1); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawUISetings:uizscale", ref cfg!.data.uiOffsetScale, 0.1f, 5.0f, "%f"))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.uiDepth_Line1, ref cfg!.data.uiDepth))
            doUpdate = true;

        ImGui.Text(lngOptions.ipdOffset_Line1); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawUISetings:ipdoff", ref cfg!.data.ipdOffset, -10, 10, "%f"))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.swapEyes_Line1, ref cfg!.data.swapEyes))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.swapEyesUI_Line1, ref cfg!.data.swapEyesUI))
            doUpdate = true;

        ImGui.Text(lngOptions.armMultiplier_Line1); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawUISetings:armmultiplier", ref cfg!.data.armMultiplier, 0, 200, "%.f"))
            doUpdate = true;

        ImGui.Text(lngOptions.offsetAmountX_Line1); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawUISetings:xoff", ref cfg!.data.offsetAmountX, -100, 100, "%.f"))
            doUpdate = true;

        ImGui.Text(lngOptions.offsetAmountY_Line1); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawUISetings:yoff", ref cfg!.data.offsetAmountY, -100, 100, "%.f"))
            doUpdate = true;

        ImGui.Text(lngOptions.offsetAmountZ_Line1); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawUISetings:zoff", ref cfg!.data.offsetAmountZ, -100, 100, "%.f"))
            doUpdate = true;

        ImGui.Text(lngOptions.offsetAmountYFPS_Line1); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawUISetings:fpsyoff", ref cfg!.data.offsetAmountYFPS, -100, 100, "%.f"))
            doUpdate = true;

        ImGui.Text(lngOptions.offsetAmountZFPS_Line1); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawUISetings:fpszoff", ref cfg!.data.offsetAmountZFPS, -100, 100, "%.f"))
            doUpdate = true;

        ImGui.Text(lngOptions.offsetAmountYFPSMount_Line1); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawUISetings:mountyoff", ref cfg!.data.offsetAmountYFPSMount, -100, 100, "%.f"))
            doUpdate = true;

        ImGui.Text(lngOptions.offsetAmountZFPSMount_Line1); ImGui.SameLine();
        if (ImGui.SliderFloat("##DrawUISetings:mountzoff", ref cfg!.data.offsetAmountZFPSMount, -100, 100, "%.f"))
            doUpdate = true;

        ImGui.Text(lngOptions.targetCursorSize_Line1); ImGui.SameLine();
        if (ImGui.SliderInt("##DrawUISetings:targetcur", ref cfg!.data.targetCursorSize, 25, 255))
            doUpdate = true;

        if (ImGui.Checkbox(lngOptions.ultrawideshadows_Line1, ref cfg!.data.ultrawideshadows))
            doUpdate = true;

        ImGui.EndChild();
    }


    private void ShowKofi()
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


    public bool CheckUpdate()
    {
        return doUpdate;
    }

    public void Reset()
    {
        doUpdate = false;
    }


    private void setCombo(string[] optionList, bool reverse, ref uint optionValue)
    {
        for (uint n = 0; n < optionList.Length; n++)
        {
            uint r = reverse ? (uint)(optionList.Length - 1) - n : n;
            bool is_selected = (optionValue == r);
            if (ImGui.Selectable(optionList[r], is_selected))
                optionValue = r;
            if (is_selected)
                ImGui.SetItemDefaultFocus();
        }
    }

}

