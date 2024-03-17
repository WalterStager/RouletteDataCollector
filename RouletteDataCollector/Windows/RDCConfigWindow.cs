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

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        bool enableSaveData = this.configuration.enableSaveData;
        bool lockExamineWindow = this.configuration.lockExamineWindow;

        ImGui.Text($"Plugin version: {versionNumber}");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("General configuration: (?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"Use {RouletteDataCollector.ConfigCommand} to open this window.");
        }
        if (ImGui.Checkbox("Record data", ref enableSaveData))
        {
            this.configuration.enableSaveData = enableSaveData;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Examine window options: (?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"Use {RouletteDataCollector.ExamineCommand} to open or close the window.");
        }
        if (ImGui.Checkbox("Lock position", ref lockExamineWindow))
        {
            this.configuration.lockExamineWindow = lockExamineWindow;
        }
        ImGui.SameLine();

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Database browser Window options: (?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"Use {RouletteDataCollector.BrowserCommand} to open the window.");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Save config"))
        {
            this.configuration.Save();
        }
    }
}
