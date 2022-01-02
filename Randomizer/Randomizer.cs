using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Random = System.Random;

namespace Randomizer
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class Randomizer : BaseUnityPlugin
    {
        public const string ID = "com.exp111.randomizer";
        public const string NAME = "Randomizer";
        public const string VERSION = "1.0";

        public static ManualLogSource Log;

        public static Random random = new Random();

        public static ConfigEntry<string> RandomizerSeed; //TODO: generate random seed

        public static ConfigEntry<bool> RandomizeMerchants;
        public static ConfigEntry<bool> RandomizeGatherables;
        public static ConfigEntry<bool> RandomizeEnemyDrops;
        public static ConfigEntry<bool> RandomizeEnemyWeapons; //TODO: enemy weapons
        public static ConfigEntry<bool> RandomizeContainers; //TODO: rather RandomizeLoot?

        public static ConfigEntry<bool> RestrictSameCategory;
        //TODO: seed

        // Droptable name to existing droptable //TODO: save as what? string array? ItemDrop list? //TODO: remove this?
        //public static Dictionary<string, string> DropTableMap = new Dictionary<string, string>();

        // Awake is called when your plugin is created. Use this to set up your mod.
        internal void Awake()
        {
            try
            {
                Log = Logger;

                // Config
                SetupConfig();

                var harmony = new Harmony(ID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                Log.LogMessage($"Initialized!");
            }
            catch (Exception e)
            {
                Logger.LogMessage($"Exception during Init: {e}");
            }
        }

        private void SetupConfig()
        {
            RandomizerSeed = Config.Bind("General", "Seed", "test", "Randomizer Seed. Should give same items for the same seed (per drop table). Doesn't affect items/merchants immediately, but only after an area change/shop refresh.");
            // Randomize
            RandomizeMerchants = Config.Bind("General", "Randomize Merchants", true, "Randomize merchant inventories.");
            RandomizeGatherables = Config.Bind("General", "Randomize Gatherables", true, "Randomize gatherables like mining/fishing spots or berry bushes.");
            RandomizeEnemyDrops = Config.Bind("General", "Randomize Enemy Drops", true, "Randomize enemy drops.");
            RandomizeContainers = Config.Bind("General", "Randomize Containers", true, "Randomize containers like treasure chests or junk piles.");

            // Filter Options
            RestrictSameCategory = Config.Bind("Filters", "Restrict items to same category", true, "Keeps items in the same category (melee weapons only generate another melee weapon).");
        }
        
        public static int GetRandomItem(Item original, out Item item)
        {
            var itemPrefabs = ResourcesPrefabManager.ITEM_PREFABS;
            
            if (!RestrictSameCategory.Value)
            {
                var next = random.Next(0, ResourcesPrefabManager.ITEM_PREFABS.Values.Count);
                item = itemPrefabs.Values.ElementAt(next);
            }
            else
            {
                // filter first
                var originalType = original.GetType();
                var filtered = new List<Item>();
                foreach (var prefab in itemPrefabs.Values)
                {
                    if (prefab.GetType() == originalType)
                        filtered.Add(prefab);
                }
                // then get a random item from the filtered list
                var next = random.Next(0, filtered.Count);
                item = filtered[next];
            }

            var ret = item.ItemID;
            //Log.LogMessage($"[RANDOM] Generated {item} ({ret})");
            return ret;
        }

        public static void RandomizeDropTable<T>(List<T> itemDrops) where T : BasicItemDrop
        {
            //TODO: add key item filter
            foreach (var item in itemDrops)
            {
                if (item.DroppedItem is Currency)
                    continue;

                if (item.DroppedItem.name == "6300030_GoldIngot")
                    continue;

                item.ItemID = GetRandomItem(item.DroppedItem, out var newItem);
                item.ItemRef = newItem;
            }
            //TODO: add the droptable cache here or a level higher
        }

        public static void RandomizeDropable(Dropable dropable)
        {
            try
            {
                // sometimes it's just a list with a null thing. idk if this is some hack but happens for treasure chests
                if (dropable == null)
                    return;

                // Microsoft docs say that this is "fairly expensive" (generating a new random) but my testing said otherwise
                var seed = $"{RandomizerSeed.Value}_{dropable.name}";
                random = new Random(seed.GetHashCode()); //TODO: check if the hash code is deterministic
                //Log.LogMessage($"Dropable: {dropable}");

                // as the reference lists aren't init'ed yet, we need to manuallly collect them
                // Guaranteed Drops
                var guaranteedDrops = dropable.GetComponentsInChildren<GuaranteedDrop>();
                //Log.LogMessage($"{guaranteedDrops} ({guaranteedDrops.Length} Elements)");
                foreach (var drop in guaranteedDrops)
                {
                    var drops = drop.m_itemDrops;
                    //Log.LogMessage($"- {ItemListToString(drops)}");
                    RandomizeDropTable(drops);
                }

                // Drop Tables
                var dropTables = dropable.GetComponentsInChildren<DropTable>();
                //Log.LogMessage($"dropTables: {dropTables} ({dropTables.Length} Elements)");
                foreach (var drop in dropTables)
                {
                    var drops = drop.m_itemDrops;
                    //Log.LogMessage($"- {ItemListToString(drops)}");
                    RandomizeDropTable(drops);
                }

                // conditional tables/drops are serialized, so we can read them out

                // Conditional Drop Tables
                var conditionalTables = dropable.m_conditionalDropTables;
                //Log.LogMessage($"conditionalTables: {conditionalTables} ({conditionalTables.Count} Elements)");
                foreach (var drop in conditionalTables)
                {
                    var drops = drop.Dropper.m_itemDrops;
                    //Log.LogMessage($"- {ItemListToString(drops)}");
                    RandomizeDropTable(drops);
                }

                // Conditional Guaranteed Drops
                var conditionalGuaranteedTables = dropable.m_conditionalGuaranteedDrops;
                //Log.LogMessage($"conditionalGuaranteedTables: {conditionalGuaranteedTables} ({conditionalGuaranteedTables.Count} Elements)");
                foreach (var drop in conditionalGuaranteedTables)
                {
                    var drops = drop.Dropper.m_itemDrops;
                    //Log.LogMessage($"- {ItemListToString(drops)}");
                    RandomizeDropTable(drops);
                }
            }
            catch (Exception e)
            {
                Log.LogMessage($"Dropable: {e}");
            }
        }

        // Joins item list
        public static string ItemListToString<T>(List<T> items) where T : BasicItemDrop
        {
            var str = "";
            foreach (var item in items)
            {
                if (item != -1)
                {
                    str += item.DroppedItem.DisplayName + ", ";
                }
                else
                {
                    str += "<Invalid ID>, ";
                }
            }
            return str;
        }
    }

    [HarmonyPatch(typeof(Merchant), "Initialize")]
    public class MerchantInitializePatch
    {
        [HarmonyPrefix]
        public static void Prefix(Merchant __instance)
        {
            if (!Randomizer.RandomizeMerchants.Value)
                return;

            //Randomizer.Log.LogMessage($"merchant.initialize: instance: {__instance}, UID: {__instance.HolderUID}");
            var prefabTransform = __instance.m_merchantInventoryTablePrefab;
            var dropable = prefabTransform.GetComponent<Dropable>();
            Randomizer.RandomizeDropable(dropable);
        }
    }

    [HarmonyPatch(typeof(SelfFilledItemContainer), "InitDrops")]
    public class SelfFilledItemContainerInitPatch
    {
        [HarmonyPrefix]
        public static void Prefix(SelfFilledItemContainer __instance)
        {
            if (!Randomizer.RandomizeGatherables.Value)
                return;

            //Randomizer.Log.LogMessage($"SelfFilledItemContainer.InitDrops: instance: {__instance}, UID: {__instance.HolderUID}");
            var dropComp = __instance.GetComponent<Dropable>();
            if (dropComp != null) //TODO: check whats up with this comp. is it being used? should we change it?
            {
                Randomizer.Log.LogMessage($"TODO: Found dropable comp in SelfFilledContainer {dropComp}");
                var guaranteedDrops = dropComp.GetComponentsInChildren<GuaranteedDrop>();
                Randomizer.Log.LogMessage($"{guaranteedDrops} ({guaranteedDrops.Length} Elements)");
                foreach (var drop in guaranteedDrops)
                {
                    var drops = drop.m_itemDrops;
                    Randomizer.Log.LogMessage($"- {Randomizer.ItemListToString(drops)}");
                    Randomizer.RandomizeDropTable(drops);
                }
                var dropTables = dropComp.GetComponentsInChildren<DropTable>();
                Randomizer.Log.LogMessage($"dropTables: {dropTables} ({dropTables.Length} Elements)");
                foreach (var drop in dropTables)
                {
                    var drops = drop.m_itemDrops;
                    Randomizer.Log.LogMessage($"- {Randomizer.ItemListToString(drops)}");
                    Randomizer.RandomizeDropTable(drops);
                }
            }

            foreach (var dropable in __instance.DropPrefabs)
            {
                Randomizer.RandomizeDropable(dropable);
            }
        }
    }

    [HarmonyPatch(typeof(LootableOnDeath), "Start")]
    public class LootableOnDeathInitPatch
    {
        [HarmonyPrefix]
        public static void Prefix(LootableOnDeath __instance)
        {
            if (!Randomizer.RandomizeEnemyDrops.Value)
                return;

            //Randomizer.Log.LogMessage($"LootableOnDeath.Start: instance: {__instance}, UID: {__instance.Character}");
            foreach (var drop in __instance.SkinDrops)
            {
                //Randomizer.Log.LogMessage($"SkinDrop: {drop} (Instantiated: {drop.Instantiated})");
                Randomizer.RandomizeDropable(drop.Dropper);
            }

            foreach (var drop in __instance.LootDrops)
            {
                //Randomizer.Log.LogMessage($"LootDrop: {drop} (Instantiated: {drop.Instantiated})");
                Randomizer.RandomizeDropable(drop.Dropper);
            }
        }
    }

    [HarmonyPatch(typeof(TreasureChest), "InitDrops")]
    public class TreasureChestInitPatch
    {
        [HarmonyPrefix]
        public static void Prefix(TreasureChest __instance)
        {
            if (!Randomizer.RandomizeContainers.Value)
                return;

            //Randomizer.Log.LogMessage($"TreasureChest.InitDrops: instance: {__instance}, UID: {__instance.HolderUID}");
            // no need to check for the spare comp cause the SelfFilledItemContainer patch will also check it (cause TreasureChest inherits that)
            foreach (var dropable in __instance.DropPrefabsGen)
            {
                Randomizer.RandomizeDropable(dropable);
            }
        }
    }
}
