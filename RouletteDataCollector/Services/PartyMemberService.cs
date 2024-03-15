using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections;
using System.Collections.Generic;

namespace RouletteDataCollector.Services
{
    public sealed class PartyMemberService
    {
        private RouletteDataCollector plugin { get; init; }
        private IAddonLifecycle addonLifecycle { get; init; }
        private IPartyList partyList { get; init; }
        private IClientState clientState { get; init;}
        private PartyMemberAddedDelegate partyMemberAddedCallback;
        private PartyMemberGearDelegate partyMemberGearCallback;

        public delegate void PartyMemberAddedDelegate(PartyMember newMember);
        public unsafe delegate bool PartyMemberGearDelegate(PartyMember member, InventoryContainer* invContainer);

        public PartyMemberService(
            RouletteDataCollector plugin,
            IAddonLifecycle addonLifecycle,
            IPartyList partyList,
            IClientState clientState,
            PartyMemberAddedDelegate partyMemberAddedCallback,
            PartyMemberGearDelegate partyMemberGearCallback)
        {
            plugin.log.Debug("Start of RouletteDataCollector.PartyMemberService constructor");
            this.plugin = plugin;
            this.addonLifecycle = addonLifecycle;
            this.partyList = partyList;
            this.clientState = clientState;
            this.partyMemberAddedCallback = partyMemberAddedCallback;
            this.partyMemberGearCallback = partyMemberGearCallback;
        }

        public void Start()
        {
            this.addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", PartyListUpdateListener);
        }

        public void Stop()
        {
            this.addonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", PartyListUpdateListener);
        }

        // inspects 1 player that has not already been inspected
        // returns the number of players in party that have not been inspected
        public unsafe uint? inspectParty()
        {
            // do nothing if not in content
            if (!this.plugin.inContent) return null;

            // only inspect one player
            bool inspectedOnce = false;

            // return number of uninspected party members (clears when you leave instance)
            uint countUninspected = 0;
            for (int i = 0; i < this.partyList.Length; i++) 
            {
                PartyMember? partyMember = this.partyList[i];
                if (partyMember == null) continue;

                // don't re-inspect (this includes between instances)
                string memberUID = RouletteDataCollector.getPartyMemberUniqueString(partyMember);
                bool inspected = this.plugin.inspectedPlayers.Contains(memberUID);
                if (!inspected)
                {
                    countUninspected++;
                }

                if (!inspected && !inspectedOnce)
                {
                    inspectedOnce = true;
                    AgentInspect.Instance()->ExamineCharacter(partyMember.ObjectId);
                    // TODO is it possible to check if we got the correct inventory container for the expected character?
                    // currently rate limit is the best way to avoid this
                    InventoryContainer* examineInvContainer = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Examine);
                    if (examineInvContainer == null)
                    {
                        this.plugin.log.Verbose($"InventoryContainer null");
                        continue;
                    }
                    
                    // the inventory can be not-null but empty
                    // this is checked in callback because that is where items are iterated
                    if (partyMemberGearCallback(partyMember, examineInvContainer))
                    {
                        // successfully inspected
                        countUninspected--;
                        this.plugin.inspectedPlayers.Add(memberUID);
                    }

                    // close Inspect window
                    AtkUnitBase* baseAddon = (AtkUnitBase*)this.plugin.gameGui.GetAddonByName("CharacterInspect");
                    if (baseAddon != null)
                    {
                        this.plugin.log.Verbose("Closing CharacterInspect via getter");
                        baseAddon->Close(true);
                    }
                }
            }

            return countUninspected;
        }

        // detects when new players join party
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
                    this.plugin.log.Verbose($"Partymember {i}=null");
                }
            }
        }
    }
}
