﻿using System;
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
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Linq;
using AutoMapper;
using RouletteDataCollector.Mappings;

namespace RouletteDataCollector
{
    public sealed class RouletteDataCollector : IDalamudPlugin
    {
        public string Name => "Roulette Data Collector";
        private const string ConfigCommand = "/prdcconfig";
        private const string BrowserCommand = "/prdcbrowse";
    
        // these are all cleared when exiting instance
        internal HashSet<string> inspectedPlayers = new HashSet<string>();
        internal Dictionary<string, string> playerToGearset = new Dictionary<string, string>();
        internal string? currentGUID = null;
        internal bool inContent = false;
        private IMapper mapper;

        // saved between instances
        internal HashSet<string> seenPlayers = new HashSet<string>();

        internal ToDoListService toDoListService { get; init; }
        internal DatabaseService databaseService { get; init; }
        internal PartyMemberService partyMemberService  { get; init; }
        
        public RDCConfig configuration { get; init; }
        public WindowSystem windowSystem = new("RouletteDataCollectorConfig");
        public IPluginLog log { get; init; }

        internal DalamudPluginInterface pluginInterface { get; init; }
        internal ICommandManager commandManager { get; init; }
        internal static IDataManager? dataManager { get; private set; }
        internal IAddonLifecycle addonLifecycle { get; init; }
        internal IDutyState dutyState { get; init; }
        internal IPartyList partyList  { get; init; }
        internal IClientState clientState { get; init; }
        internal IGameGui gameGui { get; init; }

        private RDCConfigWindow configWindow { get; init; }
        private RDCBrowserWindow browseWindow;

        public RouletteDataCollector(
            DalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            IPluginLog log,
            IDataManager dataManager,
            IAddonLifecycle addonLifecycle,
            IDutyState dutyState,
            IPartyList partyList,
            IClientState clientState,
            IGameGui gameGui)
        {
            log.Debug("Start of RouletteDataCollector constructor");
            this.log = log;
            this.pluginInterface = pluginInterface;
            this.commandManager = commandManager;
            RouletteDataCollector.dataManager = dataManager;
            this.addonLifecycle = addonLifecycle;
            this.dutyState = dutyState;
            this.partyList = partyList;
            this.clientState = clientState;
            this.gameGui = gameGui;

            this.configuration = this.pluginInterface.GetPluginConfig() as RDCConfig ?? new RDCConfig();
            this.configuration.Initialize(this, this.pluginInterface);

            configWindow = new RDCConfigWindow(this);
            windowSystem.AddWindow(configWindow);
            this.commandManager.AddHandler(ConfigCommand, new CommandInfo(OnConfigCommand)
            {
                HelpMessage = "Display configuration window."
            });
            browseWindow = new RDCBrowserWindow(this);
            windowSystem.AddWindow(browseWindow);
            this.commandManager.AddHandler(BrowserCommand, new CommandInfo(OnBrowseCommand)
            {
                HelpMessage = "Display database browser window. Mostly for debugging."
            });
            this.pluginInterface.UiBuilder.Draw += DrawUI;
            this.pluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            this.toDoListService = new ToDoListService(this, this.addonLifecycle, this.OnRouletteQueue);
            this.databaseService = new DatabaseService(this, Path.Combine(this.pluginInterface.GetPluginConfigDirectory(), "database.db"));
            // find a better way to pass unsafe callback OnPartyMemberExamine
            unsafe
            {
                this.partyMemberService = new PartyMemberService(this, this.addonLifecycle, this.partyList, this.clientState, this.OnPartyMemberAdded, this.OnPartyMemberExamine);
            }
            
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

            MapperConfiguration config = new MapperConfiguration(cfg => {
                cfg.AddProfile<ListsToDBGearset>();
                cfg.AddProfile<ListToDBMateriaset>(); });

            this.mapper = config.CreateMapper();
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
            this.commandManager.RemoveHandler(BrowserCommand);
        }

        private void OnConfigCommand(string command, string args)
        {
            configWindow.IsOpen = true;
        }

