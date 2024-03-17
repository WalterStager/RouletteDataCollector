using System;
using System.Collections.Generic;
using AutoMapper;
using RouletteDataCollector.Structs;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.RaptureGearsetModule;

namespace RouletteDataCollector.Mappings;

public class DBGearsetToResolvedGearset : Profile
{
    public DBGearsetToResolvedGearset()
    {
        CreateMap<DBGearset, ResolvedGearset>()
            .ForMember( dest => dest.job,
                        opti => opti.MapFrom(src => RDCResolvers.resolveJob(src.job)))
            .ForMember( dest => dest.weapon,
                        opti => opti.MapFrom(src => RDCResolvers.resolveItem(src.weapon)))
            .ForMember( dest => dest.offhand,
                        opti => opti.MapFrom(src => RDCResolvers.resolveItem(src.offhand)))
            .ForMember( dest => dest.head,
                        opti => opti.MapFrom(src => RDCResolvers.resolveItem(src.head)))
            .ForMember( dest => dest.body,
                        opti => opti.MapFrom(src => RDCResolvers.resolveItem(src.body)))
            .ForMember( dest => dest.hands,
                        opti => opti.MapFrom(src => RDCResolvers.resolveItem(src.hands)))
            .ForMember( dest => dest.legs,
                        opti => opti.MapFrom(src => RDCResolvers.resolveItem(src.legs)))
            .ForMember( dest => dest.feet,
                        opti => opti.MapFrom(src => RDCResolvers.resolveItem(src.feet)))
            .ForMember( dest => dest.ears,
                        opti => opti.MapFrom(src => RDCResolvers.resolveItem(src.ears)))
            .ForMember( dest => dest.neck,
                        opti => opti.MapFrom(src => RDCResolvers.resolveItem(src.neck)))
            .ForMember( dest => dest.wrists,
                        opti => opti.MapFrom(src => RDCResolvers.resolveItem(src.wrists)))
            .ForMember( dest => dest.ring1,
                        opti => opti.MapFrom(src => RDCResolvers.resolveItem(src.ring1)))
            .ForMember( dest => dest.ring2,
                        opti => opti.MapFrom(src => RDCResolvers.resolveItem(src.ring2)))
            .ForMember( dest => dest.soulstone,
                        opti => opti.MapFrom(src => RDCResolvers.resolveItem(src.soulstone)))
            .ForMember( dest => dest.created,
                        opti => opti.MapFrom(src => src.created.ToLocalTime()))
            .ForMember( dest => dest.updated,
                        opti => opti.MapFrom(src => src.updated.ToLocalTime()));
    }
}

public class ListsToDBGearset : Profile
{
    public ListsToDBGearset()
    {
        CreateMap<(List<uint>, List<string?>), DBGearset>()
            .ForMember( dest => dest.weapon,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src.Item1[(int)GearsetItemIndex.MainHand])))
            .ForMember( dest => dest.offhand,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src.Item1[(int)GearsetItemIndex.OffHand])))
            .ForMember( dest => dest.head,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src.Item1[(int)GearsetItemIndex.Head])))
            .ForMember( dest => dest.body,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src.Item1[(int)GearsetItemIndex.Body])))
            .ForMember( dest => dest.hands,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src.Item1[(int)GearsetItemIndex.Hands])))
            .ForMember( dest => dest.legs,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src.Item1[(int)GearsetItemIndex.Legs])))
            .ForMember( dest => dest.feet,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src.Item1[(int)GearsetItemIndex.Feet])))
            .ForMember( dest => dest.ears,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src.Item1[(int)GearsetItemIndex.Ears])))
            .ForMember( dest => dest.neck,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src.Item1[(int)GearsetItemIndex.Neck])))
            .ForMember( dest => dest.wrists,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src.Item1[(int)GearsetItemIndex.Wrists])))
            .ForMember( dest => dest.ring1,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src.Item1[(int)GearsetItemIndex.RingRight])))
            .ForMember( dest => dest.ring2,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src.Item1[(int)GearsetItemIndex.RingLeft])))
            .ForMember( dest => dest.soulstone,
                        opti => opti.MapFrom(src => RDCResolvers.nullIfZero((int)src.Item1[(int)GearsetItemIndex.SoulStone])))
            .ForMember( dest => dest.materia_weapon,
                        opti => opti.MapFrom(src => src.Item2[(int)GearsetItemIndex.MainHand]))
            .ForMember( dest => dest.materia_offhand,
                        opti => opti.MapFrom(src => src.Item2[(int)GearsetItemIndex.OffHand]))
            .ForMember( dest => dest.materia_head,
                        opti => opti.MapFrom(src => src.Item2[(int)GearsetItemIndex.Head]))
            .ForMember( dest => dest.materia_body,
                        opti => opti.MapFrom(src => src.Item2[(int)GearsetItemIndex.Body]))
            .ForMember( dest => dest.materia_hands,
                        opti => opti.MapFrom(src => src.Item2[(int)GearsetItemIndex.Hands]))
            .ForMember( dest => dest.materia_legs,
                        opti => opti.MapFrom(src => src.Item2[(int)GearsetItemIndex.Legs]))
            .ForMember( dest => dest.materia_feet,
                        opti => opti.MapFrom(src => src.Item2[(int)GearsetItemIndex.Feet]))
            .ForMember( dest => dest.materia_ears,
                        opti => opti.MapFrom(src => src.Item2[(int)GearsetItemIndex.Ears]))
            .ForMember( dest => dest.materia_neck,
                        opti => opti.MapFrom(src => src.Item2[(int)GearsetItemIndex.Neck]))
            .ForMember( dest => dest.materia_wrists,
                        opti => opti.MapFrom(src => src.Item2[(int)GearsetItemIndex.Wrists]))
            .ForMember( dest => dest.materia_ring1,
                        opti => opti.MapFrom(src => src.Item2[(int)GearsetItemIndex.RingRight]))
            .ForMember( dest => dest.materia_ring2,
                        opti => opti.MapFrom(src => src.Item2[(int)GearsetItemIndex.RingLeft]));
    }
}


