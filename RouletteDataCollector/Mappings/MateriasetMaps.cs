using AutoMapper;
using RouletteDataCollector.Structs;
using System.Collections.Generic;

namespace RouletteDataCollector.Mappings;

public class DBMateriasetToResolvedMateriaset : Profile
{
    public DBMateriasetToResolvedMateriaset()
    {
        CreateMap<DBMateriaset, ResolvedMateriaset>()
            .ForMember( dest => dest.materia0,
                        opt => opt.MapFrom(src => RDCResolvers.resolveMateria(src.materia0_type, src.materia0_grade)))
            .ForMember( dest => dest.materia1,
                        opt => opt.MapFrom(src => RDCResolvers.resolveMateria(src.materia1_type, src.materia1_grade)))
            .ForMember( dest => dest.materia2,
                        opt => opt.MapFrom(src => RDCResolvers.resolveMateria(src.materia2_type, src.materia2_grade)))
            .ForMember( dest => dest.materia3,
                        opt => opt.MapFrom(src => RDCResolvers.resolveMateria(src.materia3_type, src.materia3_grade)))
            .ForMember( dest => dest.materia4,
                        opt => opt.MapFrom(src => RDCResolvers.resolveMateria(src.materia4_type, src.materia4_grade)))
            .ForMember( dest => dest.created,
                        opt => opt.MapFrom(src => src.created.ToLocalTime()))
            .ForMember( dest => dest.updated,
                        opt => opt.MapFrom(src => src.updated.ToLocalTime()));
    }
}


public class ListToDBMateriaset : Profile
{
    public ListToDBMateriaset()
    {
        CreateMap<List<(uint, uint)>, DBMateriaset>()
            .ForMember( dest => dest.materia0_type,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src[0].Item1)))
            .ForMember( dest => dest.materia1_type,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src[1].Item1)))
            .ForMember( dest => dest.materia2_type,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src[2].Item1)))
            .ForMember( dest => dest.materia3_type,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src[3].Item1)))
            .ForMember( dest => dest.materia4_type,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src[4].Item1)))
            .ForMember( dest => dest.materia0_grade,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src[0].Item2)))
            .ForMember( dest => dest.materia1_grade,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src[1].Item2)))
            .ForMember( dest => dest.materia2_grade,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src[2].Item2)))
            .ForMember( dest => dest.materia3_grade,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src[3].Item2)))
            .ForMember( dest => dest.materia4_grade,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src[4].Item2)));
    }
}
