using System;
using System.IO;
using System.Data.SQLite;

using Dapper;

using Dalamud.Game.Command;
using Dalamud.Game.Addon.Events;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes; 
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;

using RouletteDataCollector.Windows;

namespace RouletteDataCollector
{
    public sealed class RouletteDataCollector : IDalamudPlugin
    {
        public string Name => "Roulette Data Collector";
        private const string ConfigCommand = "/prdc";
        private string? currentGUID = null;
        private bool inContent = false;

        public RDCConfig Configuration { get; init; }
        public WindowSystem WindowSystem = new("RouletteDataCollectorConfig");

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        private IAddonEventManager AddonEventManager { get; init; }
        private IAddonLifecycle AddonLifecycle { get; init; }
        private RDCConfigWindow ConfigWindow { get; init; }
        private IPluginLog log { get; init; }
        private SQLiteConnection sqconn { get; init; }

        public RouletteDataCollector(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager,
            [RequiredVersion("1.0")] IPluginLog log,
            [RequiredVersion("1.0")] IAddonEventManager addonEventManager,
            [RequiredVersion("1.0")] IAddonLifecycle addonLifecycle)
        {
            log.Debug("Start of RDC constructor");
            this.log = log;
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.AddonEventManager = addonEventManager;
            this.AddonLifecycle = addonLifecycle;

            DalamudContext.Initialize(pluginInterface);

            this.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_ToDoList", this.toDoListEvents);

            this.Configuration = this.PluginInterface.GetPluginConfig() as RDCConfig ?? new RDCConfig();
            this.Configuration.Initialize(this.PluginInterface, this.log);

            string DatabaseFilePath = Path.Combine(this.PluginInterface.GetPluginConfigDirectory(), "database.db");
            this.log.Info($"DatabaseFilePath={DatabaseFilePath}", DatabaseFilePath);
            this.sqconn = new SQLiteConnection($"Data Source={DatabaseFilePath};Version=3;New=True;");
            this.InitializeDatabase();

            DalamudContext.PlayerLocationManager.LocationStarted += this.OnStartLocation;
            DalamudContext.PlayerLocationManager.LocationEnded += this.OnEndLocation;

            ConfigWindow = new RDCConfigWindow(this);

            WindowSystem.AddWindow(ConfigWindow);

            this.CommandManager.AddHandler(ConfigCommand, new CommandInfo(OnConfigCommand)
            {
                HelpMessage = "Display roulette data collector configuration window."
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            DalamudContext.PlayerLocationManager.Start();

            ToadLocation? startLocation = DalamudContext.PlayerLocationManager.GetCurrentLocation();
            if (startLocation != null)
            {
                this.inContent = startLocation.InContent();
                this.log.Info($"Detected initialized while in content {this.inContent}");
            }
        }

        // can return null
        private unsafe AtkResNode* searchNodeListForID(AtkResNode* parent, uint id)
        {
            AtkUldManager manager = parent->GetAsAtkComponentNode()->Component->UldManager;
            for (int i = 0; i < manager.NodeListCount; i++)
            {
                if (manager.NodeList[i]->NodeID == id)
                {
                    return manager.NodeList[i];
                }
            }
            return null;
        }

        // can return null
        private unsafe AtkResNode* searchLinkedListForID(AtkResNode* parent, uint id)
        {
            AtkResNode* child = parent->ChildNode;
            while (child != null)
            {
                if (child->NodeID == id)
                {
                    break;
                }
                else
                {
                    child = child->PrevSiblingNode;
                }
            }
            return child;
        }

        private unsafe void toDoListEvents(AddonEvent type, AddonArgs args)
        {
            AtkUnitBase* toDoListBase = (AtkUnitBase*)args.Addon;
            if (toDoListBase == null)
            {
                this.log.Verbose("null toDoListBase AtkUnitBase*");
                return;
            }
            AtkResNode* toDoListRootNode = toDoListBase->GetRootNode();
            if (toDoListRootNode == null)
            {
                this.log.Verbose("null toDoListRootNode AtkResNode*");
                return;
            }
            AtkResNode* dutyInfoNode = searchLinkedListForID(toDoListRootNode, 20001);
            if (dutyInfoNode == null)
            {
                this.log.Verbose("null dutyInfoNode AtkResNode*");
                return;
            }
            AtkTextNode* dutyInfoTextNode = (AtkTextNode*)searchNodeListForID(dutyInfoNode, 6);
            if (dutyInfoTextNode == null)
            {
                this.log.Verbose("null dutyInfoTextNode AtkResNode*");
                return;
            }
            AtkResNode* queueTimeNode = searchLinkedListForID(toDoListRootNode, 20004);
            if (queueTimeNode == null)
            {
                this.log.Verbose("null queueTimeNode AtkResNode*");
                return;
            }
            AtkTextNode* queueTimeTextNode = (AtkTextNode*)searchNodeListForID(queueTimeNode, 6);
            if (queueTimeTextNode == null)
            {
                this.log.Verbose("null queueTimeTextNode AtkTextNode*");
                return;
            }

            // Normal duties use 20004 and Roulettes use 20005
            if (!queueTimeTextNode->NodeText.ToString().Contains("Time Elapsed"))
            {
                queueTimeNode = searchLinkedListForID(toDoListRootNode, 20005);
                if (queueTimeNode == null)
                {
                    this.log.Verbose("null queueTimeNode AtkResNode*");
                    return;
                }
                queueTimeTextNode = (AtkTextNode*)searchNodeListForID(queueTimeNode, 6);
                if (queueTimeTextNode == null)
                {
                    this.log.Verbose("null queueTimeTextNode AtkTextNode*");
                    return;
                }
            }
            
            if (queueTimeTextNode->NodeText.ToString().Contains("Time Elapsed: 0:00/Average Wait Time: More than 30m")) {
                this.currentGUID = Guid.NewGuid().ToString();
                string timestamp = GetTimestamp();
                string rouletteType = dutyInfoTextNode->NodeText.ToString();
                var data = new {Guid = this.currentGUID, Created = timestamp, Updated = timestamp, QueueStart = timestamp, RouletteType=rouletteType};
                
                this.log.Info($"Roulettes INSERT {data}");
                this.sqconn.Execute("INSERT INTO Roulettes (id, created, updated, queue_start, roulette_type) VALUES (@Guid, @Created, @Updated, @QueueStart, @RouletteType)", data);
            }
        }

        private unsafe void testAddonHandler(AddonEventType atkEventType, nint atkUnitBase, nint atkResNode)
        {
            AtkUnitBase* baseUnit = (AtkUnitBase*)atkUnitBase;
            string baseName = "";
            if (baseUnit->Name != null) {
                baseName = System.Text.Encoding.UTF8.GetString(baseUnit->Name, 32);
            }
            this.log.Verbose($"testAddonHandler enter {atkEventType}, {atkUnitBase:X}, {atkResNode:X}, name {baseName}");

        }

        private void InitializeDatabase()
        {
            const string create_tables = @"
                CREATE TABLE IF NOT EXISTS Roulettes (
                    id                  TEXT NOT NULL PRIMARY KEY,
                    created             NUMERIC NOT NULL,
                    updated             NUMERIC NOT NULL,
                    roulette_type       TEXT,
                    territory_id        INT,
                    content_id          INT,
                    queue_start         NUMERIC,
                    queue_end           NUMERIC,
                    duty_start          NUMERIC,
                    duty_end            NUMERIC,
                    conclusion          TEXT,
                    wipes               INT,
                    synclevel           INT
                );
                CREATE TABLE IF NOT EXISTS Players (
                    id                  TEXT NOT NULL PRIMARY KEY,
                    created             NUMERIC NOT NULL,
                    updated             NUMERIC NOT NULL,
                    name                TEXT,
                    homeworld           TEXT,
                    lodestone_id        INT,
                    collecter           INT
                );
                CREATE TABLE IF NOT EXISTS Gearsets (
                    id                  TEXT NOT NULL PRIMARY KEY,
                    created             NUMERIC NOT NULL,
                    updated             NUMERIC NOT NULL,
                    player              TEXT NOT NULL REFERENCES Players (id),
                    roulette            TEXT NOT NULL REFERENCES Roulettes (id),
                    item_level          INT,
                    job                 TEXT,
                    race                TEXT,
                    level               INT,
                    weapon              TEXT,
                    offhand             TEXT,
                    head                TEXT,
                    body                TEXT,
                    hands               TEXT,
                    legs                TEXT,
                    feet                TEXT,
                    ears                TEXT,
                    neck                TEXT,
                    wrists              TEXT,
                    ring1               TEXT,
                    ring2               TEXT,
                    materia_weapon      TEXT,
                    materia_offhand     TEXT,
                    materia_head        TEXT,
                    materia_body        TEXT,
                    materia_hands       TEXT,
                    materia_legs        TEXT,
                    materia_feet        TEXT,
                    materia_ears        TEXT,
                    materia_neck        TEXT,
                    materia_wrists      TEXT,
                    materia_ring1       TEXT,
                    materia_ring2       TEXT
                );";

            this.sqconn.Execute(create_tables);
        }

        public void Dispose()
        {
            this.sqconn.Close();

            DalamudContext.PlayerLocationManager.LocationStarted -= this.OnStartLocation;
            DalamudContext.PlayerLocationManager.LocationStarted -= this.OnEndLocation;

            this.WindowSystem.RemoveAllWindows();

            ConfigWindow.Dispose();

            this.CommandManager.RemoveHandler(ConfigCommand);
        }

        private void OnConfigCommand(string command, string args)
        {
            ConfigWindow.IsOpen = true;
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
        
        public static string GetTimestamp()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss.ssss");
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
                string timestamp = GetTimestamp();
                this.log.Info($"UPDATE {this.currentGUID} updated = '{timestamp}', queue_end = '{timestamp}', duty_start = '{timestamp}', territory_id = {location.TerritoryId}, content_id = {location.ContentId}");
                this.sqconn.Execute($@"
                    UPDATE Roulettes
                    SET updated = '{timestamp}', queue_end = '{timestamp}', duty_start = '{timestamp}', territory_id = {location.TerritoryId}, content_id = {location.ContentId}
                    WHERE id = '{this.currentGUID}'");
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
                string timestamp = GetTimestamp();
                this.log.Info($"UPDATE {this.currentGUID} updated = '{timestamp}', duty_end = '{timestamp}'");
                this.sqconn.Execute($@"
                    UPDATE Roulettes
                    SET updated = '{timestamp}', duty_end = '{timestamp}'
                    WHERE id = '{this.currentGUID}'");
                this.currentGUID = null;
            }
        }

    }
}
