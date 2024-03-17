using System;

namespace RouletteDataCollector.Structs;

public class DBRoulette
{
    public string id { get; set; } = "";
    public DateTime created { get; set; }
    public DateTime updated { get; set; }
    public string? roulette_type { get; set; }
    public int? territory_id { get; set; }
    public int? content_id { get; set; }
    public DateTime? queue_start { get; set; }
    public DateTime? queue_end { get; set; }
    public DateTime? duty_start { get; set; }
    public DateTime? duty_end { get; set; }
    public string? conclusion { get; set; }
    public int? wipes { get; set; }
    public int? synclevel { get; set; }
}

public class ResolvedRoulette
{
    public string id = "";
    public DateTime created;
    public DateTime updated;
    public string? roulette_type;
    public string? territory;
    public string? content;
    public DateTime? queue_start;
    public DateTime? queue_end;
    public DateTime? duty_start;
    public DateTime? duty_end;
    public string? conclusion;
    public int? wipes;
    public int? synclevel;
}
