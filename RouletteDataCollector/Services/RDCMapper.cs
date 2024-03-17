using AutoMapper;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes; 
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;
using RouletteDataCollector.Mappings;

namespace RouletteDataCollector
{
    public sealed class RDCMapper
    {
        private RouletteDataCollector plugin{ get; init; }
        public IMapper mapper;

        public RDCMapper(
            RouletteDataCollector plugin)
        {
            plugin.log.Debug("Start of RouletteDataCollector.MappingService constructor");
            this.plugin = plugin;

            MapperConfiguration config = new MapperConfiguration(cfg => {
                cfg.AddProfile<ListsToDBGearset>();
                cfg.AddProfile<ListToDBMateriaset>(); 
                cfg.AddProfile<DBGearsetToResolvedGearset>();
                cfg.AddProfile<DBPlayerToResolvedPlayer>();
                cfg.AddProfile<DBMateriasetToResolvedMateriaset>();
                cfg.AddProfile<DBRouletteToResolvedRoulette>(); });

            this.mapper = config.CreateMapper();
        }
    }
}
