using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Randomizer
{
    public static class RandomItemLibrary
    {
        static HashSet<int> Blacklist;

        static Item[] AllItems;
        static Item[] MonsterWeapons;
        static Dictionary<Type, List<Item>> AllFilteredItems;
        static Dictionary<Weapon.WeaponType, Dictionary<Type, List<Item>>> WeaponsByType;
        static Dictionary<EquipmentSlot.EquipmentSlotIDs, Dictionary<Type, List<Item>>> EquipmentBySlot;

        static Tag MonsterWeaponTag;

        static readonly HashSet<int> ManualBlacklistIDs = new()
        {
            2300250, //Virgin Shield
            2020170, //CalixaMaceGun // weird model
            2400540, //WolfgangRunicSword //can't be picked up, disappears immediatelly
            2100221, //Great Bloodsword // can't be picked up
            2000221, //Blood Sword // can't be picked up
            2130230, //Blood Spear // can't be picked up
            2150042, // Elite trog queen staff
            2110021, 2110022,  // NPC Marble Greataxes // can't be picked up //TODO: force physics on those?
        };

        internal static IEnumerator Init()
        {
            // Wait one frame in case items are being setup at OnPacksLoaded.
            Randomizer.DebugTrace("Initializing RandomItemLibrary...");
            yield return null;

            GenerateCache();

            // fix uids
            foreach (var prefab in ResourcesPrefabManager.ITEM_PREFABS.Values)
            {
                if (!string.IsNullOrEmpty(prefab.UID))
                {
                    Randomizer.DebugTrace($"Removing uid of {prefab}");
                    prefab.UID = UID.Empty;
                }
            }
        }

#if DEBUG
        // used to debug print differences
        static HashSet<int> debugPreviousBlacklist;
#endif

        public static void GenerateCache()
        {
            try
            {
                Randomizer.DebugTrace("Generating Cache...");
#if DEBUG
                if (Blacklist != null)
                {
                    debugPreviousBlacklist = new HashSet<int>();
                    foreach (var item in Blacklist)
                    {
                        debugPreviousBlacklist.Add(item);
                    }
                }
#endif
                MonsterWeaponTag = TagSourceManager.Instance.GetTag("196");
                Stopwatch sw = new();
                sw.Start();

                Blacklist = new();
                AllFilteredItems = new();
                EquipmentBySlot = new();
                WeaponsByType = new();

                List<Item> allItems = new();
                List<Item> monsterWeapons = new();
                foreach (var item in ResourcesPrefabManager.ITEM_PREFABS.Values)
                {
                    if (item.HasTag(MonsterWeaponTag))
                    {
                        monsterWeapons.Add(item);
                        continue;
                    }

                    if (ShouldBlacklist(item))
                    {
                        Blacklist.Add(item.ItemID);
                        continue;
                    }

                    allItems.Add(item);

                    // Add to filtered list
                    Type type = item.GetType();
                    if (!AllFilteredItems.ContainsKey(type))
                        AllFilteredItems.Add(type, new());
                    AllFilteredItems[type].Add(item);

                    // Add equipment to slot dictionary
                    if (item is Weapon weapon)
                    {
                        Weapon.WeaponType weaponType = weapon.Type;
                        if (!WeaponsByType.ContainsKey(weaponType))
                            WeaponsByType.Add(weaponType, new());
                        if (!WeaponsByType[weaponType].ContainsKey(type))
                            WeaponsByType[weaponType].Add(type, new());
                        WeaponsByType[weaponType][type].Add(item);
                    }
                    else if (item is Equipment equipment)
                    {
                        EquipmentSlot.EquipmentSlotIDs slot = equipment.EquipSlot;
                        if (!EquipmentBySlot.ContainsKey(slot))
                            EquipmentBySlot.Add(slot, new());
                        if (!EquipmentBySlot[slot].ContainsKey(type))
                            EquipmentBySlot[slot].Add(type, new());
                        EquipmentBySlot[slot][type].Add(item);
                    }
                }

                AllItems = allItems.ToArray();
                MonsterWeapons = monsterWeapons.ToArray();

                sw.Stop();
                Randomizer.Log.LogMessage($"Initialized item library in {sw.ElapsedMilliseconds} milliseconds.");
                Randomizer.DebugLog($"Blacklist contains {Blacklist.Count} items");
                
#if DEBUG
                if (debugPreviousBlacklist != null)
                {
                    // Check the differences
                    List<int> added = new();
                    foreach (var item in Blacklist)
                    {
                        if (!debugPreviousBlacklist.Contains(item))
                            added.Add(item);
                    }
                    List<int> removed = new();
                    foreach (var item in debugPreviousBlacklist)
                    {
                        if (!Blacklist.Contains(item))
                            removed.Add(item);
                    }
                    Randomizer.Log.LogMessage($"Added to Blacklist: {added.Join(delimiter: ",")}");
                    Randomizer.Log.LogMessage($"Removed from Blacklist: {removed.Join(delimiter: ",")}");
                }
#endif
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during RandomItemLibrary.GenerateCache: {e}");
            }
        }

        // Only used when building our cache. Use Blacklist.Contains(item) otherwise.
        static bool ShouldBlacklist(Item item)
        {
            try
            {
                if ((item.ItemID < 2000000 // <- these are dev items
                    && item.ItemID > 0) // don't outright blacklist modded items
                    || item is Skill  // we dont want to give people any of these types of items, right? (Skill, etc)
                    || item is Building
                    || item is Quest
                    || item is Blueprint
                    || item is CraftingStation
                    || item is WrittenNote
                    || (item.HasDefaultIcon && item is not Equipment)  // <- HasDefaultIcon means the item has no icon, ie probably not a finished item; exception for equipment
                    || ManualBlacklistIDs.Contains(item.ItemID)
                    || (!Randomizer.RandomizeKeys.Value && item.ItemID.ToString().StartsWith("5600")) // <- keys and special items
                    || item.Name.Trim() == "-" // <- unfinished items
                    || item.Description.Trim() == "-" // <- mostly used for placed tents/crafting stations
                    || item.Name.Contains("stat boost", StringComparison.OrdinalIgnoreCase) // <- used to give enemies more stats
                    || item.Name.Contains("removed", StringComparison.OrdinalIgnoreCase) // <- unfinished items
                    || (item is Equipment equipment
                        && (equipment.RequiredPType == PlayerSystem.PlayerTypes.Trog // <- cant use trog items
                        || !equipment.GetComponent<ItemStats>() // <- equipment has no stats
                        || string.IsNullOrWhiteSpace(equipment.VisualPrefabPath))) // <- equipment has no visuals
                        || (item.HasDefaultIcon && Randomizer.HideEquipmentNoIcon.Value))
                {
                    return true; // should blacklist
                }

                return false; // dont blacklist
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during RandomItemLibrary.ShouldBlacklist: {e}. {Environment.NewLine}Blacklisting item.");
                return true; // blacklist cause it made some issues
            }
        }

        public static Item Randomize(Random seed, Item original, bool filter, bool isStartingEquipment = false)
        {
            if (!original)
                return original;

            if (Blacklist.Contains(original.ItemID))
                return original;

            Item ret;

            // monster weapons only return monster weapons
            if (original.HasTag(MonsterWeaponTag))
            {
                int next = seed.Next(0, MonsterWeapons.Length);
                return MonsterWeapons[next];
            }

            Type type = original.GetType();

            if (isStartingEquipment && original is Equipment equipment)
            {
                if (equipment is Weapon weapon)
                {
                    Weapon.WeaponType weaponType = weapon.Type;
                    if (!WeaponsByType[weaponType].ContainsKey(type))
                        return original;

                    List<Item> list = WeaponsByType[weaponType][type];
                    ret = list[seed.Next(0, list.Count)];
                }
                else
                {
                    EquipmentSlot.EquipmentSlotIDs slot = equipment.EquipSlot;
                    if (!EquipmentBySlot[slot].ContainsKey(type))
                        return original;

                    List<Item> list = EquipmentBySlot[slot][type];
                    ret = list[seed.Next(0, list.Count)];
                }
            }
            else
            {
                if (!filter)
                {
                    int next = seed.Next(0, AllItems.Length);
                    ret = AllItems[next];
                }
                else
                {

                    // Shouldn't really happen, but just in case
                    if (!AllFilteredItems.ContainsKey(type))
                        return original;

                    int next = seed.Next(0, AllFilteredItems[type].Count);
                    ret = AllFilteredItems[type][next];
                }
            }

            // FIXME: this is probably completely useless?
            if (original.GetExtension(nameof(MultipleUsage)) is MultipleUsage origMulti
                && ret.GetExtension(nameof(MultipleUsage)) is MultipleUsage newMulti)
            {
                newMulti.m_currentStack = origMulti.RemainingAmount;
            }

            return ret;
        }

        // If we've got a stackable item that is being transformed into a non stackable
        //  clamp the drop amount to 1 so we dont get 60 different item stacks
        public static void ClampDropAmount<T>(T drop, Item item) where T : BasicItemDrop
        {
            // No need to clamp if only one item is dropped anyways
            if (drop.MaxDropCount <= 1)
                return;

            if (IsStackable(drop.ItemRef, item))
            {
                Randomizer.DebugTrace($"Clamped Item {drop.ItemRef} (now {item}) from {drop.MinDropCount}-{drop.MaxDropCount} to {Math.Min(drop.MinDropCount, 1)}-1");
                drop.MinDropCount = Math.Min(drop.MinDropCount, 1); // 0 || 1
                drop.MaxDropCount = 1;
            }
        }

        public static void ClampDropAmount(ItemQuantity drop, Item item)
        {
            // No need to clamp if only one item is dropped anyways
            if (drop.Quantity <= 1)
                return;

            if (IsStackable(drop.Item, item))
            {
                Randomizer.DebugTrace($"Clamped Item {drop.Item} (now {item}) from {drop.Quantity} to {Math.Min(drop.Quantity, 1)}");
                drop.Quantity = Math.Min(drop.Quantity, 1); // 0 || 1
            }
        }

        private static bool IsStackable(Item drop, Item item)
        {
            // drop itemref hasnt gotten cached info, so m_stackable isnt set, get it manually
            var mStackable = drop.GetComponent<MultipleUsage>();
            var dropIsStackable = mStackable != null; //TODO: also check AutoStack?

            var itemStackable = item.GetComponent<MultipleUsage>();
            var itemIsStackable = itemStackable != null;

            return (dropIsStackable && !itemIsStackable) || // arrows and stuff
                (drop.GroupItemInDisplay && !item.GroupItemInDisplay); // smth like gaberries
        }

        public static void FixCombatStates(Character character, Item item, Item original)
        {
            // If orig item was a weapon used by an AI Combat state, replace the reference to our new item.
            if (original is not Weapon)
                return;

            //TODO: are those maybe not saved and therefore loaded enemies lose their weapons?

            var combatStates = character.GetComponentsInChildren<AISCombat>(true);
            foreach (var state in combatStates)
            {
                for (int i = 0; i < state.RequiredWeapon.Length; i++)
                {
                    if (state.RequiredWeapon[i] == original)
                    {
                        state.RequiredWeapon[i] = item as Weapon;
                        break;
                    }
                }
            }
        }

        public static bool IsSimpleChest(int id) => id == 1000000 || id == 1000010 || id == 1000140;
        public static bool IsOrnateChest(int id) => id == 1000040 || id == 1000050 || id == 1000120;
        public static bool IsTrogChest(int id) => id == 1000060;
        public static bool IsPile(int id) => id == 1000070 || id == 1000080 || id == 1000130 || id == 1001000;
        public static bool IsCash(int id) => id == 1000110;
        public static bool IsStash(int id) => id == 40;
        public static bool IsCorpse(int id) => (id >= 1000090 && id <= 1000101) || id == 1000800;
    }
}
