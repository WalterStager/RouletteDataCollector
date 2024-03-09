using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace RouletteDataCollector
{
    [Serializable]
    public class RDCConfig : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool EnableSaveData { get; set; } = true;

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? PluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.PluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }
    }
}
