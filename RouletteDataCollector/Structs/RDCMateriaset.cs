using System.Collections.Generic;

namespace RouletteDataCollector.Structs;


public struct RDCMateriaset
{
    public List<uint> materiaTypes;
    public List<uint> materiaGrades;

    public RDCMateriaset()
    {
        materiaTypes = new List<uint>(new uint[5]);
        materiaGrades = new List<uint>(new uint[5]);
    }
}
