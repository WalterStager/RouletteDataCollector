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

namespace RouletteDataCollector
{
    public sealed class RouletteDataCollector : IDalamudPlugin
    {
        public string Name => "Roulette Data Collector";
        private const string ConfigCommand = "/prdc";
    
        // these are all cleared when exiting instance
        internal HashSet<string> inspectedPlayers = new HashSet<string>();
        internal Dictionary<string, string> playerToGearset = new Dictionary<string, string>();
        internal string? currentGUID = null;
        internal bool inContent = false;

        // saved between instances
        internal Dictionary<string, string> playerToGUID = new Dictionary<string, string>();

        internal ToDoListService toDoListService { get; init; }
        internal DatabaseService databaseService { get; init; }
        internal PartyMemberService partyMemberService  { get; init; }
        
        public RDCConfig configuration { get; init; }
        public WindowSystem windowSystem = new("RouletteDataCollectorConfig");
        public IPluginLog log { get; init; }

        internal DalamudPluginInterface pluginInterface { get; init; }
        internal ICommandManager commandManager { get; init; }
        internal IAddonEventManager addonEventManager { get; init; }
        internal IAddonLifecycle addonLifecycle { get; init; }
        internal IDutyState dutyState { get; init; }
        internal IPartyList partyList  { get; init; }
        internal IClientState clientState { get; init; }
        internal IGameGui gameGui { get; init; }

        private RDCConfigWindow configWindow { get; init; }

        public RouletteDataCollector(
            DalamudPluginInterface pluginInterface,
            ICommandManager commandManager,
            IPluginLog log,
            IAddonEventManager addonEventManager,
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
            this.addonEventManager = addonEventManager;
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
                HelpMessage = "Display roulette data collector configuration window."
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

        private unsafe bool OnPartyMemberExamine(PartyMember member, InventoryContainer* invContainer)
        {
            this.log.Verbose($"Inspecting {member.Name}");
            // save created materia sets to link them to the gearset
            List<string?> materiaSetGuids = new List<string?>(new string?[12]);
            RDCGearset gear = new RDCGearset();
            for (int i = 0; i < invContainer->Size; i++)
            {
                // skip belt slot
                if (i == 5) continue;

                // i = item slot index with belt
                // iAdjusted = item slot index without belt (for RDC structs)
                int iAdjusted = i > 5 ? i-1 : i;
                
                InventoryItem* item = invContainer->GetInventorySlot(i);
                if (item != null)
                {
                    if (i == (int)RDCGearSlot.Weapon && item->GetItemId() == 0)
                    {
                        this.log.Verbose($"{member.Name} weapon is null, didn't get inventory container correctly.");
                        return false;
                    }
                    
                    // soulstone slot vs other slots
                    if (i == 13)
                    {
                        gear.soulstone = item->GetItemId();
                    }
                    else 
                    {
                        gear.items[iAdjusted] = item->GetItemId();

                        // create materia set
                        RDCMateriaset materiaSet = new RDCMateriaset();
                        bool gotMateria = false;
                        for (byte j = 0; j < item->GetMateriaCount() && j < 5; j++)
                        {
                            gotMateria |=  item->GetMateriaGrade(j) != 0;
                            materiaSet.materiaTypes[j] = item->GetMateriaId(j);
                            materiaSet.materiaGrades[j] = item->GetMateriaGrade(j);
                        }

                        // skip creating if there is no materia
                        if (gotMateria)
                        {
                            gear.materia[iAdjusted] = materiaSet;

                            string matGuid = Guid.NewGuid().ToString();
                            materiaSetGuids[iAdjusted] = matGuid;
                            this.databaseService.MateriasetInsert(matGuid, materiaSet);
                        }
                    }

                }
                else
                {
                    this.log.Verbose($"{member.Name} item {i}=null");
                }
            }

            this.databaseService.GearsetGearUpdate(this.playerToGearset[getPartyMemberUniqueString(member)], gear, materiaSetGuids);
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

            // add player if not already detected
            string playerGUID;
            if (this.playerToGUID.ContainsKey(playerUniqueStr))
            {
                playerGUID = this.playerToGUID[playerUniqueStr];
            }
            else
            {
                playerGUID = Guid.NewGuid().ToString();
                this.playerToGUID.Add(playerUniqueStr, playerGUID);
                this.databaseService.PlayerInsert(playerGUID, new RDCPartyMember(newMember.Name.ToString(), newMember.World.Id, 0));
            }

            this.configuration.remainingInspections = null; 

            // add gearset
            string gearsetGUID = Guid.NewGuid().ToString();
            this.playerToGearset.Add(playerUniqueStr, gearsetGUID);
            this.databaseService.GearsetInsert(gearsetGUID, playerGUID, this.currentGUID, newMember.ClassJob.Id, newMember.Level);
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
