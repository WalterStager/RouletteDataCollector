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
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Linq;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace RouletteDataCollector
{
    public sealed class RouletteDataCollector : IDalamudPlugin
    {
        public string Name => "Roulette Data Collector";
        internal const string ConfigCommand = "/prdcconfig";
        internal const string BrowserCommand = "/prdcbrowse";
        internal const string ExamineCommand = "/prdcexamine";
    
        // these are all cleared when exiting instance
        internal Dictionary<string, string> playerToGearset = new Dictionary<string, string>();
        internal string? currentGUID = null;
        internal bool inContent = false;

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
        internal RDCMapper rdcmapper { get; init; }
        internal static ITargetManager? targetManager { get; private set; }
        internal static IObjectTable? objectTable { get; private set; }

        private RDCConfigWindow configWindow { get; init; }
        private RDCBrowserWindow browseWindow { get; init; }
        private RDCExamineWindow examineWindow { get; init; }

        public RouletteDataCollector(
            DalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            IPluginLog log,
            IDataManager dataManager,
            IAddonLifecycle addonLifecycle,
            IDutyState dutyState,
            IPartyList partyList,
            IClientState clientState,
            IGameGui gameGui,
            ITargetManager targetManager,
            IObjectTable objectTable)
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
            RouletteDataCollector.targetManager = targetManager;
            RouletteDataCollector.objectTable = objectTable;
            rdcmapper = new RDCMapper(this);

            this.configuration = this.pluginInterface.GetPluginConfig() as RDCConfig ?? new RDCConfig();
            this.configuration.Initialize(this, this.pluginInterface);

            // add windows
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
            examineWindow = new RDCExamineWindow(this);
            windowSystem.AddWindow(examineWindow);
            this.commandManager.AddHandler(ExamineCommand, new CommandInfo(OnExamineCommand)
            {
                HelpMessage = "Display examine window."
            });
            examineWindow.IsOpen = true;
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

        private void OnExamineCommand(string command, string args)
        {
            examineWindow.IsOpen = !examineWindow.IsOpen;
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

        public unsafe DBGearset getInvContainerIds(InventoryContainer* invContainer)
        {
            List<uint> itemIds = Enumerable.Repeat(0U, 14).ToList();
            List<string?> materiaGuids = Enumerable.Repeat<string?>(null, 14).ToList();
            
            for (int i = 0; i < 14; i++)
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
                    this.log.Verbose($"item {i}=null");
                }

                // if there is at least some materia
                if (materiaIds[0].Item1 != 0)
                {
                    DBMateriaset matSet = rdcmapper.mapper.Map<DBMateriaset>(materiaIds);
                    string matSetGuid = Guid.NewGuid().ToString();
                    matSet.id = matSetGuid;
                    this.databaseService.MateriasetInsert(matSetGuid, matSet);
                    materiaGuids[i] = matSetGuid;
                }
            }
            
            return rdcmapper.mapper.Map<DBGearset>((itemIds, materiaGuids));
        }

        private unsafe bool OnPartyMemberExamine(string playerId, int race, InventoryContainer* invContainer)
        {
            this.log.Info($"Inspecting {playerId}");
            DBGearset gear = getInvContainerIds(invContainer);
            gear.race = race;
            this.databaseService.GearsetGearUpdate(this.playerToGearset[playerId], gear);
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
            string playerUniqueStr = getPlayerUid(newMember);

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

            // add gearset
            if (!this.playerToGearset.ContainsKey(playerUniqueStr))
            {
                this.configuration.remainingInspections = null; 
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
                this.partyMemberService.clearInspectedPlayers();
                this.playerToGearset.Clear();
                this.configuration.remainingInspections = null; 
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
        public static string getPlayerUid(PartyMember player)
        {
            return $"{player.Name}+{player.World.Id}";
        }

        public static string getPlayerUid(PlayerCharacter player)
        {
            return $"{player.Name}+{player.HomeWorld.Id}";
        }
    }
}
