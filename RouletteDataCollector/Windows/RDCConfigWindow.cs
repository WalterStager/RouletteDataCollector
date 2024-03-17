using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace RouletteDataCollector.Windows;

public class RDCConfigWindow : Window, IDisposable
{
    private RouletteDataCollector plugin;
    private RDCConfig configuration;
    private string versionNumber;

    public RDCConfigWindow(RouletteDataCollector plugin) : base(
        $"Roulette Data Collector Config",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(232, 75);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
        this.configuration = plugin.configuration;

        this.versionNumber = Assembly.GetCallingAssembly().VersionNumber().ToString();
    }


    public void Dispose()
    {
        
    }

    private string remainingInspectionsStr()
    {
        if (this.configuration.remainingInspections != null)
        {
            return configuration.remainingInspections.ToString()!;
        }
        else
        {
            return "?";
        }
    }


    public override void Draw()
    {
        // can't ref a property, so use a local copy
        var configValue = this.configuration.enableSaveData;
        
        if (ImGui.Checkbox("Enable saving data", ref configValue))
        {
            this.configuration.enableSaveData = configValue;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Tooltip here.");
        }

        if (ImGui.Button("Save config"))
        {
            this.configuration.Save();
        }
        ImGui.SameLine();

        ImGui.Text($"Plugin version: {versionNumber}");
        if (ImGui.Button("Debug1"))
        {
            this.configuration.OnDebugButton();
        }
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (this.configuration.buttonLocked) ImGui.BeginDisabled();
        if (ImGui.Button("Inspect party member")) 
        {
            this.configuration.InspectButtonAction();
        }
        if (this.configuration.buttonLocked) ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.Text($"Remaining inspections: {remainingInspectionsStr()}");
    }
}