        private void OnBrowseCommand(string command, string args)
        {
            browseWindow.IsOpen = true;
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

        private unsafe bool OnPartyMemberExamine(PartyMember member, InventoryContainer* invContainer)
        {
            this.log.Verbose($"Inspecting {member.Name}");
            
            List<uint> itemIds = Enumerable.Repeat(0U, 14).ToList();
            List<string?> materiaGuids = Enumerable.Repeat<string?>(null, 14).ToList();
            
            for (int i = 0; i < 13; i++)
            {
                List<(uint, uint)> materiaIds = Enumerable.Repeat((0U, 0U), 5).ToList();
                InventoryItem* item = invContainer->GetInventorySlot(i);
                if (item != null)
                {
                    itemIds[i] = item->GetItemId();
                    for (byte j = 0; j < item->GetMateriaCount() && j < 5; j++)
                    {
                        materiaIds[j] = ((uint)item->GetMateriaId(j), (uint)item->GetMateriaGrade(j));
                    }
                }
                else
                {
                    this.log.Verbose($"{member.Name} item {i}=null");
                }

                // if there is at least some materia
                if (materiaIds[0].Item1 != 0)
                {
                    
                    DBMateriaset matSet = mapper.Map<DBMateriaset>(materiaIds);
                    string matSetGuid = Guid.NewGuid().ToString();
                    matSet.id = matSetGuid;
                    this.databaseService.MateriasetInsert(matSetGuid, matSet);
                    materiaGuids[i] = matSetGuid;
                }
            }

            DBGearset gear = mapper.Map<DBGearset>((itemIds, materiaGuids));
            this.databaseService.GearsetGearUpdate(this.playerToGearset[getPartyMemberUniqueString(member)], gear);
            return true;
        }

        private void OnPartyMemberAdded(PartyMember newMember)
        {
            if (this.currentGUID == null) 
            {
                this.log.Warning($"Didn't detect queue into content correctly on Party Member Added");
                return;
            }
            // should i just use this unique str as the key instead of guid? IDK
            string playerUniqueStr = getPartyMemberUniqueString(newMember);

            if (!this.seenPlayers.Contains(playerUniqueStr))
            {
                this.seenPlayers.Add(playerUniqueStr);
                DBPlayer player = new DBPlayer();
                player.id = playerUniqueStr;
                player.name = $"{newMember.Name}";
                player.homeworld = (int?)newMember.World.Id;
                player.collector = false;
                this.databaseService.PlayerInsert(playerUniqueStr, player);
            }

            this.configuration.remainingInspections = null; 

            // add gearset
            if (!this.playerToGearset.ContainsKey(playerUniqueStr))
            {
                string gearsetGUID = Guid.NewGuid().ToString();
                this.playerToGearset.Add(playerUniqueStr, gearsetGUID);
                this.databaseService.GearsetInsert(gearsetGUID, playerUniqueStr, this.currentGUID, newMember.ClassJob.Id, newMember.Level);
            }
        }

        private void OnStartLocation(ToadLocation location)
        {
            if (location.InContent())
            {
                if (this.currentGUID == null) {
                    this.log.Warning($"Didn't detect queue into content correctly {location.GetName()}");
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
                    this.log.Warning($"Didn't detect leaving content correctly {location.GetName()}");
                    return;
                }
                this.inContent = false;
                this.databaseService.EndLocationUpdate(this.currentGUID);
                this.currentGUID = null;
                this.inspectedPlayers.Clear();
                this.playerToGearset.Clear();
            }
        }

        private void OnDutyWipe(object? sender, ushort eventValue) {
            if (this.currentGUID == null) {
                this.log.Warning($"Didn't detect queue into content correctly on wipe");
                return;
            }
            this.databaseService.DutyWipeUpdate(this.currentGUID);
        }

        private void OnDutyCompleted(object? sender, ushort eventValue) {
            if (this.currentGUID == null) {
                this.log.Warning($"Didn't detect queue into content correctly on completed");
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
