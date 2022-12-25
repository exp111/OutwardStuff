using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace MoreChatCommands
{
    public class Give : CustomDebugCmd
    {
        public override string Command => "give";

        public override string HelpText => "Give items into inventory";

        public override string Usage => "/give <itemID> [amount]\n/give <itemID,itemID2,...> [amount]";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            if (args.Length < 2) // not enough args
                return false;

            // Parse item id
            // First split by ,
            var itemIDSplit = args[1].Split(',');
            var itemIDs = new List<int>();
            // then parse each single id
            foreach (var itemID in itemIDSplit)
            {
                if (!int.TryParse(itemID, out var id))
                {
                    ChatError($"Not a valid item id ({id})");
                    return true;
                }
                // and add them into a list
                itemIDs.Add(id);
            }

            // Parse amount
            var amount = 1;
            if (args.Length > 2)
            {
                if (!int.TryParse(args[2], out amount) || amount <= 0)
                {
                    ChatError($"Not a valid amount ({args[2]}).");
                    return true;
                }
            }

            // Check if item ids exists
            foreach (var id in itemIDSplit)
            {
                if (!ResourcesPrefabManager.ITEM_PREFABS.ContainsKey(id))
                {
                    ChatError($"This item does not exist ({args[1]}).");
                    return true;
                }
            }

            // then spawn the items
            foreach (var id in itemIDs)
                GiveItem(id, amount);

            return true;
        }

        public static void GiveItem(int itemID, int amount)
        {
            if (SplitScreenManager.Instance.LocalPlayers.Count == 0)
            {
                ChatError("No players found.");
                return;
            }
            var localPlayer = SplitScreenManager.Instance.LocalPlayers[0].AssignedCharacter;
            if (localPlayer == null)
            {
                ChatError("Player has no character.");
                return;
            }

            // abuse the item quest reward func so we get a info tooltip
            localPlayer.Inventory.ReceiveItemReward(itemID, amount, false);
        }
    }
}
