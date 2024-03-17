using AutoMapper;
using Lumina.Excel.GeneratedSheets;
using RouletteDataCollector.Structs;
using RouletteDataCollector.Mappings;

namespace RouletteDataCollector.Mappings;

public class DBPlayerToResolvedPlayer : Profile
{
    public DBPlayerToResolvedPlayer()
    {
        CreateMap<DBPlayer, ResolvedPlayer>()
            .ForMember( dest => dest.homeworld,
                        opt => opt.MapFrom(src => RDCResolvers.resolveWorld(src.homeworld)))
            .ForMember( dest => dest.created,
                        opt => opt.MapFrom(src => src.created.ToLocalTime()))
            .ForMember( dest => dest.updated,
                        opt => opt.MapFrom(src => src.updated.ToLocalTime()));
    }
}


