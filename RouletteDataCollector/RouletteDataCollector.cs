using System;
using System.IO;
using System.Collections.Generic;

using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;

using RouletteDataCollector.Windows;
using RouletteDataCollector.Services;
using RouletteDataCollector.Structs;

namespace RouletteDataCollector
{
    public sealed class RouletteDataCollector : IDalamudPlugin
    {
        public string Name => "Roulette Data Collector";
        private const string ConfigCommand = "/prdc";
        private string? currentGUID = null;
        private bool inContent = false;
        private Dictionary<string, RDCPartyMember> partyMembers = new Dictionary<string, RDCPartyMember>();
        private Dictionary<string, string> playerToGUID = new Dictionary<string, string>();

        private ToDoListService toDoListService { get; init; }
        private DatabaseService databaseService { get; init; }
        private PartyMemberService partyMemberService  { get; init; }
        
        public RDCConfig configuration { get; init; }
        public WindowSystem windowSystem = new("RouletteDataCollectorConfig");
        public IPluginLog log { get; init; }

        private DalamudPluginInterface pluginInterface { get; init; }
        private ICommandManager commandManager { get; init; }
        private IAddonEventManager addonEventManager { get; init; }
        private IAddonLifecycle addonLifecycle { get; init; }
        private IDutyState dutyState { get; init; }
        private IPartyList partyList  { get; init; }

        private RDCConfigWindow configWindow { get; init; }

        public RouletteDataCollector(
            DalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            IPluginLog log,
            IAddonEventManager addonEventManager,
            IAddonLifecycle addonLifecycle,
            IDutyState dutyState,
            IPartyList partyList)
        {
            log.Debug("Start of RouletteDataCollector constructor");
            this.log = log;
            this.pluginInterface = pluginInterface;
            this.commandManager = commandManager;
            this.addonEventManager = addonEventManager;
            this.addonLifecycle = addonLifecycle;
            this.dutyState = dutyState;
            this.partyList = partyList;

            this.configuration = this.pluginInterface.GetPluginConfig() as RDCConfig ?? new RDCConfig();
            this.configuration.Initialize(this.pluginInterface, this.log);
            configWindow = new RDCConfigWindow(this);
            windowSystem.AddWindow(configWindow);
            this.commandManager.AddHandler(ConfigCommand, new CommandInfo(OnConfigCommand)
            {
                HelpMessage = "Display roulette data collector configuration window."
            });
            this.pluginInterface.UiBuilder.Draw += DrawUI;
            this.pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            this.toDoListService = new ToDoListService(this, this.addonLifecycle, this.OnRouletteQueue);
            this.databaseService = new DatabaseService(this, Path.Combine(this.pluginInterface.GetPluginConfigDirectory(), "database.db"));
            this.partyMemberService = new PartyMemberService(this, this.addonLifecycle, this.partyList, this.OnPartyMemberAdded);
            
            DalamudContext.Initialize(pluginInterface);
            DalamudContext.PlayerLocationManager.Start();
            this.toDoListService.Start();
            this.databaseService.Start();
            this.partyMemberService.Start();

            DalamudContext.PlayerLocationManager.LocationStarted += this.OnStartLocation;
            DalamudContext.PlayerLocationManager.LocationEnded += this.OnEndLocation;
            this.dutyState.DutyWiped += this.OnDutyWipe;
            this.dutyState.DutyCompleted += this.OnDutyCompleted;

            ToadLocation? startLocation = DalamudContext.PlayerLocationManager.GetCurrentLocation();
            if (startLocation != null)
            {
                this.inContent = startLocation.InContent();
                this.log.Info($"Starting plugin while in content {this.inContent}");
            }
        }

        public void Dispose()
        {
            DalamudContext.PlayerLocationManager.LocationStarted -= this.OnStartLocation;
            DalamudContext.PlayerLocationManager.LocationStarted -= this.OnEndLocation;
            DalamudContext.PlayerLocationManager.Dispose();
            DalamudContext.Dispose();

            this.dutyState.DutyWiped -= this.OnDutyWipe;
            this.dutyState.DutyCompleted -= this.OnDutyCompleted;

            this.toDoListService.Stop();
            this.databaseService.Stop();
            this.partyMemberService.Stop();

            this.windowSystem.RemoveAllWindows();

            configWindow.Dispose();

            this.commandManager.RemoveHandler(ConfigCommand);
        }

        private void OnConfigCommand(string command, string args)
        {
            configWindow.IsOpen = true;
        }

        private void DrawUI()
        {
            this.windowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            configWindow.IsOpen = true;
        }

        private void OnRouletteQueue(string rouletteType)
        {
            this.currentGUID = Guid.NewGuid().ToString();
            this.databaseService.RouletteInsert(this.currentGUID, rouletteType);
        }

        private void OnPartyMemberAdded(PartyMember newMember)
        {
            if (this.currentGUID == null) 
            {
                this.log.Error($"Didn't detect queue into content correctly on Party Member Added");
                return;
            }
            string playerUniqueStr = getPartyMemberUniqueString(newMember);
            if (this.playerToGUID.ContainsKey(playerUniqueStr))
            {
                return;
            }
            string playerGUID = Guid.NewGuid().ToString();

            this.partyMembers.Add(playerGUID, new RDCPartyMember(newMember.Name.ToString(), newMember.World.Id, 0));
            this.playerToGUID.Add(playerUniqueStr, playerGUID);

            this.databaseService.PlayerInsert(playerGUID, this.partyMembers[playerGUID]);
            this.databaseService.GearsetInsert(Guid.NewGuid().ToString(), playerGUID, this.currentGUID, newMember.ClassJob.Id, newMember.Level);
        }

        private void OnStartLocation(ToadLocation location)
        {
            if (location.InContent())
            {
                if (this.currentGUID == null) {
                    this.log.Error($"Didn't detect queue into content correctly {location.GetName()}");
                    return;
                }
                this.inContent = true;
                this.databaseService.StartLocationUpdate(this.currentGUID, location.TerritoryId, location.ContentId);
            }
        }

        private void OnEndLocation(ToadLocation location)
        {
            if (location.InContent())
            {
                if (this.currentGUID == null) {
                    this.log.Error($"Didn't detect leaving content correctly {location.GetName()}");
                    return;
                }
                this.inContent = false;
                this.databaseService.EndLocationUpdate(this.currentGUID);
                this.currentGUID = null;
            }
        }

        private void OnDutyWipe(object? sender, ushort eventValue) {
            if (this.currentGUID == null) {
                this.log.Error($"Didn't detect queue into content correctly on wipe");
                return;
            }
            this.databaseService.DutyWipeUpdate(this.currentGUID);
        }

        private void OnDutyCompleted(object? sender, ushort eventValue) {
            if (this.currentGUID == null) {
                this.log.Error($"Didn't detect queue into content correctly on completed");
                return;
            }
            this.databaseService.DutySuccessfulUpdate(this.currentGUID);
        }

        // Name+WorldID is as unique an identifier for a player (in the short term) as we can get
        public static string getPartyMemberUniqueString(PartyMember member)
        {
            return $"{member.Name}+{member.World.Id}";
        }
    }
}
