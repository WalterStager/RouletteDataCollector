using System;
using System.Numerics;
using System.Reflection;
using System.Linq;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using RouletteDataCollector.Structs;
using System.Collections.Generic;
using AutoMapper;
using RouletteDataCollector.Mappings;

namespace RouletteDataCollector.Windows;

public class RDCExamineWindow : Window, IDisposable
{
    private RouletteDataCollector plugin;

    private IEnumerable<object>? dbTableData = null;

    public RDCExamineWindow(RouletteDataCollector plugin) : base(
        $"Roulette Data Collector Examine window",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize)
    {
        this.plugin = plugin;
    }

    public void Dispose()
    {
        
    }

    private string remainingInspectionsStr()
    {
        if (this.plugin.configuration.remainingInspections != null)
        {
            return this.plugin.configuration.remainingInspections.ToString()!;
        }
        else
        {
            return "?";
        }
    }

    public override void Draw()
    {
        if (this.plugin.configuration.lockExamineWindow)
        {
            this.Flags |= ImGuiWindowFlags.NoMove ;
        }
        else
        {
            this.Flags &= ~(ImGuiWindowFlags.NoMove);
        }

        if (this.plugin.configuration.buttonLocked) ImGui.BeginDisabled();
        if (ImGui.Button("Inspect party member")) 
        {
            this.plugin.configuration.InspectButtonAction();
        }
        if (this.plugin.configuration.buttonLocked) ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.Text($"Remaining inspections: {remainingInspectionsStr()}");
    }
}
