using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes; 
using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Component.GUI;

namespace RouletteDataCollector.Services
{
    public sealed class ToDoListService
    {
        private RouletteDataCollector plugin{ get; init; }
        private IAddonLifecycle addonLifecycle { get; init; }
        private ToDoListRouletteInfoDelegate toDoListRouletteInfoCallback { get; init; }
        
        public delegate void ToDoListRouletteInfoDelegate(string rouletteType);

        public ToDoListService(
            RouletteDataCollector plugin,
            IAddonLifecycle addonLifecycle,
            ToDoListRouletteInfoDelegate toDoListRouletteInfoCallback)
        {
            plugin.log.Debug("Start of RouletteDataCollector.ToDoListService constructor");
            this.addonLifecycle = addonLifecycle;
            this.plugin = plugin;
            this.toDoListRouletteInfoCallback = toDoListRouletteInfoCallback;
        }

        public void Start()
        {
            this.addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_ToDoList", this.ToDoListPostRequestedUpdate);
        }

        public void Stop()
        {
            this.addonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "_ToDoList", this.ToDoListPostRequestedUpdate);
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

            // Normal duties use 20004 and Roulettes use 20005
            AtkResNode* queueTimeNode = SearchLinkedListForID(toDoListRootNode, 20004);
            if (queueTimeNode == null)
            {
                plugin.log.Verbose("null queueTimeNode AtkResNode*");
                return;
            }
            AtkTextNode* queueTimeTextNode = (AtkTextNode*)SearchNodeListForID(queueTimeNode, 6);
            if (queueTimeTextNode == null)
            {
                plugin.log.Verbose("null queueTimeTextNode AtkTextNode*");
                return;
            }

            // Normal duties use 20004 and Roulettes use 20005
            if (!queueTimeTextNode->NodeText.ToString().Contains("Time Elapsed"))
            {
                queueTimeNode = SearchLinkedListForID(toDoListRootNode, 20005);
                if (queueTimeNode == null)
                {
                    plugin.log.Verbose("null queueTimeNode AtkResNode*");
                    return;
                }
                queueTimeTextNode = (AtkTextNode*)SearchLinkedListForID(queueTimeNode, 6);
                if (queueTimeTextNode == null)
                {
                    plugin.log.Verbose("null queueTimeTextNode AtkTextNode*");
                    return;
                }
            }
            
            
            if (queueTimeTextNode->NodeText.ToString().Contains("Time Elapsed: 0:00/Average Wait Time: More than 30m")) {
                string rouletteType = dutyInfoTextNode->NodeText.ToString();
                plugin.log.Verbose($"rouletteType '{rouletteType}'");
                this.toDoListRouletteInfoCallback(rouletteType);
            }
        }
    }
}