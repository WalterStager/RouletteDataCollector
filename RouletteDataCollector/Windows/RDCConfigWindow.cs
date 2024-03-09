using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace RouletteDataCollector.Windows;

public class RDCConfigWindow : Window, IDisposable
{
    private RDCConfig Configuration;

    public RDCConfigWindow(RouletteDataCollector plugin) : base(
        "Roulette Data Collector Config",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(232, 75);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.Configuration = plugin.Configuration;
    }


    public void Dispose() { }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        var configValue = this.Configuration.EnableSaveData;

        
        if (ImGui.Checkbox("Enable saving data", ref configValue))
        {
            this.Configuration.EnableSaveData = configValue;
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Tooltip here.");
        }

        if (ImGui.Button("Save config"))
        {
            this.Configuration.Save();
        }
    }
}
