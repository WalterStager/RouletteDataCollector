
using System;
using Dalamud.DrunkenToad.Extensions;
using Lumina.Excel.GeneratedSheets;

namespace RouletteDataCollector.Mappings;


public class RDCResolvers
{
    public static string? resolveJob(int? jobId)
    {
        if (jobId == null || jobId == 0) return null;
        return RouletteDataCollector.dataManager?.GetExcelSheet<ClassJob>()?.GetRow((uint)jobId)?.Abbreviation!;
    }

    public static string? resolveItem(int? itemId)
    {
        if (itemId == null || itemId == 0) return null;
        return RouletteDataCollector.dataManager?.GetExcelSheet<Item>()?.GetRow((uint)itemId)?.Name!;
    }

    public static string? resolveMateria(int? materiaType, int? materiaGrade)
    {
        if (materiaType == null || materiaType == 0) return null;
        if (materiaGrade == null || materiaGrade == 0) return null;
        return RouletteDataCollector.dataManager?.GetExcelSheet<Materia>()?.GetRow((uint)materiaType)?.Item[(int)materiaGrade]?.Value?.Name!;
    }

    public static string? resolveWorld(int? worldId)
    {
        if (worldId == null || worldId == 0) return null;
         return RouletteDataCollector.dataManager?.GetExcelSheet<World>()?.GetRow((uint)worldId)?.Name!;
    }

    public static string? resolveTerritory(int? territoryId)
    {
        if (territoryId == null || territoryId == 0) return null;
         return RouletteDataCollector.dataManager?.GetExcelSheet<TerritoryType>()?.GetRow((uint)territoryId)?.Name!;
    }

    public static string? resolveContent(int? contentId)
    {
        if (contentId == null || contentId == 0) return null;
         return RouletteDataCollector.dataManager?.GetExcelSheet<ContentFinderCondition>()?.GetRow((uint)contentId)?.Name!;
    }

    public static DateTime? utcToLocal(DateTime? utcTime)
    {
        return utcTime?.ToLocalTime();
    }

    public static int? nullIfZero(int input)
    {
        return input == 0 ? null : input;
    }
}
