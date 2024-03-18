using AutoMapper;
using RouletteDataCollector.Structs;

namespace RouletteDataCollector.Mappings;

public class DBRouletteToResolvedRoulette : Profile
{
    public DBRouletteToResolvedRoulette()
    {
        CreateMap<DBRoulette, ResolvedRoulette>()
            .ForMember( dest => dest.territory,
                        opt => opt.MapFrom(src => RDCResolvers.resolveTerritory(src.territory_id)))
            .ForMember( dest => dest.content,
                        opt => opt.MapFrom(src => RDCResolvers.resolveContent(src.content_id)))
            .ForMember( dest => dest.created,
                        opt => opt.MapFrom(src => src.created.ToLocalTime()))
            .ForMember( dest => dest.updated,
                        opt => opt.MapFrom(src => src.updated.ToLocalTime()))
            .ForMember( dest => dest.queue_start,
                        opt => opt.MapFrom(src => RDCResolvers.utcToLocal(src.queue_start)))
            .ForMember( dest => dest.queue_end,
                        opt => opt.MapFrom(src => RDCResolvers.utcToLocal(src.queue_end)))
            .ForMember( dest => dest.duty_start,
                        opt => opt.MapFrom(src => RDCResolvers.utcToLocal(src.duty_start)))
            .ForMember( dest => dest.duty_end,
                        opt => opt.MapFrom(src => RDCResolvers.utcToLocal(src.duty_end)));
    }
}


