using System;
using RouletteDataCollector.Structs;

namespace RouletteDataCollector.Structs;

public class DBGearset
{
    public string id { get; set; } = "";
    public DateTime created { get; set; }
    public DateTime updated { get; set; }
    public string player { get; set; } = "";
    public string roulette { get; set; } = "";
    public int? item_level { get; set; }
    public int? job { get; set; }
    public int? race { get; set; }
    public int? level { get; set; }
    public bool? left_early { get; set; }
    public int? weapon { get; set; }
    public int? offhand { get; set; }
    public int? head { get; set; }
    public int? body { get; set; }
    public int? hands { get; set; } 
    public int? legs { get; set; }
    public int? feet { get; set; }
    public int? ears { get; set; }
    public int? neck { get; set; }
    public int? wrists { get; set; }
    public int? ring1 { get; set; }
    public int? ring2 { get; set; }
    public int? soulstone { get; set; }
    public string? materia_weapon { get; set; }
    public string? materia_offhand { get; set; }
    public string? materia_head { get; set; }
    public string? materia_body { get; set; }
    public string? materia_hands { get; set; }
    public string? materia_legs { get; set; }
    public string? materia_feet { get; set; }
    public string? materia_ears { get; set; }
    public string? materia_neck { get; set; }
    public string? materia_wrists { get; set; }
    public string? materia_ring1 { get; set; }
    public string? materia_ring2 { get; set; }

    public override string ToString()
    {
        return @$"
            DBGearset
                id      {id}
                player  {player}
                race    {race}
                weapon  {weapon}
                head    {head}
                body    {body}
                soulst  {soulstone}";
    }
}

public class ResolvedGearset
{
    public string id = "";
    public DateTime created;
    public DateTime updated;
    public string player = "";
    public string roulette = "";
    public int? item_level;
    public string? job;
    public string? race;
    public int? level;
    public bool? left_early;
    public string? weapon;
    public string? offhand;
    public string? head;
    public string? body;
    public string? hands;
    public string? legs;
    public string? feet;
    public string? ears;
    public string? neck;
    public string? wrists;
    public string? ring1;
    public string? ring2;
    public string? soulstone;
    public string? materia_weapon;
    public string? materia_offhand;
    public string? materia_head;
    public string? materia_body;
    public string? materia_hands;
    public string? materia_legs;
    public string? materia_feet;
    public string? materia_ears;
    public string? materia_neck;
    public string? materia_wrists;
    public string? materia_ring1;
    public string? materia_ring2;

    public override string ToString()
    {
        return @$"
        DBMateriaset {id}
            player {player}
            weapon {weapon}
            head   {head}
            body   {body}
            ss     {soulstone}";
    }
}



