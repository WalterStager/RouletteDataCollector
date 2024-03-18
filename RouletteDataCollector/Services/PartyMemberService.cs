using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Collections.Generic;
using System.Timers;
using System;
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
        private PartyMemberAddedDelegate partyMemberAddedCallback;
        private PartyMemberGearDelegate partyMemberGearCallback;
        private Timer closeInspectTimer = new Timer();

        public delegate void PartyMemberAddedDelegate(PartyMember newMember);
        public unsafe delegate bool PartyMemberGearDelegate(string playerId, int race, InventoryContainer* invContainer);

        public PartyMemberService(
            RouletteDataCollector plugin,
            PartyMemberAddedDelegate partyMemberAddedCallback,
            PartyMemberGearDelegate partyMemberGearCallback)
        {
            plugin.log.Debug("Start of RouletteDataCollector.PartyMemberService constructor");
            this.plugin = plugin;
            this.partyMemberAddedCallback = partyMemberAddedCallback;
            this.partyMemberGearCallback = partyMemberGearCallback;

            this.closeInspectTimer.Interval = 1000;
            this.closeInspectTimer.AutoReset = false;
        }

        public void Start()
        {
            RouletteDataCollector.addonLifecycle?.RegisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", PartyListUpdateListener);
        }

        public void Stop()
        {
            RouletteDataCollector.addonLifecycle?.UnregisterListener(AddonEvent.PostRequestedUpdate, "_PartyList", PartyListUpdateListener);
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
                        int race = (int)pc.Customize[(int)CustomizeIndex.Race];
                        if (!inspectedPlayers.Contains(uid))
                        {
                            plugin?.log.Info($"Starting timer for {uid}");
                            inspectedPlayers.Add(uid);
                            AgentInspect.Instance()->ExamineCharacter(pc.ObjectId);
                            var callback = (Object? source, ElapsedEventArgs e) => getDataFromExamineWindow(source, e, uid, race);
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

        private unsafe void getDataFromExamineWindow(Object? source, ElapsedEventArgs e, string playerUid, int race)
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
            partyMemberGearCallback(playerUid, race, examineInvContainer);
        }

        // detects when new players join party
        private void PartyListUpdateListener(AddonEvent type, AddonArgs args)
        {
            if (RouletteDataCollector.partyList == null) return;

            for (int i = 0; i < RouletteDataCollector.partyList.Length; i++)
            {
                PartyMember? partyMember = RouletteDataCollector.partyList[i];
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
