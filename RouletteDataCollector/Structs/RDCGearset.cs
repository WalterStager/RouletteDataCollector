using System.Collections.Generic;
using RouletteDataCollector.Structs;

namespace RouletteDataCollector.Structs;


enum RDCGearSlot : int
{
    Weapon = 0,
    Offhand = 1,
    Head = 2,
    Body = 3,
    Hands = 4,
    Legs = 5,
    Feet = 6,
    Ears = 7,
    Neck = 8,
    Wrists = 9,
    Ring1 = 10,
    Ring2 = 11
}

public struct RDCGearset
{
    public List<uint> items;
    public List<RDCMateriaset> materia;
    public uint soulstone;

    public RDCGearset()
    {
        items = new List<uint>(new uint[12]);
        materia = new List<RDCMateriaset>(new RDCMateriaset[12]);
    }

    public override string ToString() => $"RDCGearset[{string.Join(',', items)}]";
}





