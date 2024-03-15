using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

using Dalamud.Plugin.Services;
using System.Timers;

namespace RouletteDataCollector
{
    [Serializable]
    public class RDCConfig : IPluginConfiguration
    {
        public bool buttonLocked { get; private set; } = false;
        private Timer buttonLockTimer = new Timer();

        public int Version { get; set; } = 0;

        public bool enableSaveData { get; set; } = true;

        public uint? remainingInspections { get; set; } = null; 

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;
        private RouletteDataCollector? plugin;

        public void Initialize(RouletteDataCollector plugin, DalamudPluginInterface pluginInterface)
        {
            this.buttonLockTimer.Elapsed += OnButtonLockTimerElapsed;
            this.buttonLockTimer.Interval = 1000;
            this.buttonLockTimer.AutoReset = false;
            this.pluginInterface = pluginInterface;
            this.plugin = plugin;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }


        public void InspectButtonAction()
        {
            // rate limit this button (every 1 second)
            if (!buttonLocked)
            {
                this.buttonLocked = true;
                if (plugin == null) return;
                // inspects 1 player that has not already been inspected
                // returns the number of players in party that have not been inspected
                remainingInspections = plugin.partyMemberService.inspectParty();
                this.buttonLockTimer.Start();
            }
        }

        private void OnButtonLockTimerElapsed(Object? source, ElapsedEventArgs e)
        {
            this.buttonLocked = false;
        }
    }
}
