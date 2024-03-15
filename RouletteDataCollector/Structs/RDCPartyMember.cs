namespace RouletteDataCollector.Structs;


public struct RDCPartyMember
{
    public string name;
    public uint homeworldId;
    public uint? lodestoneId;
    public ushort collector;

    public RDCPartyMember(string name, uint homeworldId, ushort collector)
    {
        this.name = name;
        this.homeworldId = homeworldId;
        this.collector = collector;
    }
}


