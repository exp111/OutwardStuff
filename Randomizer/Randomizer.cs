using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
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
        public const string VERSION = "1.1";

        public static Randomizer Instance { get; private set; }
        public static ManualLogSource Log;

        public static Random random = new();

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
                Instance = this;
                Log = Logger;

                // Config
                SetupConfig();

                var harmony = new Harmony(ID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                SideLoader.SL.OnPacksLoaded += SL_OnPacksLoaded;

                Log.LogMessage("Initialized!");
            }
            catch (Exception e)
            {
                Logger.LogMessage($"Exception during Init: {e}");
            }
        }

        private void SL_OnPacksLoaded()
        {
            StartCoroutine(RandomItemLibrary.Init());
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

        public static void RandomizeDropable(Dropable dropable)
        {
            try
            {
                // sometimes it's just a list with a null thing. idk if this is some hack but happens for treasure chests
                if (!dropable)
                    return;

                // Microsoft docs say that this is "fairly expensive" (generating a new random) but my testing said otherwise
                if (!RandomizeTrueRandom.Value)
                {
                    var seed = $"{RandomizerSeed.Value}_{dropable.name}";
                    random = new Random(seed.GetHashCode()); //TODO: check if the hash code is deterministic.
                }
                //Log.LogMessage($"Dropable: {dropable}");

                // Guaranteed Drops
                //Log.LogMessage($"{guaranteedDrops} ({guaranteedDrops.Length} Elements)");
                foreach (var drop in dropable.m_allGuaranteedDrops)
                {
                    //Log.LogMessage($"- {ItemListToString(drops)}");
                    RandomizeDropTable(drop.m_itemDrops);
                }

                // Drop Tables
                var dropTables = dropable.GetComponentsInChildren<DropTable>();
                //Log.LogMessage($"dropTables: {dropTables} ({dropTables.Length} Elements)");
                foreach (var drop in dropable.m_mainDropTables)
                {
                    var drops = drop.m_itemDrops;
                    //Log.LogMessage($"- {ItemListToString(drops)}");
                    RandomizeDropTable(drops);
                }

                // Sinai: conditional tables are never actually used.
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during RandomizeDropable: {e}");
            }
        }

        public static void RandomizeDropTable<T>(List<T> itemDrops) where T : BasicItemDrop
        {
            foreach (var drop in itemDrops)
            {
                if (drop == -1) // this may happen?
                    continue;

                if (drop.DroppedItem is Currency)
                    continue;

                if (drop.DroppedItem.ItemID == Currency.GoldItemID)
                    continue;

                Item item = RandomItemLibrary.Randomize(random, drop.ItemRef, RestrictSameCategory.Value);
                drop.ItemID = item.ItemID;
                drop.ItemRef = item;
            }
        }

        public static string ItemListToString<T>(List<T> items) where T : BasicItemDrop
        {
            return string.Join(", ", items.Select(it => it == null || it?.ItemID == -1
                                                        ? "<invalid ID>"
                                                        : it.DroppedItem.DisplayName));
        }
    }

    // Harmony Hooks

    [HarmonyPatch(typeof(Merchant), nameof(Merchant.RefreshInventory))]
    public class Merchant_RefreshInventory
    {
        [HarmonyPrefix]
        public static void Prefix(Merchant __instance)
        {
            try
            {
                if (!Randomizer.RandomizeMerchants.Value)
                    return;

                //Randomizer.Log.LogMessage($"Merchant.Initialize: instance: {__instance}, UID: {__instance.HolderUID}");
                Randomizer.RandomizeDropable(__instance.DropableInventory);
                //Randomizer.Log.LogMessage("Merchant.Initialize: end");
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during MerchantInitializePatch: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(SelfFilledItemContainer), nameof(SelfFilledItemContainer.ProcessGenerateContent))]
    static class SelfFilledItemContainer_ProcessGenerateContent
    {
        [HarmonyPrefix]
        internal static void Prefix(SelfFilledItemContainer __instance)
        {
            try
            {
                if (__instance is Gatherable && !Randomizer.RandomizeGatherables.Value)
                    return;
                else if (__instance is not Gatherable && !Randomizer.RandomizeContainers.Value)
                    return;

                foreach (Dropable dropable in __instance.m_drops)
                {
                    Randomizer.RandomizeDropable(dropable);
                }
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during SelfFilledItemContainerInitPatch: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(LootableOnDeath), nameof(LootableOnDeath.OnDeath))]
    static class LootableOnDeath_OnDeath
    {
        [HarmonyPrefix]
        internal static void Prefix(LootableOnDeath __instance)
        {
            try
            {
                if (!Randomizer.RandomizeEnemyDrops.Value)
                    return;

                foreach (var drop in __instance.m_lootDroppers)
                {
                    //Randomizer.Log.LogMessage($"LootDrop: {drop} (Instantiated: {drop.Instantiated})");
                    Randomizer.RandomizeDropable(drop);
                }
                //Randomizer.Log.LogMessage("LootableOnDeath.Start: end");
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during LootableOnDeathInitPatch: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(StartingEquipment), nameof(StartingEquipment.InitItems))]
    static class StartingEquipmentInitPatch //FIXME: some enemies "skip" the starting Equipment and use the normal weapons
    {
        [HarmonyPrefix]
        internal static void Prefix(StartingEquipment __instance)
        {
            try
            {
                // do we need a localplayer check? or is this a feature
                if (__instance.m_character.IsLocalPlayer)
                    return;

                //TODO: also randomize StartingPouchItems?

                //Randomizer.Log.LogMessage($"StartingEquipment.InitItems: instance: {__instance} for {__instance.m_character}");
                if (__instance.m_startingEquipmentTable != null) //TODO: is this deprecated? does anyone use it? maybe use OverrideStartingEquipments?
                    Randomizer.Log.LogMessage($"TODO: startingEquipmentTable: {__instance.m_startingEquipmentTable.Equipments}");

                // Set seed
                if (!Randomizer.RandomizeTrueRandom.Value)
                {
                    var seed = $"{Randomizer.RandomizerSeed.Value}_{__instance.m_character.m_name}";
                    Randomizer.random = new Random(seed.GetHashCode());
                }

                AISCombat[] combatStates = __instance.m_character.GetComponentsInChildren<AISCombat>(true);

                // Starting Pouch Items
                if (__instance.StartingPouchItems != null)
                {
                    //Randomizer.Log.LogMessage($"StartingPouchItems: {__instance.StartingPouchItems} ({__instance.StartingPouchItems.Count} Elements)");
                    foreach (var itemQty in __instance.StartingPouchItems)
                    {
                        if (itemQty == null)
                           continue;

                        Item origItem = itemQty.Item;
                        if (!origItem)
                            continue;

                        //Randomizer.Log.LogMessage($"{item.Item}x {item.Quantity}");
                        // it's a weapon + randomize weapons disabled? dont (weapons may be in the starting items)
                        if (origItem is Weapon && !Randomizer.RandomizeEnemyWeapons.Value)
                            continue;
                        else if (!Randomizer.RandomizeEnemyItems.Value) // any other item? check config
                            continue;

                        itemQty.Item = RandomItemLibrary.Randomize(Randomizer.random, origItem, Randomizer.RestrictSameCategory.Value, true);

                        // If orig item was a weapon used by an AI Combat state, replace the reference to our new item.
                        if (origItem is Weapon)
                        {
                            foreach (var state in combatStates)
                            {
                                for (int i = 0; i < state.RequiredWeapon.Length; i++)
                                {
                                    if (state.RequiredWeapon[i] == origItem)
                                    {
                                        state.RequiredWeapon[i] = itemQty.Item as Weapon;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                // Starting Equipments
                //Randomizer.Log.LogMessage($"startingEquipment: {__instance.m_startingEquipment}");
                foreach (var equipment in __instance.m_startingEquipment)
                {
                    if (equipment != null)
                    {
                        //Randomizer.Log.LogMessage($"{equipment.EquipSlot}: {equipment}");
                        switch (equipment.EquipSlot)
                        {
                            case EquipmentSlot.EquipmentSlotIDs.RightHand:
                            case EquipmentSlot.EquipmentSlotIDs.LeftHand:
                                if (!Randomizer.RandomizeEnemyWeapons.Value)
                                    continue;
                                break;
                            // dont randomize quiver ammo or backpack, seems to cause issues.
                            case EquipmentSlot.EquipmentSlotIDs.Quiver:
                            case EquipmentSlot.EquipmentSlotIDs.Back:
                                continue;
                            default:
                                if (!Randomizer.RandomizeEnemyArmor.Value)
                                    continue;
                                break;
                        }

                        Item origItem = __instance.m_startingEquipment[(int)equipment.EquipSlot];
                        Equipment item = RandomItemLibrary.Randomize(Randomizer.random, equipment, true, true) as Equipment;
                        __instance.m_startingEquipment[(int)equipment.EquipSlot] = item;

                        // If orig item was a weapon used by an AI Combat state, replace the reference to our new item.
                        if (origItem is Weapon)
                        {
                            foreach (var state in combatStates)
                            {
                                for (int i = 0; i < state.RequiredWeapon.Length; i++)
                                {
                                    if (state.RequiredWeapon[i] == origItem)
                                    {
                                        state.RequiredWeapon[i] = item as Weapon;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                //Randomizer.Log.LogMessage("StartingEquipment.InitItems: end");

                Randomizer.Instance.StartCoroutine(DelayedAnimFix(__instance.m_character));
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during StartingEquipmentInitPatch: {e}");
            }
        }
    
        internal static IEnumerator DelayedAnimFix(Character character)
        {
            yield return new WaitForSeconds(0.5f);

            character.gameObject.SetActive(false);
            character.gameObject.SetActive(true);
        }
    }
}
