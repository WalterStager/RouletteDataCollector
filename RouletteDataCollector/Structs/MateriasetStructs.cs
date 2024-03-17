using System;
using System.Collections.Generic;

namespace RouletteDataCollector.Structs;


public class DBMateriaset
{
    public string id { get; set; } = "";
    public DateTime created { get; set; }
    public DateTime updated { get; set; }
    public int? materia0_type { get; set; }
    public int? materia1_type { get; set; }
    public int? materia2_type { get; set; }
    public int? materia3_type { get; set; }
    public int? materia4_type { get; set; }
    public int? materia0_grade { get; set; }
    public int? materia1_grade { get; set; }
    public int? materia2_grade { get; set; }
    public int? materia3_grade { get; set; }
    public int? materia4_grade { get; set; }

    public override string ToString()
    {
        return $"DBMateriaset {id} [{materia0_type}-{materia0_grade}, {materia1_type}-{materia1_grade}, {materia2_type}-{materia2_grade}, {materia3_type}-{materia3_grade}, {materia4_type}-{materia4_grade}]";
    }
}

public class ResolvedMateriaset
{
    public string id = "";
    public DateTime created;
    public DateTime updated;
    public string? materia0;
    public string? materia1;
    public string? materia2;
    public string? materia3;
    public string? materia4;
}
