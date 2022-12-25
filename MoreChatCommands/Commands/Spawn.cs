using UnityEngine;

namespace MoreChatCommands
{
    public class Spawn : CustomDebugCmd
    {
        public override string Command => "spawn";

        public override string HelpText => "Spawn items";

        public override string Usage => "/spawn <itemID> [amount]";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            if (args.Length < 2) // not enough args
                return false;

            // Parse item id
            if (!int.TryParse(args[1], out var id))
            {
                ChatError($"Not a valid item id ({args[1]}).");
                return true;
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

            // Check if item id exists
            if (!ResourcesPrefabManager.ITEM_PREFABS.ContainsKey(args[1]))
            {
                ChatError($"This item does not exist ({args[1]}).");
                return true;
            }

            SpawnItem(id, amount);
            return true;
        }

        public static void SpawnItem(int itemID, int amount)
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

            // stolen from DT_ItemSpawner.SpawnItem
            Vector3 vector = localPlayer.CenterPosition + localPlayer.transform.forward * 1.5f;
            if (Physics.Raycast(vector, localPlayer.transform.up * -1f, out _, 10f, Global.LargeEnvironmentMask))
            {
                Quaternion localRotation = localPlayer.transform.localRotation;
                Item item = ItemManager.Instance.GenerateItemNetwork(itemID);
                item.transform.position = vector;
                item.transform.rotation = localRotation;
                if (item.HasMultipleUses) //TODO: else spawn more items?
                    item.GetComponent<MultipleUsage>().RemainingAmount = amount;
                item.gameObject.AddComponent<SafeFalling>();
            }
        }
    }
}
