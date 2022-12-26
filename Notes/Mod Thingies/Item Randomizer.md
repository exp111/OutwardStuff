## Item Spawn Randomizer
## TODO
- fix enemies sometimes being saved/spawned with the default weapon/armor
- randomizes ultimate backpack items (held by the backpack) => probably because of no localplayer check in `StartingEquipmentInitPatch`
- fix enchanted weapons spawning their non enchanted part when picked up
- some enemies get their original armor/weapon => delete them?
- ~~fix enemies spinning sometimes (call Character.FixAnimationBugCheat after equipping/setting new armor?)~~ => hasnt happened again?
- didnt randomize without config manager/in first area??
- randomize spawned items
- config options to not randomize backpack?
- quest rewards?
- recheck manual blacklist

## Details
Randomize:
- Chest Drops
- Item Drops from enemies
- Enemy weapons?
- Gatherables (ore minining)
- merchants
- spawned items?

**DropTables**: 
- does drops, gatherables and chests 
- Generates items into containers from the `m_itemDrops` list
- `LoadInfos` loads saveData, `ToSaveData` saves data
- is data stored in assets or generated? => stored as prefab like `DropTable_JunkPile_High.prefab`
- DropTables have `SaveIdentifier`/`UID` => not the same for each drop table though => use `Object.name`?

**Dropable**:
- References the droptables, probably manages which one is called (entity->dropable->droptable).
-  `Dropable.GenerateContents` is called at interaction time (gatherable + chests) and death time (enemies, `LootableOnDeath`)
- referenced by `TreasureChest.DropPrefabsGen` and `Gatherable/SelfFilledItemContainer.DropPrefabs` => change those
- prefabs don't have the lists  `m_allGuaranteedDrops` + `m_mainDropTables` initialized. this is done in `InitReferences()`. conditional tables/drops are serialized

**GuarantedDrop**: 
- ~~also probably needs hooking? but probably best to hook `ItemDropper.GenerateItem` then to minimize hooks needed (DropTables also inherits that)~~
- dont forget to also change those

**BasicItemDrop**:
* specifies an item through `ItemID`. references item through `ItemRef`. -1 if null
* sets `ItemRef` through `ResourcesPrefabManager.Instance.GetItemPrefab`, which draws from `ResourcesPrefabManager.ITEM_PREFABS`

### Specific Classes

**Merchant**:
- `Merchant.m_dropableInventory`: contains dropable with droptables, set in `InitDropTableGameObject`
- `m_merchantInventoryTablePrefab`: contains prefab
- `WaitForItemBundleLoaded`: waits for resourcemanager, then calls init. actual set of the prefab isn't done here (probably done through resourcemanager or some unity shittery). 
- hook into `Initialize`
- generates items into `MerchantPouch`

**TreasureChest**:
- hook into `InitDrops`
- does handling for chests and other item piles.
- `TreasureChest.DropPrefabsGen` contains droppable prefabs

**LootableOnDeath**:
- Does stuff for enemies
- `LootDrops` + `SkinDrops` contain prefabs
- hook before `Start()`; 

**SelfFilledItemContainer**:
* Does `Gatherable` and also `TreasureChest`(?)
* reads from `m_dropPrefabs`/`DropPrefabs`; TODO: also check other dropables which go into `m_drops` in `InitDrops`. whats up with those.
* hook into `StartInit` or `InitDrops`

**StartingEquipment**:
- weapons are given to `m_character.Inventory.MakeLootable` with `LootableOnDeath.DropWeapons`
- Saved in `m_startingEquipmentTable.Equipments` or `m_startingEquipment` (mostly the later)
- ~~hook into `InitEquipment`~~ hook into `InitItems` as we also need to change stuff in `StartingPouchItems`
- use `m_character.m_name` as key? maybe `m_nameLocKey`?
- problem: only uses prefab if `LastLoadedSave` != null, therefore the enemies weren't properly loaded/initialized before, so they dont have the new weapons now


##### Finding out where weapons and armor of NPCs are loaded from:
Info:
- `CharacterEquipment.EquipWithoutAssociating` is not called

