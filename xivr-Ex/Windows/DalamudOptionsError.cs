using System;
using System.IO;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using xivr.Structures;

namespace xivr.Windows;

public class DalamudOptionsError : Window, IDisposable
{
    private Plugin self;
    private Configuration cfg;
    private int optionsError = 0;
    private string dConfig = Path.GetFullPath(Path.Combine(Plugin.PluginInterface!.ConfigDirectory.FullName!, "..\\..\\dalamudConfig.json"));
    private string dCurItm = "";
    private string lConfig = Path.GetFullPath(Path.Combine(Plugin.PluginInterface!.ConfigDirectory.FullName!, "..\\..\\launcherConfigV3.json"));
    private string lCurItm = "";
    private uiOptionStrings lngOptions = Language.rawLngData[0];

    public DalamudOptionsError(Plugin self, Configuration cfg) : base("xivr##Settings Error")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(620, 270),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.self = self;
        this.cfg = cfg;
        lngOptions = Language.rawLngData[cfg!.data.languageType];
    }

    public void Dispose()
    {
        optionsError = 0;
    }

    public override void Draw()
    {
        ImGui.BeginChild($"##warning", new Vector2(600, 250), true);

        ImGui.Text(lngOptions.errorSettingsMessage_Line1);

        if ((optionsError & 1) == 1)
        {
            PrintColorText($"Error: Can not find config files 'dalamudConfig.json' or 'launcherConfigV3.json'", new Vector3(255, 0, 0));
        }
        if ((optionsError & 2) == 2)
        {
            ImGui.Text($"");
            PrintColorText($"Dalamud Settings:", new Vector3(0, 255, 255), true);
            PrintColorText($"General Tab:", new Vector3(255, 255, 0), true);
            PrintColorText($"'Wait for plugins before game loads'", new Vector3(255, 255, 255));
            ImGui.Text($"{dConfig}");
            PrintColorText($"   -- \"IsResumeGameAfterPluginLoad\": ", new Vector3(255, 255, 255), true);
            PrintColorText($"{dCurItm}", new Vector3(255, 0, 0), true);
            PrintColorText($" -> \"IsResumeGameAfterPluginLoad\": ", new Vector3(255, 255, 255), true);
            PrintColorText($"true", new Vector3(0, 255, 0), false);

            if(ImGui.Button("Settings"))
                Plugin.CommandManager!.ProcessCommand("/xlsettings");

            PrintColorText(lngOptions.errorSettingsRestart_Line1, new Vector3(255, 255, 255));
        }
        if ((optionsError & 4) == 4)
        {
            ImGui.Text($"");
            PrintColorText($"Launcher Settings:", new Vector3(0, 255, 255), true);
            PrintColorText($"Dalamud Tab:", new Vector3(255, 255, 0), true);
            PrintColorText($"'Choose how to load Dalamud: New'", new Vector3(255, 255, 255));
            ImGui.Text($"{lConfig}");
            PrintColorText($"   -- \"InGameAddonLoadMethod\": ", new Vector3(255, 255, 255), true);
            PrintColorText($"{lCurItm}", new Vector3(255, 0, 0), true);
            PrintColorText($" -> \"InGameAddonLoadMethod\": ", new Vector3(255, 255, 255), true);
            PrintColorText($"\"EntryPoint\"", new Vector3(0, 255, 0), false);
        }

        ImGui.EndChild();
    }

    private void PrintColorText(string text, Vector3 color, bool sameLine = false)
    {
        PrintColorText(text, new Vector4(color, 255), sameLine);
    }

    private void PrintColorText(string text, Vector4 color, bool sameLine = false)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(color));
        ImGui.Text(text);
        ImGui.PopStyleColor();
        if (sameLine)
            ImGui.SameLine();
    }

    public bool CheckDalamudOptions()
    {
        optionsError = 0;
        if (File.Exists(dConfig) && File.Exists(lConfig))
        {
            //----
            // Check Dalamud Config File
            //----
            string jsonData = File.ReadAllText(dConfig);
            string[] parts = jsonData.Split("\"IsResumeGameAfterPluginLoad\":");

            if (parts.Length > 1)
            {
                string[] subparts = parts[1].Split(",");
                if (subparts.Length > 1 && subparts[0] != " true")
                {
                    dCurItm = subparts[0];
                    optionsError += 2;
                }
            }

            //----
            // Check Launcher Config File
            //----
            jsonData = File.ReadAllText(lConfig);
            parts = jsonData.Split("\"InGameAddonLoadMethod\":");
            if (parts.Length > 1)
            {
                string[] subparts = parts[1].Split(",");
                if (subparts.Length > 1 && subparts[0] != " \"EntryPoint\"")
                {
                    lCurItm = subparts[0];
                    optionsError += 4;
                }
            }
        }
        else
            optionsError += 1;

        if (optionsError > 0)
            return true;
        return false;
    }
}