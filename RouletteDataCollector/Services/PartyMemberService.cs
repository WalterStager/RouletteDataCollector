using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes; 
using Dalamud.Plugin.Services;

namespace RouletteDataCollector.Services
{
    public sealed class PartyMemberService
    {
        private RouletteDataCollector plugin { get; init; }
        private IAddonLifecycle addonLifecycle { get; init; }
        private IPartyList partyList { get; init; }
        private PartyMemberAddedDelegate partyMemberAddedCallback;

        public delegate void PartyMemberAddedDelegate(PartyMember newMember);

        public PartyMemberService(
            RouletteDataCollector plugin,
            IAddonLifecycle addonLifecycle,
            IPartyList partyList,
            PartyMemberAddedDelegate partyMemberAddedCallback)
        {
            plugin.log.Debug("Start of RouletteDataCollector.PartyMemberService constructor");
            this.plugin = plugin;
            this.addonLifecycle = addonLifecycle;
            this.partyList = partyList;
            this.partyMemberAddedCallback = partyMemberAddedCallback;
        }

        public void Start()
        {
            this.addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", PartyListUpdateListener);
        }

        public void Stop()
        {
            this.addonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", PartyListUpdateListener);
        }

        private void PartyListUpdateListener(AddonEvent type, AddonArgs args)
        {
            for (int i = 0; i < this.partyList.Length; i++)
            {
                PartyMember? partyMember = this.partyList[i];
                if (partyMember != null)
                {
                    this.partyMemberAddedCallback(partyMember);
                }
                else
                {
                    this.plugin.log.Warning($"Partymember {i}=null");
                }
            }
        }
    }
}
