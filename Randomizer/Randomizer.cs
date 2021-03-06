using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static ConfigEntry<bool> RandomizeEnemyWeapons;
        public static ConfigEntry<bool> RandomizeEnemyArmor;
        public static ConfigEntry<bool> RandomizeEnemyItems;
        public static ConfigEntry<bool> RandomizeContainers; //TODO: rather RandomizeLoot?
        public static ConfigEntry<bool> RandomizeTrueRandom;

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

                Log.LogMessage("Initialized!");
            }
            catch (Exception e)
            {
                Logger.LogMessage($"Exception during Init: {e}");
            }
        }


        private void SetupConfig()
        {
            RandomizerSeed = Config.Bind("General", "Seed", "", new ConfigDescription("Randomizer Seed. Should give same items for the same seed (per drop table). Doesn't affect items/merchants immediately, but only after an area change/shop refresh.",
                null,
                new ConfigurationManagerAttributes { CustomDrawer = SeedDrawer, HideDefaultButton = true }));
            // Generate a random seed if it's our first time/we got an empty seed
            if (RandomizerSeed.Value == (string)RandomizerSeed.DefaultValue)
                RandomizerSeed.Value = GetRandomSeed();


            // Randomize
            RandomizeMerchants = Config.Bind("General", "Randomize Merchants", true, "Randomize merchant inventories.");
            RandomizeGatherables = Config.Bind("General", "Randomize Gatherables", true, "Randomize gatherables like mining/fishing spots or berry bushes.");
            RandomizeEnemyDrops = Config.Bind("General", "Randomize Enemy Drops", true, "Randomize enemy drops.");
            RandomizeEnemyWeapons = Config.Bind("General", "Randomize Enemy Weapons", true, "Randomize enemy weapons.");
            RandomizeEnemyArmor = Config.Bind("General", "Randomize Enemy Armor", true, "Randomize enemy armor.");
            RandomizeEnemyItems = Config.Bind("General", "Randomize Enemy Items", false, "Randomize all spawned enemy items. This may lead to loss of items like dropped keys.");
            RandomizeContainers = Config.Bind("General", "Randomize Containers", true, "Randomize containers like treasure chests or junk piles.");
            RandomizeTrueRandom = Config.Bind("General", "True Random", false, "Randomize every loot table completely random (even same enemy types will drop different things).");


            // Filter Options
            RestrictSameCategory = Config.Bind("Filters", "Restrict items to same category", true, "Keeps items in the same category (melee weapons only generate another melee weapon).");
        }

        [Conditional("DEBUG")]
        public static void DebugLog(string message)
        {
            Log.LogMessage(message);
        }

        [Conditional("TRACE")]
        public static void DebugTrace(string message)
        {
            Log.LogMessage(message);
        }

        // Generates a random 8 letter string
        private static string GetRandomSeed()
        {
            var random = new Random();
            var str = "";
            for (var i = 0; i < 8; i++)
            {
                var next = random.Next(0, 26);
                str += Convert.ToChar(65 + next);
            }
            return str;
        }

        // Config GUI for the Randomizer Seed
        static void SeedDrawer(ConfigEntryBase entry)
        {
            // Textfield
            entry.BoxedValue = GUILayout.TextField((string)entry.BoxedValue, GUILayout.ExpandWidth(true));
            // Random button to generate a random seed
            if (GUILayout.Button("Random", GUILayout.ExpandWidth(true)))
            {
                entry.BoxedValue = GetRandomSeed();
            }
        }

        public static int GetRandomItem(Item original, out Item item)
        {
            //TODO: set sell value to original value?
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
            //Log.LogMessage($"[RANDOM] Generated {item} ({ret}) instead of {original}");
            return ret;
        }

        public static void RandomizeDropTable<T>(List<T> itemDrops) where T : BasicItemDrop
        {
            //TODO: add key item filter
            foreach (var item in itemDrops)
            {
                if (item == -1) // this may happen?
                    continue;

                if (item.DroppedItem is Currency)
                    continue;

                if (item.DroppedItem.ItemID == Currency.GoldItemID)
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
                if (!RandomizeTrueRandom.Value)
                {
                    var seed = $"{RandomizerSeed.Value}_{dropable.name}";
                    random = new Random(seed.GetHashCode()); //TODO: check if the hash code is deterministic
                }
                DebugTrace($"Dropable: {dropable}");

                // as the reference lists aren't init'ed yet, we need to manuallly collect them
                // Guaranteed Drops
                var guaranteedDrops = dropable.GetComponentsInChildren<GuaranteedDrop>();
                DebugTrace($"{guaranteedDrops} ({guaranteedDrops.Length} Elements)");
                foreach (var drop in guaranteedDrops)
                {
                    var drops = drop.m_itemDrops;
                    DebugTrace($"- {ItemListToString(drops)}");
                    RandomizeDropTable(drops);
                }

                // Drop Tables
                var dropTables = dropable.GetComponentsInChildren<DropTable>();
                DebugTrace($"dropTables: {dropTables} ({dropTables.Length} Elements)");
                foreach (var drop in dropTables)
                {
                    var drops = drop.m_itemDrops;
                    DebugTrace($"- {ItemListToString(drops)}");
                    RandomizeDropTable(drops);
                }

                // conditional tables/drops are serialized, so we can read them out

                // Conditional Drop Tables
                var conditionalTables = dropable.m_conditionalDropTables;
                DebugTrace($"conditionalTables: {conditionalTables} ({conditionalTables.Count} Elements)");
                foreach (var drop in conditionalTables)
                {
                    var drops = drop.Dropper.m_itemDrops;
                    DebugTrace($"- {ItemListToString(drops)}");
                    RandomizeDropTable(drops);
                }

                // Conditional Guaranteed Drops
                var conditionalGuaranteedTables = dropable.m_conditionalGuaranteedDrops;
                DebugTrace($"conditionalGuaranteedTables: {conditionalGuaranteedTables} ({conditionalGuaranteedTables.Count} Elements)");
                foreach (var drop in conditionalGuaranteedTables)
                {
                    var drops = drop.Dropper.m_itemDrops;
                    DebugTrace($"- {ItemListToString(drops)}");
                    RandomizeDropTable(drops);
                }
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during RandomizeDropable: {e}");
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

    // Harmony Hooks

    [HarmonyPatch(typeof(Merchant), nameof(Merchant.Initialize))]
    public class MerchantInitializePatch
    {
        [HarmonyPrefix]
        public static void Prefix(Merchant __instance)
        {
            try
            {
                if (!Randomizer.RandomizeMerchants.Value)
                    return;

                Randomizer.DebugLog($"Merchant.Initialize: instance: {__instance}, UID: {__instance.HolderUID}");
                var prefabTransform = __instance.m_merchantInventoryTablePrefab;
                var dropable = prefabTransform.GetComponent<Dropable>();
                Randomizer.RandomizeDropable(dropable);
                Randomizer.DebugLog("Merchant.Initialize: end");
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during MerchantInitializePatch: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(SelfFilledItemContainer), nameof(SelfFilledItemContainer.InitDrops))]
    public class SelfFilledItemContainerInitPatch
    {
        [HarmonyPrefix]
        public static void Prefix(SelfFilledItemContainer __instance)
        {
            try
            {
                if (!Randomizer.RandomizeGatherables.Value)
                    return;

                Randomizer.DebugLog($"SelfFilledItemContainer.InitDrops: instance: {__instance}, UID: {__instance.HolderUID}");
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
                Randomizer.DebugLog("SelfFilledItemContainer.InitDrops: end");
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during SelfFilledItemContainerInitPatch: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(LootableOnDeath), nameof(LootableOnDeath.Start))]
    public class LootableOnDeathInitPatch
    {
        [HarmonyPrefix]
        public static void Prefix(LootableOnDeath __instance)
        {
            try
            {
                if (!Randomizer.RandomizeEnemyDrops.Value)
                    return;

                Randomizer.DebugLog($"LootableOnDeath.Start: instance: {__instance}, UID: {__instance.Character}");
                foreach (var drop in __instance.SkinDrops)
                {
                    Randomizer.DebugTrace($"SkinDrop: {drop} (Instantiated: {drop.Instantiated})");
                    Randomizer.RandomizeDropable(drop.Dropper);
                }

                foreach (var drop in __instance.LootDrops)
                {
                    Randomizer.DebugTrace($"LootDrop: {drop} (Instantiated: {drop.Instantiated})");
                    Randomizer.RandomizeDropable(drop.Dropper);
                }
                Randomizer.DebugLog("LootableOnDeath.Start: end");
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during LootableOnDeathInitPatch: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(TreasureChest), nameof(TreasureChest.InitDrops))]
    public class TreasureChestInitPatch
    {
        [HarmonyPrefix]
        public static void Prefix(TreasureChest __instance)
        {
            try
            {
                if (!Randomizer.RandomizeContainers.Value)
                    return;

                Randomizer.DebugLog($"TreasureChest.InitDrops: instance: {__instance}, UID: {__instance.HolderUID}");
                // no need to check for the spare comp cause the SelfFilledItemContainer patch will also check it (cause TreasureChest inherits that)
                foreach (var dropable in __instance.DropPrefabsGen)
                {
                    Randomizer.RandomizeDropable(dropable);
                }
                Randomizer.DebugLog("TreasureChest.InitDrops: end");
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during TreasureChestInitPatch: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(StartingEquipment), nameof(StartingEquipment.InitItems))]
    public class StartingEquipmentInitPatch //FIXME: some enemies "skip" the starting Equipment and use the normal weapons
    {
        [HarmonyPrefix]
        public static void Prefix(StartingEquipment __instance)
        {
            try
            {
                // do we need a localplayer check? or is this a feature
                if (__instance.m_character.IsLocalPlayer)
                    return;

                Randomizer.DebugLog($"StartingEquipment.InitItems: instance: {__instance} for {__instance.m_character}");
                if (__instance.m_startingEquipmentTable != null) //TODO: is this deprecated? does anyone use it? maybe use OverrideStartingEquipments?
                    Randomizer.Log.LogMessage($"TODO: startingEquipmentTable: {__instance.m_startingEquipmentTable.Equipments}");

                // Set seed
                if (!Randomizer.RandomizeTrueRandom.Value)
                {
                    var seed = $"{Randomizer.RandomizerSeed.Value}_{__instance.m_character.m_name}";
                    Randomizer.random = new Random(seed.GetHashCode());
                }

                // Starting Pouch Items
                if (__instance.StartingPouchItems != null)
                {
                    Randomizer.DebugTrace($"StartingPouchItems: {__instance.StartingPouchItems} ({__instance.StartingPouchItems.Count} Elements)");
                    foreach (var item in __instance.StartingPouchItems)
                    {
                        if (item != null)
                        {
                            if (item.Item == null)
                            {
                                Randomizer.DebugTrace($"item.Item is null");
                                continue;
                            }

                            Randomizer.DebugTrace($"{item.Item}x {item.Quantity}");
                            // it's a weapon + randomize weapons disabled? dont (weapons may be in the starting items)
                            if (item.Item is Weapon && !Randomizer.RandomizeEnemyWeapons.Value)
                                continue;
                            else if (!Randomizer.RandomizeEnemyItems.Value) // any other item? check config
                                continue;

                            //TODO: maybe remove the randomize enemy items if we find a way to detect & skip key items

                            Randomizer.GetRandomItem(item.Item, out var item1);
                            item.Item = item1;
                            Randomizer.DebugTrace($"now: {item.Item}");
                        }
                    }
                }

                // Starting Equipments
                Randomizer.DebugTrace($"startingEquipment: {__instance.m_startingEquipment}");
                foreach (var equipment in __instance.m_startingEquipment)
                {
                    if (equipment != null)
                    {
                        Randomizer.DebugTrace($"{equipment.EquipSlot}: {equipment}");
                        switch (equipment.EquipSlot)
                        {
                            case EquipmentSlot.EquipmentSlotIDs.RightHand:
                            case EquipmentSlot.EquipmentSlotIDs.LeftHand:
                                if (!Randomizer.RandomizeEnemyWeapons.Value)
                                    continue;

                                break;
                            //TODO: quiver
                            default:
                                if (!Randomizer.RandomizeEnemyArmor.Value)
                                    continue;

                                break;
                        }

                        // check if its a monster weapon
                        var monsterWeapon = false;
                        // helper method that checks if a tag list contains a monster weapon
                        bool isMonsterWeapon(IList<Tag> tags)
                        {
                            foreach (var tag in tags)
                            {
                                //Randomizer.Log.LogMessage($"checking tag {tag}, {tag.TagName}");
                                if (tag.TagName == "MonsterWeapon")
                                {
                                    return true;
                                }
                            }
                            return false;
                        }

                        // tags arent init yet, so we need to suck em out of the tagsrc
                        var equip = __instance.m_character.GetComponentInChildren<Equipment>();
                        if (equip != null)
                        {
                            var tagSrc = equip.m_tagSource;
                            if (tagSrc != null)
                            {
                                if (isMonsterWeapon(tagSrc.Tags))
                                    monsterWeapon = true;
                            }
                        }

                        // first filter the list
                        var originalType = equipment.GetType();
                        var filtered = new List<Item>();
                        foreach (var prefab in ResourcesPrefabManager.ITEM_PREFABS.Values)
                        {
                            if (prefab.GetType() != originalType) // only same type
                                continue;

                            if (((Equipment)prefab).EquipSlot != equipment.EquipSlot) // also check if it's a boot/chest/helmet
                                continue;

                            // monster weapon check
                            if (isMonsterWeapon(prefab.Tags))
                            {
                                // if we have a monster weapon, dont give it to humans (cause they mostly cant hit with it)
                                if (!monsterWeapon)
                                    continue;
                            }
                            else
                            {
                                // only replace original monster weapon with monster weapons (so monsters wont have fucky visuals)
                                if (monsterWeapon)
                                    continue;
                            }

                            filtered.Add(prefab);
                        }

                        // then get a random item from the filtered list
                        var next = Randomizer.random.Next(0, filtered.Count);
                        __instance.m_startingEquipment[(int)equipment.EquipSlot] = (Equipment)filtered[next];
                        Randomizer.DebugTrace($"{__instance.m_character.m_name}: now {((Equipment)filtered[next]).EquipSlot}: {filtered[next]}");
                    }
                }
                Randomizer.DebugLog("StartingEquipment.InitItems: end");
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during StartingEquipmentInitPatch: {e}");
            }
        }
    }
}
