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
using System.Timers;
using System;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.DrunkenToad.Extensions;

namespace RouletteDataCollector.Services
{
    public sealed class PartyMemberService
    {
        internal HashSet<string> inspectedPlayers = new HashSet<string>();

        private List<Action<object?, ElapsedEventArgs>> timerCallbacks = new List<Action<object?, ElapsedEventArgs>>();
        private RouletteDataCollector plugin { get; init; }
        private IAddonLifecycle addonLifecycle { get; init; }
        private IPartyList partyList { get; init; }
        private IClientState clientState { get; init;}
        private PartyMemberAddedDelegate partyMemberAddedCallback;
        private PartyMemberGearDelegate partyMemberGearCallback;
        private Timer closeInspectTimer = new Timer();

        public delegate void PartyMemberAddedDelegate(PartyMember newMember);
        public unsafe delegate bool PartyMemberGearDelegate(string playerId, InventoryContainer* invContainer);

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

            this.closeInspectTimer.Interval = 1000;
            this.closeInspectTimer.AutoReset = false;
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
        // returns the number of players that have not been inspected
        public unsafe uint? inspectParty()
        {
            if (RouletteDataCollector.objectTable == null)
            {
                return null;
            }

            uint numberPlayers = 0;
            foreach (GameObject obj in RouletteDataCollector.objectTable)
            {
                if (obj.IsValidPlayerCharacter())
                {
                    numberPlayers++;
                }
            }

            foreach (GameObject obj in RouletteDataCollector.objectTable)
            {
                if (obj.IsValidPlayerCharacter())
                {
                    PlayerCharacter? pc = (PlayerCharacter?)RouletteDataCollector.objectTable.CreateObjectReference(obj.Address);
                    if (pc != null)
                    {
                        string uid = RouletteDataCollector.getPlayerUid(pc);
                        if (!inspectedPlayers.Contains(uid))
                        {
                            plugin?.log.Info($"Starting timer for {uid}");
                            inspectedPlayers.Add(uid);
                            AgentInspect.Instance()->ExamineCharacter(pc.ObjectId);
                            var callback = (Object? source, ElapsedEventArgs e) => getDataFromExamineWindow(source, e, uid);
                            this.timerCallbacks.Add(callback);
                            this.closeInspectTimer.Elapsed += callback.Invoke;
                            this.closeInspectTimer.Start();
                            break;
                        }
                    }
                }
            }
            return (uint?)(numberPlayers - inspectedPlayers.Count);
        }

        public void clearInspectedPlayers()
        {
            this.inspectedPlayers.Clear();
        }

        private unsafe void getDataFromExamineWindow(Object? source, ElapsedEventArgs e, string playerUid)
        {
            foreach (var callback in this.timerCallbacks)
            {
                this.closeInspectTimer.Elapsed -= callback.Invoke;
            }
            timerCallbacks.Clear();
            // TODO is it possible to check if we got the correct inventory container for the expected character?
            InventoryContainer* examineInvContainer = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Examine);
            if (examineInvContainer == null)
            {
                this.plugin.log.Verbose($"InventoryContainer null");
                return;
            }
            partyMemberGearCallback(playerUid, examineInvContainer);
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
