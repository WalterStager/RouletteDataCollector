using System;
using System.IO;
using System.Data.SQLite;

using Dapper;

using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;

using RouletteDataCollector.Windows;

namespace RouletteDataCollector
{
    public sealed class RouletteDataCollector : IDalamudPlugin
    {
        public string Name => "Roulette Data Collector";
        private const string ConfigCommand = "/prdc";

        public RDCConfig Configuration { get; init; }
        public WindowSystem WindowSystem = new("RouletteDataCollectorConfig");

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        private RDCConfigWindow ConfigWindow { get; init; }
        private IPluginLog log { get; init; }
        private SQLiteConnection sqconn { get; init; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public RouletteDataCollector(
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager,
            [RequiredVersion("1.0")] IPluginLog log)
        {
            log.Debug("Start of RDC constructor");
            this.log = log;
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            // this.ClientState = clientState;

            if (!DalamudContext.Initialize(pluginInterface))
            {
                this.log.Error("Drunken Toad failed to initialize properly.");
                return;
            }

            this.Configuration = this.PluginInterface.GetPluginConfig() as RDCConfig ?? new RDCConfig();
            this.Configuration.Initialize(this.PluginInterface);

            string DatabaseFilePath = Path.Combine(this.PluginInterface.GetPluginConfigDirectory(), "database.db");
            this.log.Verbose($"DatabaseFilePath={DatabaseFilePath}", DatabaseFilePath);
            this.sqconn = new SQLiteConnection($"Data Source={DatabaseFilePath};Version=3;New=True;");
            this.InitializeDatabase();

            DalamudContext.PlayerLocationManager.LocationStarted += this.OnStartLocation;

            ConfigWindow = new RDCConfigWindow(this);

            WindowSystem.AddWindow(ConfigWindow);

            this.CommandManager.AddHandler(ConfigCommand, new CommandInfo(OnConfigCommand)
            {
                HelpMessage = "Display roulette data collector configuration window."
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            DalamudContext.PlayerLocationManager.Start();
        }

        private void InitializeDatabase()
        {
            const string create_tables = @"
            CREATE TABLE IF NOT EXISTS RouletteData (
                id          TEXT NOT NULL,
                created     NUMERIC NOT NULL,
                updated     NUMERIC NOT NULL,
                duty        TEXT
            )";

            this.sqconn.Execute(create_tables);
        }

        public void Dispose()
        {
            this.sqconn.Close();

            DalamudContext.PlayerLocationManager.LocationStarted -= this.OnStartLocation;

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


        private void OnStartLocation(ToadLocation location)
        {
            this.log.Verbose("OnStartLocation entered");
            if (location.InContent())
            {
                this.log.Verbose("OnStartLocation inContent true");
                string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd hh:mm:ss.ssss");
                this.log.Verbose($"OnStartLocation timestamp {timestamp}");

                var data = new {Guid = Guid.NewGuid().ToString(), Created = timestamp, Updated = timestamp, Location = location.GetName() };
                var res = this.sqconn.Execute("INSERT INTO RouletteData (id, created, updated, duty) VALUES (@Guid, @Created, @Updated, @Location)", data);
                this.log.Verbose($"OnStartLocation execute res {res}");
            }
        }

    }
}
