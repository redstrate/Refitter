using System;
using System.Collections.Generic;
using Dalamud.Configuration;

namespace Refitter;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public List<ConfigModel> Configs = [];
    public int Version { get; set; } = 1;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }

    public ConfigModel? GetModelOverride(int modelId)
    {
        foreach (var config in Configs)
            if (config.Model == modelId)
                return config;

        return null;
    }
}
