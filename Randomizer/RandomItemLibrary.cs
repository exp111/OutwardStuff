using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            2020170, //CalixaMaceGun 
            2400540, //WolfgangRunicSword
            2100221, //Great Bloodsword
            2000221, //Blood Sword
            2130230, //Blood Spear
            2150042, // Elite trog queen staff
            2000141, // Cyrene's sword
            2110021, 2110022, 2110023, // NPC Marble Greataxes
            2010002, // Etheral axe
            2400532, // grandmother reach
        };

        internal static IEnumerator Init()
        {
            // Wait one frame in case items are being setup at OnPacksLoaded.
            yield return null;

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

            Randomizer.Log.LogMessage($"Randomizer initialized item library in {sw.ElapsedMilliseconds} milliseconds.");
            sw.Stop();
        }

        // Only used when building our cache. Use Blacklist.Contains(item) otherwise.
        static bool ShouldBlacklist(Item item)
        {
            if (item.ItemID < 2000000 // <- these are dev items
                || item is Skill  // we dont want to give people any of these types of items, right? (Skill, etc)
                || item is Building
                || item is Quest
                || item is Blueprint
                || item is CraftingStation
                || item is WrittenNote
                || item.HasDefaultIcon  // <- HasDefaultIcon means the item has no icon, ie probably not a finished item
                || ManualBlacklistIDs.Contains(item.ItemID)
                || item.ItemID.ToString().StartsWith("5600") // <- keys and special items
                || item.Name.Trim() == "-" // <- unfinished items
                || item.Name.Contains("stat boost", StringComparison.OrdinalIgnoreCase) // <- used to give enemies more stats
                || item.Name.Contains("removed", StringComparison.OrdinalIgnoreCase) // <- unfinished items
                || (item is Equipment equipment 
                    && (equipment.RequiredPType == PlayerSystem.PlayerTypes.Trog // <- cant use trog items
                    || !equipment.GetComponent<ItemStats>() // <- equipment has no stats
                    || string.IsNullOrWhiteSpace(equipment.VisualPrefabPath)))) // <- equipment has no visuals
            {
                return true; // should blacklist
            }

            return false; // dont blacklist
        }

        public static Item Randomize(Random seed, Item original, bool filter, bool isStartingEquipment = false)
        {
            if (!original)
                return original;

            if (Blacklist.Contains(original.ItemID))
                return original;

            Item ret;

            if (original.HasTag(MonsterWeaponTag))
            {
                int next = seed.Next(0, MonsterWeapons.Length);
                ret = MonsterWeapons[next];
            }

            Type type = original.GetType();

            if (isStartingEquipment && original is Equipment equipment)
            {
                if (equipment is Weapon weapon)
                {
                    Weapon.WeaponType weaponType = weapon.Type;
                    if (!WeaponsByType[weaponType].ContainsKey(type))
                        return original;

                    Randomizer.Log.LogMessage($"{weaponType}:{type.Name}");
                    List<Item> list = WeaponsByType[weaponType][type];
                    ret = list[seed.Next(0, list.Count)];
                }
                else
                {
                    EquipmentSlot.EquipmentSlotIDs slot = equipment.EquipSlot;
                    if (!EquipmentBySlot[slot].ContainsKey(type))
                        return original;

                    Randomizer.Log.LogMessage($"{slot}:{type.Name}");
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

            if (original.GetExtension(nameof(MultipleUsage)) is MultipleUsage origMulti
                && ret.GetExtension(nameof(MultipleUsage)) is MultipleUsage newMulti)
            {
                newMulti.m_currentStack = origMulti.RemainingAmount;
            }

            return ret;
        }
    }
}
