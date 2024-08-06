using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Common.Configuration;
using Dalamud.Memory;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace SettingsManager
{
    public unsafe class ConfigManager
    {
        [PluginService] public static IPluginLog? Log { get; private set; } = null;

        private Framework* frameworkInstance = Framework.Instance();

        private ConfigBase*[] cfgBase = new ConfigBase*[4];
        private Dictionary<string, List<Tuple<uint, uint>>> MappedSettings = new Dictionary<string, List<Tuple<uint, uint>>>();
        private List<string> cfgSearchStrings = new List<string>();

        private Dictionary<string, Dictionary<uint, KeyValuePair<uint, ConfigValue>>> savedSettings = new Dictionary<string, Dictionary<uint, KeyValuePair<uint, ConfigValue>>>();

        public ConfigManager()
        {
            cfgBase[0] = &(frameworkInstance->SystemConfig.SystemConfigBase.ConfigBase);
            cfgBase[1] = &(frameworkInstance->SystemConfig.SystemConfigBase.UiConfig);
            cfgBase[2] = &(frameworkInstance->SystemConfig.SystemConfigBase.UiControlConfig);
            cfgBase[3] = &(frameworkInstance->SystemConfig.SystemConfigBase.UiControlGamepadConfig);

            ClearList();
        }

        public void Dispose()
        {
            ClearList();
        }

        public void ClearList()
        {
            cfgSearchStrings.Clear();
            MappedSettings.Clear();
            savedSettings.Clear();
        }

        public void AddToList(string name)
        {
            if(!cfgSearchStrings.Contains(name))
                cfgSearchStrings.Add(name);
        }

        public void AddToList(List<string> list)
        {
            foreach(string name in list)
                AddToList(name);
        }

        public void MapSettings()
        {
            MappedSettings.Clear();
            for (uint cfgId = 0; cfgId < cfgBase.Length; cfgId++)
            {
                for (uint i = 0; i < cfgBase[cfgId]->ConfigCount; i++)
                {
                    if (cfgBase[cfgId]->ConfigEntry[i].Type == 0)
                        continue;

                    string name = MemoryHelper.ReadStringNullTerminated(new IntPtr(cfgBase[cfgId]->ConfigEntry[i].Name));
                    if (cfgSearchStrings.Contains(name))
                    {
                        if (!MappedSettings.ContainsKey(name))
                            MappedSettings[name] = new List<Tuple<uint, uint>>();
                        MappedSettings[name].Add(new Tuple<uint, uint>(cfgId, cfgBase[cfgId]->ConfigEntry[i].Index));
                    }
                }
            }
        }

        public void SetSettingsValue(string setting, uint value)
        {
            List<Tuple<uint, uint>> list = MappedSettings.GetValueOrDefault<string, List<Tuple<uint, uint>>>(setting, new List<Tuple<uint, uint>>());
            foreach (Tuple<uint, uint> item in list)
                cfgBase[item.Item1]->ConfigEntry[item.Item2].SetValueUInt(value);
        }

        public uint GetSettingsValue(string setting, int index)
        {
            List<Tuple<uint, uint>> list = MappedSettings.GetValueOrDefault<string, List<Tuple<uint, uint>>>(setting, new List<Tuple<uint, uint>>());
            if (index >= list.Count)
                return 0;
            return cfgBase[list[index].Item1]->ConfigEntry[list[index].Item2].Value.UInt;
        }


        public void DebugSettings(bool printAll = false)
        {
            Save(false);

            foreach (KeyValuePair<string, Dictionary<uint, KeyValuePair<uint, ConfigValue>>> itemByName in savedSettings)
            {
                foreach (KeyValuePair<uint, KeyValuePair<uint, ConfigValue>> cfgEntry in itemByName.Value)
                {
                    string name = itemByName.Key;
                    uint cfgId = cfgEntry.Key;
                    uint i = cfgEntry.Value.Key;

                    if (cfgSearchStrings.Contains(name))
                        Log!.Info($"Location: * {cfgId} | {i} name: {name} value: {cfgBase[cfgId]->ConfigEntry[i].Value.UInt}");
                    else if (printAll)
                        Log!.Info($"Location:   {cfgId} | {i} name: {name} value: {cfgBase[cfgId]->ConfigEntry[i].Value.UInt}");
                }
            }

            foreach(string itemName in cfgSearchStrings)
            {
                if (!savedSettings.ContainsKey(itemName))
                    Log!.Info($"{itemName} Not found in config options");
            }
        }

        public void Save(bool compare = false)
        {
            if (!compare)
            {
                savedSettings.Clear();
                for (uint cfgId = 0; cfgId < cfgBase.Length; cfgId++)
                {
                    for (uint i = 0; i < cfgBase[cfgId]->ConfigCount; i++)
                    {
                        if (cfgBase[cfgId]->ConfigEntry[i].Type == 0)
                            continue;

                        string name = MemoryHelper.ReadStringNullTerminated(new IntPtr(cfgBase[cfgId]->ConfigEntry[i].Name));
                        if (!savedSettings.ContainsKey(name))
                            savedSettings[name] = new Dictionary<uint, KeyValuePair<uint, ConfigValue>>();
                        savedSettings[name][cfgId] = new KeyValuePair<uint, ConfigValue>(i, cfgBase[cfgId]->ConfigEntry[i].Value);
                    }
                }
                Log!.Info($"--- Current Settings Saved ---");
            }
            else
            {
                Log!.Info($"--- Changed Settings ---");
                foreach (KeyValuePair<string, Dictionary<uint, KeyValuePair<uint, ConfigValue>>> itemByName in savedSettings)
                    foreach (KeyValuePair<uint, KeyValuePair<uint, ConfigValue>> cfgEntry in itemByName.Value)
                        if(cfgEntry.Value.Value.UInt != cfgBase[cfgEntry.Key]->ConfigEntry[cfgEntry.Value.Key].Value.UInt)
                            Log!.Info($"{cfgEntry.Key} | {cfgEntry.Value.Key} -- {itemByName.Key} -- {cfgEntry.Value.Value.UInt} {cfgEntry.Value.Value.Float} | {cfgBase[cfgEntry.Key]->ConfigEntry[cfgEntry.Value.Key].Value.UInt} {cfgBase[cfgEntry.Key]->ConfigEntry[cfgEntry.Value.Key].Value.Float}");
            }
        }
    }
}
