using System.Collections.Generic;
using UnityEngine;

namespace MoreChatCommands
{
    public class SpawnChest : CustomDebugCmd
    {
        public override string Command => "spawnChest";

        public override string HelpText => "Spawns a chest.";

        public override string Usage => "/spawnChest <id>";

        public override bool Cheat => true;

        public static Dictionary<string, Dropable> Dropables = new()
        {
            ["exp111.DropTable_Neutral_Survival_Low_2"] = CreateDropable("DropTable_Neutral_Survival_Low_2", new() { MinNumberOfDrops = 2, MaxNumberOfDrops = 2, m_itemDrops = new() { new() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000080 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000081 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000082 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000083 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000084 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000085 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000086 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000087 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000130 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000131 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000132 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000133 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000134 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000135 }, new ItemDropChance() { DropChance = 5, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000136 }, new ItemDropChance() { DropChance = 10, MinDropCount = 2, MaxDropCount = 5, ItemID = 6500090 }, new ItemDropChance() { DropChance = 10, MinDropCount = 2, MaxDropCount = 4, ItemID = 6400140 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 1, ItemID = 2120050 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 1, ItemID = 2130130 }, new ItemDropChance() { DropChance = 10, MinDropCount = 6, MaxDropCount = 12, ItemID = 5200001 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 3, ItemID = 6000070 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 3, ItemID = 6100010 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 1, ItemID = 5600010 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 1, ItemID = 5100010 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 2, ItemID = 6600020 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 1, ItemID = 5100060 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 1, ItemID = 5010100 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 1, ItemID = 5000020 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 1, ItemID = 4200040 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 3, ItemID = 6500150 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000174 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 3, ItemID = 4100250 }, new ItemDropChance() { DropChance = 10, MinDropCount = 3, MaxDropCount = 10, ItemID = 9000010 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 1, ItemID = 5110003 }, } }),
            ["exp111.DropTable_Neutral_Cloth_Poor_1"] = CreateDropable("DropTable_Neutral_Cloth_Poor_1", new DropTable() { MinNumberOfDrops = 1, MaxNumberOfDrops = 1, m_itemDrops = new() { new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000080 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000081 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000082 }, new ItemDropChance() { DropChance = 5, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000083 }, new ItemDropChance() { DropChance = 5, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000084 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000085 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000086 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000087 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000130 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000131 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000132 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000133 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000134 }, new ItemDropChance() { DropChance = 3, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000135 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000136 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000000 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000001 }, new ItemDropChance() { DropChance = 5, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000002 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000003 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000005 }, new ItemDropChance() { DropChance = 5, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000004 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000006 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000007 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000008 }, new ItemDropChance() { DropChance = 2, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000009 }, new ItemDropChance() { DropChance = 10, MinDropCount = 1, MaxDropCount = 1, ItemID = 3000174 }, } }),
        };
        public static Dictionary<int, Dropable[]> ChestDropablePrefabs = new()
        {
            [1001000] = new Dropable[] { Dropables["exp111.DropTable_Neutral_Cloth_Poor_1"], Dropables["exp111.DropTable_Neutral_Survival_Low_2"] },
        };

        public static Dropable CreateDropable(string name, DropTable droptable)
        {
            var obj = new GameObject(name);
            var table = obj.AddComponent<DropTable>();
            table.m_itemDrops = droptable.m_itemDrops;
            table.MinNumberOfDrops = droptable.MinNumberOfDrops;
            table.MaxNumberOfDrops = droptable.MaxNumberOfDrops;
            var dropable = obj.AddComponent<Dropable>();
            return dropable;
        }

        public override bool Run(string[] args)
        {
            // chest ids:
            // 1000000 Simple Chest
            // 1000010 Simple Chest B?
            // 1000040 Ornate Chest
            // 1000050 Ornate Chest B?
            // 1000060 Trog Chest
            // 1000070 Hollowed Trunk
            // 1001000 Junk Pile
            /*if (args.Length < 2) // not enough args
                return false;

            // Parse item id
            if (!int.TryParse(args[1], out var id))
            {
                ChatError($"Not a valid item id ({args[1]}).");
                return true;
            }*/

            //var itemID = args[1];
            var itemID = "1001000";
            var id = 1001000; //TODO: remove

            // Check if item id exists
            if (!ResourcesPrefabManager.ITEM_PREFABS.ContainsKey(itemID))
            {
                ChatError($"This item does not exist ({itemID}).");
                return true;
            }

            var localPlayer = GetLocalPlayer();
            if (localPlayer == null)
            {
                ChatError("Couldn't get local player.");
                return true;
            }

            //GameObject.Instantiate();
            Vector3 vector = localPlayer.CenterPosition + localPlayer.transform.forward * 1.5f;
            if (Physics.Raycast(vector, localPlayer.transform.up * -1f, out _, 10f, Global.LargeEnvironmentMask))
            {
                Quaternion localRotation = localPlayer.transform.localRotation;
                Item item = ItemManager.Instance.GenerateItemNetwork(id);
                item.transform.position = vector;
                item.transform.rotation = localRotation;
                item.gameObject.AddComponent<SafeFalling>();
                var container = item.gameObject.GetComponent<SelfFilledItemContainer>();
                // add prefabs to chest
                container.DropPrefabs = ChestDropablePrefabs[id];

            }
            //TODO: spawn chest, add dropable with droptable in gameobject (or link it?)
            return true;
        }
    }
}
