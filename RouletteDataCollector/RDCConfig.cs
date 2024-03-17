using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

using Dalamud.Plugin.Services;
using System.Timers;
using System.Collections;
using System.Collections.Generic;
using RouletteDataCollector.Structs;
using RouletteDataCollector.Mappings;
using AutoMapper;
using System.Linq;
using RouletteDataCollector.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Game.ClientState.Party;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.DrunkenToad.Extensions;

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
            this.buttonLockTimer.Interval = 2000;
            this.buttonLockTimer.AutoReset = false;
            this.pluginInterface = pluginInterface;
            this.plugin = plugin;

        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }

        public unsafe void OnDebugButton()
        {

        }

        public void InspectButtonAction()
        {
            // rate limit this button (every 1 second)
            if (!buttonLocked)
            {
                this.buttonLocked = true;
                if (plugin == null) return;
                // do nothing if not in content
                if (!this.plugin.inContent) return;
                // inspects 1 player that has not already been inspected
                // returns the number of players that have not been inspected
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
