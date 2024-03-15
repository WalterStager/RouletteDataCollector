using System;
using System.Numerics;
using System.Reflection;
using Dalamud.DrunkenToad.Extensions;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace RouletteDataCollector.Windows;

public class RDCConfigWindow : Window, IDisposable
{
    private RDCConfig configuration;
    private string versionNumber;

    public RDCConfigWindow(RouletteDataCollector plugin) : base(
        $"Roulette Data Collector Config",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(232, 75);
        this.SizeCondition = ImGuiCond.FirstUseEver;

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
#pragma warning disable CS8603 // Possible null reference return.
            return configuration.remainingInspections.ToString();
#pragma warning restore CS8603 // Possible null reference return.
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
        ImGui.Text($"Plugin version: {versionNumber}");
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
