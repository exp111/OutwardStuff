using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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

        public static ConfigEntry<string> RandomizerSeed;

        public static ConfigEntry<bool> RandomizeMerchants;
        public static ConfigEntry<bool> RandomizeGatherables;
        public static ConfigEntry<bool> RandomizeEnemyDrops;
        public static ConfigEntry<bool> RandomizeEnemyWeapons;
        public static ConfigEntry<bool> RandomizeEnemyArmor;
        public static ConfigEntry<bool> RandomizeEnemyItems;
        public static ConfigEntry<bool> RandomizeContainers; //TODO: rather RandomizeLoot?
        public static ConfigEntry<bool> RandomizeTrueRandom;

        public static ConfigEntry<bool> RestrictSameCategory;

        public static ConfigEntry<bool> HideEquipmentNoIcon;
        public static ConfigEntry<bool> RandomizeKeys;

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
                PrintDebugWarning();
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
            RandomizerSeed = Config.Bind("General", "Seed", "", new ConfigDescription("Randomizer Seed. Should give same items for the same seed (per drop table). Doesn't affect merchants immediately, but only after an shop refresh.",
                null,
                new ConfigurationManagerAttributes { CustomDrawer = SeedDrawer, HideDefaultButton = true }));
            // Generate a random seed if it's our first time/we got an empty seed
            if (RandomizerSeed.Value == (string)RandomizerSeed.DefaultValue)
                RandomizerSeed.Value = GetRandomSeed();
            Log.LogMessage($"Seed: {RandomizerSeed.Value}");
            RandomizerSeed.SettingChanged += (_, _) => Log.LogMessage($"Changed seed to: {RandomizerSeed.Value}");

            // Randomize
            RandomizeMerchants = Config.Bind("General", "Randomize Merchants", true, "Randomize merchant inventories.");
            RandomizeGatherables = Config.Bind("General", "Randomize Gatherables", true, "Randomize gatherables like mining/fishing spots or berry bushes.");
            RandomizeEnemyDrops = Config.Bind("General", "Randomize Enemy Drops", true, "Randomize enemy drops.");
            RandomizeEnemyWeapons = Config.Bind("General", "Randomize Enemy Weapons", true, "Randomize enemy weapons.");
            RandomizeEnemyArmor = Config.Bind("General", "Randomize Enemy Armor", true, "Randomize enemy armor.");
            RandomizeEnemyItems = Config.Bind("General", "Randomize Enemy Items", false, "Randomize all spawned enemy items.");
            RandomizeContainers = Config.Bind("General", "Randomize Containers", true, "Randomize containers like treasure chests or junk piles.");
            RandomizeTrueRandom = Config.Bind("General", "True Random", false, "Randomize every loot table completely random (even same enemy types will drop different things).");

            // Filter Options
            RestrictSameCategory = Config.Bind("Filters", "Restrict items to same category", true, "Keeps items in the same category (melee weapons only generate another melee weapon).");
            HideEquipmentNoIcon = Config.Bind("Filters", "Hide equipment with no icon", true, "Hides weapons and armor that has no icon. These are probably not meant for players to receive, but still work.");
            HideEquipmentNoIcon.SettingChanged += (_, _) => RandomItemLibrary.GenerateCache();
            RandomizeKeys = Config.Bind("Filters", "Randomize Keys", false, "Randomize keys and other special items (this means that keys will not drop from the enemies/containers that usually drop them. They also may never drop).");
            RandomizeKeys.SettingChanged += (_, _) => RandomItemLibrary.GenerateCache();
        }

        // only used during debug
        private static Stopwatch debugStopwatch = null;

        [Conditional("DEBUG")]
        public static void StartTimer()
        {
            if (debugStopwatch == null)
                debugStopwatch = new();

            debugStopwatch.Restart();
        }
        [Conditional("DEBUG")]
        public static void StopTimer()
        {
            debugStopwatch.Stop();
            DebugLog($"Elapsed: {debugStopwatch.ElapsedMilliseconds} ms");
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
        [Conditional("DEBUG")]
        public static void PrintDebugWarning()
        {
            Log.LogMessage("Using a DEBUG build.");
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
                DebugTrace($"Dropable: {dropable}");

                // Guaranteed Drops
                var guaranteedDrops = dropable.m_allGuaranteedDrops;
                DebugTrace($"{guaranteedDrops} ({guaranteedDrops.Count} Elements)");
                foreach (var drop in guaranteedDrops)
                {
                    DebugTrace($"- {ItemListToString(drop.m_itemDrops)}");
                    RandomizeDropTable(drop.m_itemDrops);
                }

                // Drop Tables
                var dropTables = dropable.m_mainDropTables;
                DebugTrace($"dropTables: {dropTables} ({dropTables.Count} Elements)");
                foreach (var drop in dropTables)
                {
                    var drops = drop.m_itemDrops;
                    DebugTrace($"- {ItemListToString(drops)}");
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
                DebugTrace($"Generated: {item.Name} ({item.ItemID}) for {drop.ItemRef.Name} ({drop.ItemRef.ItemID})");
                RandomItemLibrary.ClampDropAmount(drop, item);
                drop.ItemID = item.ItemID;
                drop.ItemRef = item;
            }
        }

        public static string ItemListToString<T>(List<T> items) where T : BasicItemDrop
        {
            // ive recently only got dropable with itemID = -1, while ItemRef is still set, so just skip checking the itemid
            return string.Join(", ", items.Select(it => it == null || it.DroppedItem == null
                                                        ? "<invalid item>"
                                                        : $"{it.DroppedItem.DisplayName} ({it.MinDropCount}-{it.MaxDropCount})"));
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

                Randomizer.StartTimer();
                Randomizer.DebugLog($"Merchant.Initialize: instance: {__instance}, UID: {__instance.HolderUID}");
                Randomizer.RandomizeDropable(__instance.DropableInventory);
                //TODO: dont do this on refreshinventory but instead again on initialize? or take from dropableprefab => need it in refresh for true random?
                Randomizer.DebugLog($"Merchant.Initialize: end");
                Randomizer.StopTimer();
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during Merchant.RefreshInventory hook: {e}");
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

                Randomizer.StartTimer();
                Randomizer.DebugLog($"SelfFilledItemContainer.ProcessGenerateContent: instance: {__instance}, UID: {__instance.HolderUID}");
                foreach (Dropable dropable in __instance.m_drops)
                {
                    Randomizer.RandomizeDropable(dropable);
                }
                Randomizer.DebugLog("SelfFilledItemContainer.ProcessGenerateContent: end");
                Randomizer.StopTimer();
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during SelfFilledItemContainer.ProcessGenerateContent hook: {e}");
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

                Randomizer.StartTimer();
                Randomizer.DebugLog($"LootableOnDeath.OnDeath: instance: {__instance}, UID: {__instance.Character}");
                //INFO: m_skinDroppers and m_lootDroppers are inferred from SkinDrops/LootDrops on LootableOnDeath.Awake, so we need to use those
                foreach (var drop in __instance.m_skinDroppers)
                {
                    Randomizer.DebugTrace($"SkinDrop: {drop}");
                    Randomizer.RandomizeDropable(drop);
                }

                foreach (var drop in __instance.m_lootDroppers)
                {
                    Randomizer.DebugTrace($"LootDrop: {drop}");
                    Randomizer.RandomizeDropable(drop);
                }
                Randomizer.DebugLog("LootableOnDeath.OnDeath: end");
                Randomizer.StopTimer();
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during LootableOnDeath.OnDeath hook: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(StartingEquipment), nameof(StartingEquipment.InitItems))]
    static class StartingEquipmentInitItemsPatch
    {
        [HarmonyPrefix]
        internal static void Prefix(StartingEquipment __instance)
        {
            try
            {
                // do we need a localplayer check? or is this a feature
                if (__instance.m_character.IsLocalPlayer)
                    return;

                Randomizer.StartTimer();
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
                    foreach (var itemQty in __instance.StartingPouchItems)
                    {
                        if (itemQty == null)
                            continue;

                        Item origItem = itemQty.Item;
                        if (!origItem)
                            continue;

                        Randomizer.DebugTrace($"{itemQty.Quantity}x {itemQty.Item}");
                        // it's a weapon + randomize weapons disabled? dont (weapons may be in the starting items)
                        if (origItem is Weapon && !Randomizer.RandomizeEnemyWeapons.Value)
                            continue;
                        else if (!Randomizer.RandomizeEnemyItems.Value) // any other item? check config
                            continue;

                        var item = RandomItemLibrary.Randomize(Randomizer.random, origItem, Randomizer.RestrictSameCategory.Value, true);
                        Randomizer.DebugTrace($"Generated: {item.Name}, ({item.ItemID}) for {origItem.Name} ({origItem.ItemID})");
                        RandomItemLibrary.ClampDropAmount(itemQty, item);
                        itemQty.Item = item;
                        RandomItemLibrary.FixCombatStates(__instance.m_character, item, origItem);
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
                            // dont randomize quiver ammo or backpack, seems to cause issues.
                            case EquipmentSlot.EquipmentSlotIDs.Quiver:
                            case EquipmentSlot.EquipmentSlotIDs.Back:
                                continue;
                            default:
                                if (!Randomizer.RandomizeEnemyArmor.Value)
                                    continue;
                                break;
                        }

                        // the tags aren't inited on the starting equipment, so we gotta get them from the real item
                        Item prefab = ResourcesPrefabManager.Instance.GetItemPrefab(equipment.ItemID);
                        Equipment item = RandomItemLibrary.Randomize(Randomizer.random, prefab, true, true) as Equipment;
                        Randomizer.DebugTrace($"Generated: {item.Name}, ({item.ItemID}) for {prefab.Name} ({prefab.ItemID})");
                        __instance.m_startingEquipment[(int)equipment.EquipSlot] = item;
                        RandomItemLibrary.FixCombatStates(__instance.m_character, item, equipment);
                    }
                }
                Randomizer.DebugLog("StartingEquipment.InitItems: end");
                Randomizer.StopTimer();

                Randomizer.Instance.StartCoroutine(DelayedRegisterUIDFix(__instance));
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during StartingEquipment.InitItems hook: {e}");
            }
        }

        // Some items are only registered when active (ie the player near them), so we register them early so they'll get saved
        internal static IEnumerator DelayedRegisterUIDFix(StartingEquipment startingEquipment)
        {
            yield return new WaitForSeconds(0.5f);

            try
            {
                var equipment = startingEquipment.m_character.Inventory.Equipment;
                foreach (var equip in startingEquipment.m_startingEquipment)
                {
                    if (equip != null)
                    {
                        var slot = equipment.EquipmentSlots[(int)equip.EquipSlot];
                        if (slot == null)
                            continue;

                        var realEquipment = slot.EquippedItem;
                        if (realEquipment && !realEquipment.m_initialized)
                        {
                            //ItemManager.Instance.RequestItemInitialization(realEquipment);
                            realEquipment.Start();
                            Randomizer.DebugTrace($"Registering {realEquipment} for character {startingEquipment.m_character}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during DelayedRegisterUIDFix: {e}");
            }
        }

        // This fixes the spinning
        internal static IEnumerator DelayedAnimFix(Character character)
        {
            yield return new WaitForSeconds(0.5f);

            //INFO: this works better than character.FixAnimationBugCheat()
            character.gameObject.SetActive(false);
            character.gameObject.SetActive(true);
        }
    }


    [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.OnReceiveItemSync))]
    static class ItemManagerReceiveItemPatch
    {
#if DEBUG
        [HarmonyDebug]
#endif
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var cur = new CodeMatcher(instructions);
                /*
                // Replace flag2 set to true, add log in debug (conditional add)
                if (_syncType == ItemManager.ItemSyncType.SceneLoad)
                {
                    flag2 = (!Item.IsChildOfEquipmentSlot(array3) || Item.IsHandEquipment(array3));
                }
                ...

                    IL_0154: ldarg.2 // _syncType
                    IL_0155: ldc.i4.3 // ItemManager.ItemSyncType.SceneLoad
                    IL_0156: bne.un.s IL_016D // jump away
                    IL_0158: ldloc.s V_6 // array3
                    IL_015A: call      bool Item::IsChildOfEquipmentSlot(string[])
                    IL_015F: brfalse.s IL_016A // if (!..) { do }
                    IL_0161: ldloc.s V_6 // array 3
                    IL_0163: call      bool Item::IsHandEquipment(string[])
                    IL_0168: br.s IL_016B // leaves the result on stack and jumps to save
                    IL_016A: ldc.i4.1 // load true
                    IL_016B: stloc.s V_7 // save into flag2
                    IL_016D: ldloc.s   V_7

                    =>

                    if (_syncType == ItemManager.ItemSyncType.SceneLoad)
                    {
                        flag2 = true;
                        if (Item.IsChildOfEquipmentSlot(array3) && !Item.IsHandEquipment(array3))
                            Randomizer.DebugTrace($"allowing item: {itemUIDFromSyncInfo} ({Item.GetItemIDFromSyncInfo(array3)})");
                    }

                    IL_0154: ldarg.2 // _syncType
                    IL_0155: ldc.i4.3 // ItemManager.ItemSyncType.SceneLoad
                    IL_0156: bne.un.s <endOfFirstIf> // jump away
                    : call ShouldLogAllow (only added on debug)
                    IL_016A: ldc.i4.1 // load true
                    IL_016B: stloc.s V_7 // save into flag2
                    IL_016D: ldloc.s   V_7 (<endOfFirstIf>)

                    */

                var Item_IsChildOfEquipmentSlot = AccessTools.Method(typeof(Item), nameof(Item.IsChildOfEquipmentSlot));

                // find the if // if (_syncType == ItemManager.ItemSyncType.SceneLoad)
                cur.MatchForward(false, // start at the beginning
                    new CodeMatch(OpCodes.Ldloc_S),
                    new CodeMatch(OpCodes.Call, Item_IsChildOfEquipmentSlot)
                    );
                Randomizer.DebugTrace($"found match at: {cur.Pos}, op: {cur.Instruction}");
                var array3 = cur.Operand;

                cur.RemoveInstructions(6); // includes the current one

#if DEBUG
                // insert our debug log
                cur.Insert(
                    new CodeInstruction(OpCodes.Ldloc_S, array3), // put "array3" on the stack
                    Transpilers.EmitDelegate<Action<string[]>>((itemInfos) =>
                    {
                        if (Item.IsChildOfEquipmentSlot(itemInfos) && !Item.IsHandEquipment(itemInfos))
                            Randomizer.DebugTrace($"allowing item to be loaded: {Item.GetItemUIDFromSyncInfo(itemInfos)} ({Item.GetItemIDFromSyncInfo(itemInfos)})");
                    })
                );
#endif

                // debug log
                var e = cur.InstructionEnumeration();
                /*foreach (var code in e)
                {
                    Randomizer.DebugTrace(code.ToString());
                }*/
                return e;
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during ItemManager.OnReceiveItemSync transpiler: {e}");
                return instructions;
            }
        }
    }

    // Delete the editor items as they honestly just do shit
    [HarmonyPatch(typeof(StartingEquipment), nameof(StartingEquipment.Init))]
    static class StartingEquipmentInitPatch
    {
        [HarmonyPrefix]
        internal static void Prefix(StartingEquipment __instance)
        {
            var equipment = __instance.m_character.Inventory.Equipment;
            foreach (var equip in __instance.m_startingEquipment)
            {
                if (equip != null)
                {
                    var slot = equipment.EquipmentSlots[(int)equip.EquipSlot];
                    if (slot == null)
                        continue;

                    // if we've got a editor item, it will spawn even though we have the startingequipment
                    // so we've gotta delete/remove that, so it wont be added later into the inventory
                    if (slot.m_editorEquippedItem)
                    {
                        Randomizer.DebugTrace($"Deleting editor item: {slot.m_editorEquippedItem}");
                        ItemManager.Instance.DestroyItem(slot.m_editorEquippedItem);
                        slot.m_editorEquippedItem = null;
                    }
                }
            }
        }
    }

    // Fix spinning //TODO: find more efficient function:
    // CharacterEquipment.EquipItem seems to fail at times (ie on load)
    // StartingEquipment.Init isn't enough alone (doesnt work after load)
    [HarmonyPatch(typeof(Item), nameof(Item.UpdateParentChange), new Type[] { typeof(bool) })]
    static class ItemUpdateParentChangePatch
    {
        [HarmonyPostfix]
        internal static void Postfix(Item __instance)
        {
            // no owner, owner is player, owner dead
            if (!__instance.OwnerCharacter || !__instance.OwnerCharacter.IsAI || __instance.OwnerCharacter.IsDead)
                return;

            // only armor causes the spinning
            if (__instance is not Armor)
                return;

            Randomizer.DebugTrace($"UpdateParentChange item {__instance} on owner: {__instance.OwnerCharacter}. Running AnimFix");
            Randomizer.Instance.StartCoroutine(StartingEquipmentInitItemsPatch.DelayedAnimFix(__instance.OwnerCharacter));
        }
    }

    [HarmonyPatch(typeof(Item), nameof(Item.OnAwake))]
    static class ItemOnAwakePatch
    {
#if DEBUG
        [HarmonyDebug]
#endif
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var cur = new CodeMatcher(instructions);
                /*
                // Always set Savetype of monster items to saveable
                if (this.ItemID >= 2400000 && this.ItemID < 2500000)
                {
                    this.SaveType = Item.SaveTypes.Parent_NonSavable;
                ...

                    IL_0038: ldarg.0 // this
                    IL_0039: ldc.i4.3 // Parent_NonSaveable
                    IL_003A: stfld valuetype Item / SaveTypes Item::SaveType

                =>

                if (this.ItemID >= 2400000 && this.ItemID < 2500000)
                {
                    this.SaveType = Item.SaveTypes.Savable;
                    DebugLog();
                ...

                    IL_0154: ldarg.2 // this
                    IL_0155: ldc.i4.0 // ItemManager.ItemSyncType.SceneLoad
                    IL_003A: stfld valuetype Item / SaveTypes Item::SaveType
                    : call Log

                        */

                var Item_SaveTypeField = AccessTools.Field(typeof(Item), nameof(Item.SaveType));

                // find the save type set // this.SaveType = Item.SaveTypes.Parent_NonSavable;
                cur.MatchForward(false, // start at the beginning
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldc_I4_3),
                    new CodeMatch(OpCodes.Stfld, Item_SaveTypeField)
                    );
                Randomizer.DebugTrace($"found match at: {cur.Pos}, op: {cur.Instruction}");

                cur.Advance(1); // go to ldc_i4_3
                cur.Instruction.opcode = OpCodes.Ldc_I4_0;

#if DEBUG
                // insert our debug log
                cur.Advance(1); // go past the stfld
                cur.Insert(
                    new CodeInstruction(OpCodes.Ldarg_0), // put "this" on the stack
                    Transpilers.EmitDelegate<Action<Item>>((item) =>
                    {
                        Randomizer.DebugTrace($"forcing item: {item} to be saveable");
                    })
                );
#endif

                //TODO: also remove the uid set?

                // debug log
                var e = cur.InstructionEnumeration();
                /*foreach (var code in e)
                {
                    Randomizer.DebugTrace(code.ToString());
                }*/
                return e;
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during Item.OnAwake transpiler: {e}");
                return instructions;
            }
        }
    }
}
