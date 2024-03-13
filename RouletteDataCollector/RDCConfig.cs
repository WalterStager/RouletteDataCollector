using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

using Dalamud.Plugin.Services;

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
        private IPluginLog? log;

        public void Initialize(DalamudPluginInterface pluginInterface, IPluginLog log)
        {
            this.PluginInterface = pluginInterface;
            this.log = log;
        }

        public void Save()
        {
            this.PluginInterface!.SavePluginConfig(this);
        }


        public void DebugButtonAction()
        {

        }
    }
}
