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

public class RDCBrowserWindow : Window, IDisposable
{
    private RouletteDataCollector plugin;
    private string[]? dbTableNames;
    private int[]? dbTableValues;
    private int comboIndex = -1;
    private int dbPageIndex = 0;

    private IEnumerable<object>? dbTableData = null;
    private IMapper mapper;

    public RDCBrowserWindow(RouletteDataCollector plugin) : base(
        $"Roulette Data Collector DB Browser",
        ImGuiWindowFlags.HorizontalScrollbar)
    {
        this.Size = new Vector2(232, 75);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.plugin = plugin;

        this.dbTableNames = Enum.GetNames(typeof(RDCDatabaseTable));
        this.dbTableValues = (int[]?)Enum.GetValues(typeof(RDCDatabaseTable));

        MapperConfiguration config = new MapperConfiguration(cfg => {
            cfg.AddProfile<DBGearsetToResolvedGearset>();
            cfg.AddProfile<DBPlayerToResolvedPlayer>();
            cfg.AddProfile<DBMateriasetToResolvedMateriaset>();
            cfg.AddProfile<DBRouletteToResolvedRoulette>(); }); 

        this.mapper = config.CreateMapper();
    }


    public void Dispose()
    {
        
    }

    private void getDBTable()
    {
        if (this.plugin == null) return;

        if (this.comboIndex == -1)
        {
            // this.plugin?.log.Info("Unset dbTableData");
            this.dbTableData = null;
        }
        else
        {
            this.plugin?.log.Info("Set dbTableData");

            switch ((RDCDatabaseTable)this.comboIndex)
            {
                case RDCDatabaseTable.Gearsets:
                    this.dbTableData = this.plugin?.databaseService.QueryRecentlyUpdated<DBGearset>((RDCDatabaseTable)this.comboIndex, 10, (uint)(10 * dbPageIndex)).Select<DBGearset, ResolvedGearset>(element => mapper.Map<ResolvedGearset>(element)).Cast<object>();
                    break;
                case RDCDatabaseTable.Roulettes:
                    this.dbTableData = this.plugin?.databaseService.QueryRecentlyUpdated<DBRoulette>((RDCDatabaseTable)this.comboIndex, 10, (uint)(10 * dbPageIndex)).Select(element => mapper.Map<ResolvedRoulette>(element)).Cast<object>();
                    break;
                case RDCDatabaseTable.Players:
                    this.dbTableData = this.plugin?.databaseService.QueryRecentlyUpdated<DBPlayer>((RDCDatabaseTable)this.comboIndex, 10, (uint)(10 * dbPageIndex)).Select(element => mapper.Map<ResolvedPlayer>(element)).Cast<object>();
                    break;
                case RDCDatabaseTable.Materiasets:
                    this.dbTableData = this.plugin?.databaseService.QueryRecentlyUpdated<DBMateriaset>((RDCDatabaseTable)this.comboIndex, 10, (uint)(10 * dbPageIndex)).Select(element => mapper.Map<ResolvedMateriaset>(element)).Cast<object>();
                    break;
                default: break;
            }
            
        }
    }

    private void BuildDatabaseTable(IEnumerable<object> dbData)
    {
        if (dbData.Count() <= 0)
            return;
        object firstRow = dbData.First();
        Type dbRowType = firstRow.GetType();

        ImGui.BeginTable("dbData", dbRowType.GetFields().Length, ImGuiTableFlags.Hideable | ImGuiTableFlags.Resizable | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);

        foreach (FieldInfo element in dbRowType.GetFields())
            ImGui.TableSetupColumn(element.Name);

        ImGui.TableHeadersRow();

        int row = 0;
        foreach (object dbRow in dbData)
        {
            ImGui.TableNextRow();
            int col = 0;
            foreach (FieldInfo element in dbRowType.GetFields())
            {
                ImGui.TableSetColumnIndex(col);
                ImGui.TextUnformatted($"{element.GetValue(dbRow)}");
                col++;
            }
            row++;
        }
        ImGui.EndTable();
    }

    public override void Draw()
    {
        if (dbTableNames == null || dbTableValues == null) return;

        string defaultComboItem = comboIndex == -1 ? "None" : dbTableNames[comboIndex];
        ImGui.PushItemWidth(300);
        if (ImGui.BeginCombo("Select database table", defaultComboItem))
        {
            if (ImGui.Selectable("None", comboIndex == -1))
            {
                comboIndex = -1;
                this.dbPageIndex = 0;
                getDBTable();
            }

            for (int i = 0; i < dbTableNames.Length; i++)
            {
                bool isSelected = comboIndex == i;
                if (ImGui.Selectable(dbTableNames[i], isSelected))
                {
                    comboIndex = i;
                    this.dbPageIndex = 0;
                    getDBTable();
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }
        ImGui.SameLine();
        if (ImGui.ArrowButton("##left", ImGuiDir.Left))
        {
            if (this.comboIndex != -1)
            {
                this.dbPageIndex -= dbPageIndex >= 1 ? 1 : 0;
                getDBTable();
            }
        }
        ImGui.SameLine();
        if (ImGui.ArrowButton("##right", ImGuiDir.Right))
        {
            if (this.comboIndex != -1)
            {
                this.dbPageIndex++;
                getDBTable();
            }
        }
        ImGui.SameLine();
        ImGui.Text($"Page {this.dbPageIndex}");

        if (this.dbTableData != null)
        {
            BuildDatabaseTable(this.dbTableData.Cast<object>());
        }
    }
}
