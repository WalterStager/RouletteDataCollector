using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace RouletteDataCollector.Windows;

public class RDCConfigWindow : Window, IDisposable
{
    private RDCConfig configuration;

    public RDCConfigWindow(RouletteDataCollector plugin) : base(
        "Roulette Data Collector Config",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(232, 75);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.configuration = plugin.configuration;
    }


    public void Dispose()
    {
        
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

        if (ImGui.Button("Debug Button 1"))
        {
            this.configuration.DebugButtonAction();
        }
    }
}
