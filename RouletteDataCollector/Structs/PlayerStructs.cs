using System;

namespace RouletteDataCollector.Structs;


public class DBPlayer
{
    public string id { get; set; } = "";
    public DateTime created { get; set; }
    public DateTime updated { get; set; }
    public string? name { get; set; }
    public int? homeworld { get; set; }
    public int? lodestone_id { get; set; }
    public bool? collector { get; set; }
}

public class ResolvedPlayer
{
    public string id = "";
    public DateTime created;
    public DateTime updated;
    public string? name;
    public string? homeworld;
    public int? lodestone_id;
    public bool? collector;
}
