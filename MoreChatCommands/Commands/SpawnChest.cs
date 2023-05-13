using UnityEngine;

namespace MoreChatCommands
{
    public class SpawnChest : CustomDebugCmd
    {
        public override string Command => "spawnChest";

        public override string HelpText => "Spawns a chest.";

        public override string Usage => "/spawnChest <id>";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            // chest ids:
            // 1000000 Simple Chest
            // 1000010 Simple Chest B?
            // 1000040 Ornate Chest
            // 1000050 Ornate Chest B?
            // 1000060 Trog Chest
            // 1000070 Hollowed Trunk
            if (args.Length < 1) // not enough args
                return false;

            // Parse item id
            if (!int.TryParse(args[1], out var id))
            {
                ChatError($"Not a valid item id ({args[1]}).");
                return true;
            }

            // Check if item id exists
            if (!ResourcesPrefabManager.ITEM_PREFABS.ContainsKey(args[1]))
            {
                ChatError($"This item does not exist ({args[1]}).");
                return true;
            }

            //GameObject.Instantiate();
            Spawn.SpawnItem(id, 1);
            //TODO: spawn chest, add dropable with droptable in gameobject (or link it?)
            return true;
        }
    }
}
