﻿using System;
using System.IO;

using Dalamud.Game.Command;
using Dalamud.Game.Addon.Events;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;

using Dalamud.DrunkenToad.Core;
using Dalamud.DrunkenToad.Core.Models;

using RouletteDataCollector.Windows;
using RouletteDataCollector.Services;

namespace RouletteDataCollector
{
    public sealed class RouletteDataCollector : IDalamudPlugin
    {
        public string Name => "Roulette Data Collector";
        private const string ConfigCommand = "/prdc";
        private string? currentGUID = null;
        private bool inContent = false;

        private ToDoListService toDoListService { get; init; }
        private DatabaseService databaseService { get; init; }
        
        public RDCConfig configuration { get; init; }
        public WindowSystem windowSystem = new("RouletteDataCollectorConfig");
        public IPluginLog log { get; init; }

        private DalamudPluginInterface pluginInterface { get; init; }
        private ICommandManager commandManager { get; init; }
        private IAddonEventManager addonEventManager { get; init; }
        private IAddonLifecycle addonLifecycle { get; init; }
        private RDCConfigWindow configWindow { get; init; }

        public RouletteDataCollector(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager,
            [RequiredVersion("1.0")] IPluginLog log,
            [RequiredVersion("1.0")] IAddonEventManager addonEventManager,
            [RequiredVersion("1.0")] IAddonLifecycle addonLifecycle)
        {
            log.Debug("Start of RouletteDataCollector constructor");
            this.log = log;
            this.pluginInterface = pluginInterface;
            this.commandManager = commandManager;
            this.addonEventManager = addonEventManager;
            this.addonLifecycle = addonLifecycle;

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

            DalamudContext.Initialize(pluginInterface);
            DalamudContext.PlayerLocationManager.LocationStarted += this.OnStartLocation;
            DalamudContext.PlayerLocationManager.LocationEnded += this.OnEndLocation;

            this.toDoListService = new ToDoListService(this, this.addonLifecycle, this.RouletteTypeUpdate);
            this.databaseService = new DatabaseService(this, Path.Combine(this.pluginInterface.GetPluginConfigDirectory(), "database.db"));
            
            this.toDoListService.Start();
            this.databaseService.Start();
            DalamudContext.PlayerLocationManager.Start();

            ToadLocation? startLocation = DalamudContext.PlayerLocationManager.GetCurrentLocation();
            if (startLocation != null)
            {
                this.inContent = startLocation.InContent();
                this.log.Info($"Starting plugin while in content {this.inContent}");
            }
        }

        private void RouletteTypeUpdate(string rouletteType)
        {
            this.currentGUID = Guid.NewGuid().ToString();
            this.databaseService.RouletteInsert(this.currentGUID, rouletteType);
        }

        private unsafe void TestAddonHandler(AddonEventType atkEventType, nint atkUnitBase, nint atkResNode)
        {
            AtkUnitBase* baseUnit = (AtkUnitBase*)atkUnitBase;
            string baseName = "";
            if (baseUnit->Name != null) {
                baseName = System.Text.Encoding.UTF8.GetString(baseUnit->Name, 32);
            }
            this.log.Verbose($"TestAddonHandler enter {atkEventType}, {atkUnitBase:X}, {atkResNode:X}, name {baseName}");
        }

        public void Dispose()
        {
            DalamudContext.PlayerLocationManager.LocationStarted -= this.OnStartLocation;
            DalamudContext.PlayerLocationManager.LocationStarted -= this.OnEndLocation;
            DalamudContext.PlayerLocationManager.Dispose();
            DalamudContext.Dispose();

            this.toDoListService.Stop();
            this.databaseService.Stop();

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

    }
}
