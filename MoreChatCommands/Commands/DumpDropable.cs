#if DEBUG
using System.Collections.Generic;
using UnityEngine;

namespace MoreChatCommands
{
    public class DumpDropable : CustomDebugCmd
    {
        public override string Command => "dumpDropables";

        public override string HelpText => "Dumps all dropables in the scene.";

        public override string Usage => "/dumpDropable";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            // droptables:
            // DropTable_Neutral_Cloth_Poor_1

            // Check if droptable exists
            var dropables = GameObject.FindObjectsOfType<Dropable>();

            Dictionary<string, bool> done = new();
            foreach (var dropable in dropables)
            {
                //MoreChatCommands.DebugLog($"dropable {dropable}");
                var name = dropable.name;
                if (done.TryGetValue(name, out var _))
                    continue;

                //TODO: handle multiple droptables per object.
                //TODO: handle guaranteed/conditional drops
                var obj = dropable.gameObject;
                var droptableString = "null";
                var droptable = obj.GetComponent<DropTable>();
                if (droptable)
                {
                    var itemDrops = "null";
                    if (droptable.m_itemDrops != null)
                    {
                        itemDrops = "";
                        foreach (var drop in droptable.m_itemDrops)
                        {
                            itemDrops += $"new(){{DropChance={drop.DropChance},MinDropCount={drop.MinDropCount},MaxDropCount={drop.MaxDropCount},ItemID={drop.DroppedItem.ItemID}}},";
                        }
                        itemDrops = $"new() {{{itemDrops}}}";
                    }
                    droptableString = $"new(){{MinNumberOfDrops={droptable.MinNumberOfDrops},MaxNumberOfDrops={droptable.MaxNumberOfDrops},m_itemDrops={itemDrops}}}";
                }

                var text = $"[\"{name}\"] = CreateDropable(\"{name}\", {droptableString}),";
                MoreChatCommands.DebugLog(text);
                done[name] = true;
            }
            ChatPrint($"Dumped {done.Count} dropables.");

            return true;
        }
    }
}
#endif