**After** (armor & weapons aren't equipped):
- `Character.Awake` (doesn't even have inventory)
- `Character.ProcessInit`
- `CharacterInventory.ProcessStart`
- `CharacterEquipment.ProcessAwake`
- `CharacterManager.AddCharacter`

Somewhere here is the armor equipping

**Mid** (armor is equipped):
- `CharacterManager.LoadAiCharactersFromSave`
- ``CharacterInventory.LoadCharacterSave``

Somewhere here is the weapon equipping

**Before** (armor & weapons are equipped):
- `StartingEquipment.Init`
- `Character.OnOverallLoadingDone`

=> weapons loaded in `EnvironmentSave.ApplyData`->`ItemManager.LoadItems` and saved in `EnvironmentSave.PrepareSave` from `ItemManager.Instance.WorldItems` => can't do anything here though
=> TODO: question is still: why are some "immune" to the starting equip? too far away? some other spawn shittery (because of a defeat maybe?)? is the item maybe saved wrong?
Code for loading weapons (`ItemManager.OnReceiveItemSync`):
![[Pasted image 20220103233830.png]]
Example Save for weapon (Simple Bow, ID 2200000), first bow is in Pouch, second one is equipped/inventory:
```xml
 <BasicSaveData>
      <Identifier xsi:type="xsd:string">thn-Tl5bB0aJsymNn-L83Q</Identifier>
      <SyncData><?xml version="1.0" encoding="utf-16"?><Item><UID>thn-Tl5bB0aJsymNn-L83Q</UID><ID>2200000</ID><Hierarchy>1Pouch_4u_4DI0VdUyRRRiqgmciFQ;0</Hierarchy><Durability>250</Durability><ItemExtensions>WeaponLoadoutItem;-1;0</ItemExtensions><AquireTime>32.5</AquireTime><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item></SyncData>
    </BasicSaveData>
    <BasicSaveData>
      <Identifier xsi:type="xsd:string">2P4CVxx4L0CJm7I3pWSHcQ</Identifier>
      <SyncData><?xml version="1.0" encoding="utf-16"?><Item><UID>2P4CVxx4L0CJm7I3pWSHcQ</UID><ID>2200000</ID><Hierarchy>2w4MKaS5qRk6BOYi49mSVfQ</Hierarchy><Durability>250</Durability><ItemExtensions>WeaponLoadoutItem;-1;0</ItemExtensions><IsNew>1</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item></SyncData>
    </BasicSaveData>
```
armor isn't saved in in `.envc` or `worldc` files, but rather gets loaded from the npc prefab. weird that `StartingEquipment` also overwrites that armor

**EnvironmentSave**:
- saves characters in `PrepareSave`, which gets called by `SaveInstance.Save`
- loads in `ApplyData`, which gets called by `SaveInstance.ApplyEnvironment`

**CharacterManager**:
- handles all characters in scene
- calls `UpdateCharacterInitialization` every tick which inits peoples
- applies save in `LoadAiCharactersFromSave`



### ideas:
**hook place**:
- hook while putting item in there:
- - +: easy
- - -:  completely random, drop tables are basically useless, can farm one enemy till you get all items
- hook while init and change the drop lists
- - +: keeps tables and such
- - -: more hooks needed, needs some thunking to save that (or just remove it)

**save**:
- save new droptables by droptable name
- only save seed and generate prefabs by combining seed with hash of name or smth? then set seed before generating each table

**filter**:
* skip: 
* - currency
* - gold ingots
* - quests?
* - test items? filter by generic test icon?
* try to check for key items?
* keep in the same category (MeleeWeapon only gets swapped to another MeleeWeapon)

**other**:
- set sell value to original value?

### Problem: Enemies keep original weapon + armor
problem: all enemies have their original equipment in their inv after idea
problem cause idea: enemy still has a reference to the original droptable somewhere

debug ideas:
- look where loot is generated in code and look which fields are (can look at runtime)
- look directly after replacing the items if the reference is somewhere (NEEDS dnspy debug or unityexplorer?)
- look how the startingequipment is inserted, if there is smth else

#### look where loot is generated:
`LootableOnDeath.OnDeath`:
- makes items from pouch public by letting the player access the pouch as a loot container => original weapon should be contained there
- also drops weapons
- all done from `CharacterInventory.MakeLootable`

test: prefix + postfix `OnDeath` and look into pouch
```csharp
		[HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        internal static void DebugPrefix(LootableOnDeath __instance)
        {
            var pouch = __instance.m_character.m_inventory.m_inventoryPouch;
            if (pouch)
            {
                Randomizer.DebugTrace($"Prefix Pouch items for {__instance.m_character.Name}");
                foreach (var item in pouch.m_containedItems.Values)
                {
                    Randomizer.DebugTrace($"- {item.Name} ({item.ItemID})");
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        internal static void DebugPostfix(LootableOnDeath __instance)
        {
            var pouch = __instance.m_character.m_inventory.m_inventoryPouch;
            if (pouch)
            {
                Randomizer.DebugTrace($"Postfix Pouch items for {__instance.m_character.Name}");
                foreach (var item in pouch.m_containedItems.Values)
                {
                    Randomizer.DebugTrace($"- {item.Name} ({item.ItemID})");
                }
            }
        }
```
=> weapon exists beforehand in pouch

#### look how the startingequipment is inserted
`StartingEquipment.InitItems`:
- goes through `StartingPouchItems` and `m_startingEquipmentTable`+`m_startingEquipment` (in `InitEquipment`) and spawns + equips these

maybe some editor prefab stuff contains the original weapon?
test: prefix + postfix InitItems
```cs
[HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        internal static void DebugPrefix1(StartingEquipment __instance)
        {
            var pouch = __instance.m_character.m_inventory.m_inventoryPouch;
            if (pouch)
            {
                Randomizer.DebugTrace($"First Prefix Pouch items for {__instance.m_character.Name}");
                foreach (var item in pouch.m_containedItems.Values)
                {
                    Randomizer.DebugTrace($"- {item.Name} ({item.ItemID})");
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        internal static void DebugPrefix2(StartingEquipment __instance)
        {
            var pouch = __instance.m_character.m_inventory.m_inventoryPouch;
            if (pouch)
            {
                Randomizer.DebugTrace($"Prefix Last Pouch items for {__instance.m_character.Name}");
                foreach (var item in pouch.m_containedItems.Values)
                {
                    Randomizer.DebugTrace($"- {item.Name} ({item.ItemID})");
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.First)]
        internal static void DebugPostfix(StartingEquipment __instance)
        {
            var pouch = __instance.m_character.m_inventory.m_inventoryPouch;
            if (pouch)
            {
                Randomizer.DebugTrace($"Postfix Pouch items for {__instance.m_character.Name}");
                foreach (var item in pouch.m_containedItems.Values)
                {
                    Randomizer.DebugTrace($"- {item.Name} ({item.ItemID})");
                }
            }
        }
```
=> item isnt there yet => added somewhere in between

test: hook pouch item add and see what and when adds the item
```cs
[HarmonyPatch(typeof(ItemContainer), nameof(ItemContainer.AddItem), new Type[] { typeof(Item), typeof(bool) })]
    static class ItemContainerAddItemPatch
    {
        [HarmonyPrefix]
        internal static void Prefix(ItemContainer __instance, Item _item, bool _stackIfPossible)
        {
            Randomizer.DebugTrace($"Adding item {_item} ({_item.UID}) to {__instance.OwnerCharacter}");
            Randomizer.DebugTrace(new StackTrace().ToString());
        }
    }
```
=>
```
[Message:Randomizer] Adding item 2400260_PearlBirdBeak_U9gxjUQ2X0 (MeleeWeapon) to PearlBird_pixBJFjn00mZUzoqjRo55g (Character)

[Message:Randomizer]   at Randomizer.ItemContainerAddItemPatch.Prefix (ItemContainer __instance, Item _item, System.Boolean _stackIfPossible) [0x00000] in <bc80ab20b97d45d9b1915db2cac695e7>:0

  at ItemContainer.DMD<ItemContainer::AddItem> (ItemContainer , Item , System.Boolean ) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.Store (ItemContainer _parentContainer) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.UpdateParentChange (System.Boolean _updateNewParent) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.UpdateParentChange () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.CheckHasChanged () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Equipment.CheckHasChanged () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Weapon.CheckHasChanged () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.UpdateProcessing () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Equipment.UpdateProcessing () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Weapon.UpdateProcessing () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at MeleeWeapon.UpdateProcessing () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.DoUpdate () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at ItemManager+<UpdateItems>d__72.MoveNext () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at UnityEngine.SetupCoroutine.InvokeMoveNext (System.Collections.IEnumerator enumerator, System.IntPtr returnValueAddress) [0x00000] in <ad199b1c67244da3a5ed230e5d202f21>:0
```
=> Weapon notices that it has been removed/not equipped => adds itself to inventory
`ItemManager.UpdateItems` has `m_worldItems` array
test: remove equipment from `m_worldItems`
```cs
						if (ItemManager.Instance.WorldItems.ContainsKey(equipment.UID))
                        {
                            Randomizer.DebugTrace($"worlditems contains: {equipment}. removing");
                            ItemManager.Instance.WorldItems.Remove(equipment.UID);
                        }
```
=> not contained in worlditems
test: delete equipment
```cs
ItemManager.Instance.DestroyItem(equipment);
```
=> doesn't work either
=> logging the replaced item shows it doesnt have a uid, so it cant be this specific instance; probably spawned somewhere else?
is it spawned beforehand? should we go through all worlditems and look if itemid and owner matches?
test: see which items are added to worlditems => `ItemManager.ItemHasBeenAdded`
```cs
	[HarmonyPatch(typeof(ItemManager), nameof(ItemManager.ItemHasBeenAdded))]
    static class ItemManagerAddedPatch
    {
        [HarmonyPrefix]
        internal static void Prefix(ItemManager __instance, Item _newItem)
        {
            Randomizer.DebugTrace($"ItemHasBeenAdded item {_newItem} ({_newItem.UID}) to {_newItem.OwnerCharacter}");
            Randomizer.DebugTrace(new StackTrace().ToString());
        }
    }
```
=> added at lvl load, no owner => different to items in StartingEquipment
```
[Message:Randomizer] ItemHasBeenAdded item 2400260_PearlBirdBeak (MeleeWeapon) (U9gxjUQ2X0u6AthdtR3YGw) to

[Message:Randomizer]   at Randomizer.ItemManagerAddedPatch.Prefix (ItemManager __instance, Item _newItem) [0x00000] in <64fdf562b3e04f1286e6838b56986926>:0

  at ItemManager.DMD<ItemManager::ItemHasBeenAdded> (ItemManager , Item& ) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.RegisterUID () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.BaseInit () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Weapon.BaseInit () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.StartInit () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Equipment.StartInit () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.ProcessInit () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at ItemManager.UpdateItemInitialization () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at ItemManager.Update () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0
```
vs
```
[Message:Randomizer] Adding item 2400260_PearlBirdBeak_U9gxjUQ2X0 (MeleeWeapon) (U9gxjUQ2X0u6AthdtR3YGw) to PearlBird_pixBJFjn00mZUzoqjRo55g (Character)

[Message:Randomizer]   at Randomizer.ItemContainerAddItemPatch.Prefix (ItemContainer __instance, Item _item, System.Boolean _stackIfPossible) [0x00000] in <64fdf562b3e04f1286e6838b56986926>:0

  at ItemContainer.DMD<ItemContainer::AddItem> (ItemContainer , Item , System.Boolean ) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.Store (ItemContainer _parentContainer) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.UpdateParentChange (System.Boolean _updateNewParent) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.UpdateParentChange () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.CheckHasChanged () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Equipment.CheckHasChanged () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Weapon.CheckHasChanged () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.UpdateProcessing () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Equipment.UpdateProcessing () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Weapon.UpdateProcessing () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at MeleeWeapon.UpdateProcessing () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.DoUpdate () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at ItemManager+<UpdateItems>d__72.MoveNext () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at UnityEngine.SetupCoroutine.InvokeMoveNext (System.Collections.IEnumerator enumerator, System.IntPtr returnValueAddress) [0x00000] in <ad199b1c67244da3a5ed230e5d202f21>:0
```
=> called from UpdateItemInitialization => someone requested item initialization. `InitItems`? `CreateItemFromData`?
test: `ItemManager.RequestItemInitialization` hook:
```cs
[HarmonyPatch(typeof(ItemManager), nameof(ItemManager.RequestItemInitialization))]
    static class ItemManagerRequestInitPatch
    {
        [HarmonyPrefix]
        internal static void Prefix(ItemManager __instance, Item _item)
        {
            Randomizer.DebugTrace($"RequestItemInitialization item {_item} ({_item.UID}) to {_item.OwnerCharacter}");
            Randomizer.DebugTrace(new StackTrace().ToString());
        }
    }
```
=>
```cs
[Message:Randomizer] RequestItemInitialization item 2400260_PearlBirdBeak (MeleeWeapon) (-vLAAI0N1UWq5-XgDaI2rw) to

[Message:Randomizer]   at Randomizer.ItemManagerRequestInitPatch.Prefix (ItemManager __instance, Item _item) [0x00000] in <83cf0ca9fefe482281e98bcaa0e1c7dd>:0

  at ItemManager.DMD<ItemManager::RequestItemInitialization> (ItemManager , Item ) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0

  at Item.DMD<Item::Start> (Item ) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0
```
=> dead end => or can we look which func instantiates items?
=> `ItemManager.GenerateItem`? calls `ResourcesPrefabManager.Instance.GenerateItem`, which calls `Instantiate<Item>`
test: hook `ItemManager.GenerateItem` => cant find item => maybe loaded from save?
=> hook `Instantiate<T>` ?

=> instead try to find out where the added item contains a reference to the parent?
=> `EquipmentSlot` contains `m_editorEquippedItem`, which is the item we're looking at...
=> just delete that if we're replacing the slot?