using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes; 
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace RouletteDataCollector.Services
{
    public sealed class ToDoListService
    {
        private RouletteDataCollector plugin{ get; init; }
        private ToDoListRouletteInfoDelegate toDoListRouletteInfoCallback { get; init; }
        
        public delegate void ToDoListRouletteInfoDelegate(string rouletteType);

        public ToDoListService(
            RouletteDataCollector plugin,
            ToDoListRouletteInfoDelegate toDoListRouletteInfoCallback)
        {
            plugin.log.Debug("Start of RouletteDataCollector.ToDoListService constructor");
            this.plugin = plugin;
            this.toDoListRouletteInfoCallback = toDoListRouletteInfoCallback;
        }

        public void Start()
        {
            RouletteDataCollector.addonLifecycle?.RegisterListener(AddonEvent.PostRequestedUpdate, "_ToDoList", this.ToDoListPostRequestedUpdate);
        }

        public void Stop()
        {
            RouletteDataCollector.addonLifecycle?.RegisterListener(AddonEvent.PostRequestedUpdate, "_ToDoList", this.ToDoListPostRequestedUpdate);
        }

        // can return null
        private static unsafe AtkResNode* SearchNodeListForID(AtkResNode* parent, uint id)
        {
            AtkUldManager manager = parent->GetAsAtkComponentNode()->Component->UldManager;
            for (int i = 0; i < manager.NodeListCount; i++)
            {
                if (manager.NodeList[i]->NodeID == id)
                {
                    return manager.NodeList[i];
                }
            }
            return null;
        }

        // can return null
        private static unsafe AtkResNode* SearchLinkedListForID(AtkResNode* parent, uint id)
        {
            AtkResNode* child = parent->ChildNode;
            while (child != null)
            {
                if (child->NodeID == id)
                {
                    break;
                }
                else
                {
                    child = child->PrevSiblingNode;
                }
            }
            return child;
        }

        private unsafe void ToDoListPostRequestedUpdate(AddonEvent type, AddonArgs args)
        {
            AtkUnitBase* toDoListBase = (AtkUnitBase*)args.Addon;
            if (toDoListBase == null)
            {
                plugin.log.Verbose("null toDoListBase AtkUnitBase*");
                return;
            }
            AtkResNode* toDoListRootNode = toDoListBase->GetRootNode();
            if (toDoListRootNode == null)
            {
                plugin.log.Verbose("null toDoListRootNode AtkResNode*");
                return;
            }
            AtkResNode* dutyInfoNode = SearchLinkedListForID(toDoListRootNode, 20001);
            if (dutyInfoNode == null)
            {
                plugin.log.Verbose("null dutyInfoNode AtkResNode*");
                return;
            }
            AtkTextNode* dutyInfoTextNode = (AtkTextNode*)SearchNodeListForID(dutyInfoNode, 6);
            if (dutyInfoTextNode == null)
            {
                plugin.log.Verbose("null dutyInfoTextNode AtkResNode*");
                return;
            }

            string? queueTimeString = null;
            // Normal duties use 20004 and Roulettes use 20005
            AtkResNode* queueTimeNode = SearchLinkedListForID(toDoListRootNode, 20004);
            if (queueTimeNode == null)
            {
                plugin.log.Verbose("null 20004 queueTimeNode AtkResNode*");
            }
            else
            {
                AtkTextNode* queueTimeTextNode = (AtkTextNode*)SearchNodeListForID(queueTimeNode, 6);
                if (queueTimeTextNode == null)
                {
                    plugin.log.Verbose("null 20004 queueTimeTextNode AtkTextNode*");
                }
                else
                {
                    queueTimeString = queueTimeTextNode->NodeText.ToString();
                }
            }
            
            // skip if str contains "Time Elapsed"
            if (!queueTimeString?.Contains("Time Elapsed") ?? true)
            {
                queueTimeNode = SearchLinkedListForID(toDoListRootNode, 20005);
                if (queueTimeNode == null)
                {
                    plugin.log.Verbose("null 20005 queueTimeNode AtkResNode*");
                }
                else
                {
                    AtkTextNode* queueTimeTextNode = (AtkTextNode*)SearchNodeListForID(queueTimeNode, 6);
                    if (queueTimeTextNode == null)
                    {
                        plugin.log.Verbose("null 20005 queueTimeTextNode AtkTextNode*");
                    }
                    else
                    {
                        queueTimeString = queueTimeTextNode->NodeText.ToString();
                    }
                }
            }
            
            // skip if str null or doesn't contain "Time Elapsed..."
            if (queueTimeString?.Contains("Time Elapsed: 0:00/Average Wait Time: More than 30m") ?? false) {
                string rouletteType = dutyInfoTextNode->NodeText.ToString();
                plugin.log.Verbose($"rouletteType '{rouletteType}'");
                this.toDoListRouletteInfoCallback(rouletteType);
            }
        }
    }
}
