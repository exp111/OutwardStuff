## Item Spawn Randomizer
## TODO
- fix enemies sometimes being saved/spawned with the default weapon/armor
- randomizes ultimate backpack items (held by the backpack) => probably because of no localplayer check in `StartingEquipmentInitPatch`
- fix enchanted weapons spawning their non enchanted part when picked up
- didnt randomize without config manager/in first area??
- randomize spawned items
- config options to not randomize backpack?
- quest rewards?
- recheck manual blacklist
- fix spinning on save load
- fix monster weapons not being saved

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


### Finding out where weapons and armor of NPCs are loaded from:
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

### Reload shenanigans part 2
look into save files if weapon + armor is saved
if not add the custom armor?
save path: `Outward_Defed\SaveGames\76561198043682465\Save_NZ5po8c4ekyb8a1MaxxvFA\20221226014158
`
contains `.defedc` and `.deenvc` => gzipped
test: find small area with few human enemies => tutorial => new character, save, look into file
Conceiled knight in area => uid XVuyIaCAVkatv89kId9Uqw
CharData:
```
<CharacterSaveData>

      <NewSave>false</NewSave>

      <VisualData>

        <Gender>Male</Gender>

        <HairStyleIndex>0</HairStyleIndex>

        <HairColorIndex>0</HairColorIndex>

        <SkinIndex>0</SkinIndex>

        <HeadVariationIndex>0</HeadVariationIndex>

      </VisualData>

      <UID>XVuyIaCAVkatv89kId9Uqw</UID>

      <Health>1000</Health>

      <ManaPoint>0</ManaPoint>

      <Position>

        <x>1648.33752</x>

        <y>6.354431</y>

        <z>1844.27515</z>

      </Position>

      <Forward>

        <x>0</x>

        <y>0</y>

        <z>1</z>

      </Forward>

      <Money>0</Money>

      <EncounterTime>-1</EncounterTime>

      <IsAiActive>false</IsAiActive>

      <StatusList />

    </CharacterSaveData>
```
=> see which items contain reference to uid
```
<BasicSaveData>

      <Identifier xsi:type="xsd:string">JLQP25kHAkG2v--QgfAxLA</Identifier>

      <SyncData>&lt;?xml version="1.0" encoding="utf-16"?&gt;&lt;Item&gt;&lt;UID&gt;JLQP25kHAkG2v--QgfAxLA&lt;/UID&gt;&lt;ID&gt;6000424&lt;/ID&gt;&lt;Hierarchy&gt;1Pouch_XVuyIaCAVkatv89kId9Uqw;0&lt;/Hierarchy&gt;&lt;IsNew&gt;1&lt;/IsNew&gt;&lt;PreviousContainerUID&gt;-&lt;/PreviousContainerUID&gt;&lt;/Item&gt;</SyncData>

    </BasicSaveData>

    <BasicSaveData>

      <Identifier xsi:type="xsd:string">dsCOshzHJ0uhOOVoM35L-A</Identifier>

      <SyncData>&lt;?xml version="1.0" encoding="utf-16"?&gt;&lt;Item&gt;&lt;UID&gt;dsCOshzHJ0uhOOVoM35L-A&lt;/UID&gt;&lt;ID&gt;3000086&lt;/ID&gt;&lt;Hierarchy&gt;2XVuyIaCAVkatv89kId9Uqw&lt;/Hierarchy&gt;&lt;Durability&gt;90&lt;/Durability&gt;&lt;IsNew&gt;1&lt;/IsNew&gt;&lt;PreviousContainerUID&gt;-&lt;/PreviousContainerUID&gt;&lt;/Item&gt;</SyncData>

    </BasicSaveData>

    <BasicSaveData>

      <Identifier xsi:type="xsd:string">rmRDHBia-kiM8cMj79OuqQ</Identifier>

      <SyncData>&lt;?xml version="1.0" encoding="utf-16"?&gt;&lt;Item&gt;&lt;UID&gt;rmRDHBia-kiM8cMj79OuqQ&lt;/UID&gt;&lt;ID&gt;3000150&lt;/ID&gt;&lt;Hierarchy&gt;2XVuyIaCAVkatv89kId9Uqw&lt;/Hierarchy&gt;&lt;Durability&gt;440&lt;/Durability&gt;&lt;IsNew&gt;1&lt;/IsNew&gt;&lt;PreviousContainerUID&gt;-&lt;/PreviousContainerUID&gt;&lt;/Item&gt;</SyncData>

    </BasicSaveData>

    <BasicSaveData>

      <Identifier xsi:type="xsd:string">DSeOGIol00aoz1357isSRA</Identifier>

      <SyncData>&lt;?xml version="1.0" encoding="utf-16"?&gt;&lt;Item&gt;&lt;UID&gt;DSeOGIol00aoz1357isSRA&lt;/UID&gt;&lt;ID&gt;3000312&lt;/ID&gt;&lt;Hierarchy&gt;2XVuyIaCAVkatv89kId9Uqw&lt;/Hierarchy&gt;&lt;Durability&gt;375&lt;/Durability&gt;&lt;IsNew&gt;1&lt;/IsNew&gt;&lt;PreviousContainerUID&gt;-&lt;/PreviousContainerUID&gt;&lt;/Item&gt;</SyncData>

    </BasicSaveData>

    <BasicSaveData>

      <Identifier xsi:type="xsd:string">pNTqToXLs0-UP91WPH56ng</Identifier>

      <SyncData>&lt;?xml version="1.0" encoding="utf-16"?&gt;&lt;Item&gt;&lt;UID&gt;pNTqToXLs0-UP91WPH56ng&lt;/UID&gt;&lt;ID&gt;2000265&lt;/ID&gt;&lt;Hierarchy&gt;2XVuyIaCAVkatv89kId9Uqw&lt;/Hierarchy&gt;&lt;Durability&gt;320&lt;/Durability&gt;&lt;IsNew&gt;1&lt;/IsNew&gt;&lt;PreviousContainerUID&gt;-&lt;/PreviousContainerUID&gt;&lt;/Item&gt;</SyncData>

    </BasicSaveData>

    <BasicSaveData>

      <Identifier xsi:type="xsd:string">czYJcGBbF0quYdCb_87C4g</Identifier>

      <SyncData>&lt;?xml version="1.0" encoding="utf-16"?&gt;&lt;Item&gt;&lt;UID&gt;czYJcGBbF0quYdCb_87C4g&lt;/UID&gt;&lt;ID&gt;2300080&lt;/ID&gt;&lt;Hierarchy&gt;2XVuyIaCAVkatv89kId9Uqw&lt;/Hierarchy&gt;&lt;Durability&gt;85&lt;/Durability&gt;&lt;IsNew&gt;1&lt;/IsNew&gt;&lt;PreviousContainerUID&gt;-&lt;/PreviousContainerUID&gt;&lt;/Item&gt;</SyncData>

    </BasicSaveData>

  </ItemList>
```
=> randomized starting item (gold ingot => molepouch skin), armor and weapon
=> armor is saved, but apparently not applied/worn on load?
green copalt boots:
```
<?xml version="1.0" encoding="utf-16"?><Item><UID>DSeOGIol00aoz1357isSRA</UID><ID>3000312</ID><Hierarchy>2XVuyIaCAVkatv89kId9Uqw</Hierarchy><Durability>375</Durability><IsNew>1</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>
```
=> look at how player saves/loads armor:
tattered attire: (2kQKcNcKJ1UG5y0RM1qDf1Q is player)
```
<?xml version="1.0" encoding="utf-16"?><Item><UID>3C15kbEIG06czgUz3G28qw</UID><ID>3000130</ID><Hierarchy>2kQKcNcKJ1UG5y0RM1qDf1Q</Hierarchy><Durability>90</Durability><AquireTime>10.96</AquireTime><IsNew>1</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>
```
vs unequipped tattered attire:
```
<?xml version="1.0" encoding="utf-16"?><Item><UID>3C15kbEIG06czgUz3G28qw</UID><ID>3000130</ID><Hierarchy>1Pouch_kQKcNcKJ1UG5y0RM1qDf1Q;0</Hierarchy><Durability>90</Durability><AquireTime>12.45</AquireTime><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>
```
=> hierarchy (Pouch vs character) 
save code for equipment in slot (`Item.GetNetworkData`): 
```cs
else if (transform.GetComponent<EquipmentSlot())
{
	result = string.Format("{0}{1}", "2", this.m_ownerCharacter.UID);
}
```
=> so the armor is saved but not equipped?

load code: `Item.UpdateSyncParent`?
```cs
else if (_hierarchyInfos[0] == '2')
{
	Character character = CharacterManager.Instance.GetCharacter(text);
	if (character && character.IsStartInitDone && this is Equipment)
	{
		EquipmentSlot.EquipmentSlotIDs equipSlot = ((Equipment)this).EquipSlot;EquipmentSlot matchingEquipmentSlot = character.Inventory.GetMatchingEquipmentSlot(equipSlot);
		if (matchingEquipmentSlot && matchingEquipmentSlot.Initialized)
		{
		_newParent = character.Inventory.GetMatchingEquipmentSlotTransform(equipSlot);
			result = true;
		}
	}
}
```
test hook that with unityexplorer:
```
[Message:UnityExplorer] --------------------

bool Item::UpdateSyncParent(string _hierarchyInfos, UnityEngine.Transform& _newParent)

- __instance: 2000265_ObsidianSword_Legacy_pNTqToXLs0 (MeleeWeapon)

- Parameter 0 '_hierarchyInfos': 2XVuyIaCAVkatv89kId9Uqw

- Parameter 1 '_newParent': MainHand (UnityEngine.Transform)

- Return value: True

  

[Message:UnityExplorer] --------------------

bool Item::UpdateSyncParent(string _hierarchyInfos, UnityEngine.Transform& _newParent)

- __instance: 2300080_PlankShield_czYJcGBbF0 (MeleeWeapon)

- Parameter 0 '_hierarchyInfos': 2XVuyIaCAVkatv89kId9Uqw

- Parameter 1 '_newParent': LeftHand (UnityEngine.Transform)

- Return value: True
```
=> only weapons loaded
test: hook method that creates item from save data
`EnvironmentSave.ApplyData` => `ItemManager.LoadItems(ItemList)`
test: hook LoadItems
```cs
static void Postfix(ItemManager __instance, System.Collections.Generic.List<BasicSaveData> __0, bool __1)
{
    try {
       StringBuilder sb = new StringBuilder();
       sb.AppendLine("--------------------");
       sb.AppendLine("void ItemManager::LoadItems(System.Collections.Generic.List<BasicSaveData> _itemSaves, bool _clearAllItems)");
       sb.Append("- __instance: ").AppendLine(__instance.ToString());
       sb.Append("- Parameter 0 '_itemSaves': ").AppendLine(__0?.ToString() ?? "null");
       sb.Append("- Parameter 1 '_clearAllItems': ").AppendLine(__1.ToString());
foreach (var item in __0){
sb.AppendLine($"id: {item.Identifier} - {item.SyncData}");
}
       UnityExplorer.ExplorerCore.Log(sb.ToString());
    }
    catch (System.Exception ex) {
        UnityExplorer.ExplorerCore.LogWarning($"Exception in patch of void ItemManager::LoadItems(System.Collections.Generic.List<BasicSaveData> _itemSaves, bool _clearAllItems):\n{ex}");
    }
}
```
=>
```
[Message:UnityExplorer] --------------------

void ItemManager::LoadItems(System.Collections.Generic.List<BasicSaveData> _itemSaves, bool _clearAllItems)

- __instance: NetworkEntity (ItemManager)

- Parameter 0 '_itemSaves': System.Collections.Generic.List`1[BasicSaveData]

- Parameter 1 '_clearAllItems': True

id: csPu67n3sUCCQ6IQ0A5ZGQ - <?xml version="1.0" encoding="utf-16"?><Item><UID>csPu67n3sUCCQ6IQ0A5ZGQ</UID><ID>1001000</ID><SubClassesData>TreasureChestContainedSilver/0;TreasureChestGenCont/False;</SubClassesData><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: UIdBXve-mUapJv8HT3r76w - <?xml version="1.0" encoding="utf-16"?><Item><UID>UIdBXve-mUapJv8HT3r76w</UID><ID>1000070</ID><SubClassesData>TreasureChestContainedSilver/0;TreasureChestGenCont/False;</SubClassesData><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: ZDKAYaKbj0OG6A_aqJkx9w - <?xml version="1.0" encoding="utf-16"?><Item><UID>ZDKAYaKbj0OG6A_aqJkx9w</UID><ID>0</ID><SubClassesData>GatherableMult/1;</SubClassesData><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: LcYQA1-a_E2Fx2hm8jJrTw - <?xml version="1.0" encoding="utf-16"?><Item><UID>LcYQA1-a_E2Fx2hm8jJrTw</UID><ID>0</ID><SubClassesData>GatherableMult/1;</SubClassesData><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: mhYjLJKfFUquTdXdIbOBoA - <?xml version="1.0" encoding="utf-16"?><Item><UID>mhYjLJKfFUquTdXdIbOBoA</UID><ID>0</ID><SubClassesData>GatherableMult/1;</SubClassesData><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: sbVufT8dxEKmDC2Jezr08A - <?xml version="1.0" encoding="utf-16"?><Item><UID>sbVufT8dxEKmDC2Jezr08A</UID><ID>0</ID><SubClassesData>GatherableMult/1;</SubClassesData><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: OVZA7mFnKEObSd-dpEgaBA - <?xml version="1.0" encoding="utf-16"?><Item><UID>OVZA7mFnKEObSd-dpEgaBA</UID><ID>0</ID><SubClassesData>GatherableMult/1;</SubClassesData><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: NY7VZAei1k6JWkXRzcR_EQ - <?xml version="1.0" encoding="utf-16"?><Item><UID>NY7VZAei1k6JWkXRzcR_EQ</UID><ID>5000021</ID><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: hYbl3dRqLEOOSBbs9AacLQ - <?xml version="1.0" encoding="utf-16"?><Item><UID>hYbl3dRqLEOOSBbs9AacLQ</UID><ID>0</ID><SubClassesData>GatherableMult/1;</SubClassesData><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: 6u6bV9aegkqC7FIWvvOT8A - <?xml version="1.0" encoding="utf-16"?><Item><UID>6u6bV9aegkqC7FIWvvOT8A</UID><ID>0</ID><SubClassesData>GatherableMult/1;</SubClassesData><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: n1niMjcvH0uCagKiOuTSpQ - <?xml version="1.0" encoding="utf-16"?><Item><UID>n1niMjcvH0uCagKiOuTSpQ</UID><ID>5000021</ID><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: 8ESBT5MiyEa8-q74gEsDYA - <?xml version="1.0" encoding="utf-16"?><Item><UID>8ESBT5MiyEa8-q74gEsDYA</UID><ID>0</ID><SubClassesData>GatherableMult/1;</SubClassesData><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: 8EXZMxYqIkaqMJsDZp_44w - <?xml version="1.0" encoding="utf-16"?><Item><UID>8EXZMxYqIkaqMJsDZp_44w</UID><ID>5000100</ID><SubClassesData>FueledContainerKindled/False;FueledContainerFuelTime/0;</SubClassesData><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: iCuz04bJ00mnR-ydQA5zig - <?xml version="1.0" encoding="utf-16"?><Item><UID>iCuz04bJ00mnR-ydQA5zig</UID><ID>0</ID><SubClassesData>GatherableMult/1;</SubClassesData><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: lSS5UyCmgEeIkOgJK7IdBw - <?xml version="1.0" encoding="utf-16"?><Item><UID>lSS5UyCmgEeIkOgJK7IdBw</UID><ID>5300120</ID><Hierarchy>5/Interaction/Loot</Hierarchy><Position>(1243.630, 26.601, 1681.311)</Position><Rotation>(0.000, 0.000, 0.000)</Rotation><Stuck>False</Stuck><SubClassesData>BagSilver/0;BagLantern/;</SubClassesData><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: vTv9z0qookeIGghtgtOfYA - <?xml version="1.0" encoding="utf-16"?><Item><UID>vTv9z0qookeIGghtgtOfYA</UID><ID>2120050</ID><Hierarchy>5/Interaction/Loot</Hierarchy><Position>(1690.629, 5.352, 1795.322)</Position><Rotation>(0.000, 0.000, 9.484)</Rotation><Stuck>False</Stuck><Durability>275</Durability><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: eiW7LKStH0C9agBFhmiTwA - <?xml version="1.0" encoding="utf-16"?><Item><UID>eiW7LKStH0C9agBFhmiTwA</UID><ID>5100060</ID><Hierarchy>5/Interaction/Loot</Hierarchy><Position>(1840.623, 5.089, 1790.785)</Position><Rotation>(339.665, 16.531, 96.817)</Rotation><Stuck>True</Stuck><Durability>100</Durability><ItemExtensions>Perishable;</ItemExtensions><IsNew>0</IsNew><LitStatus>Lit</LitStatus><PreviousContainerUID>-</PreviousContainerUID></Item>

id: 6_qIVLVSFEqP2PQG3YtX8w - <?xml version="1.0" encoding="utf-16"?><Item><UID>6_qIVLVSFEqP2PQG3YtX8w</UID><ID>4400010</ID><Hierarchy>5/Interaction/Loot</Hierarchy><Position>(1720.287, 10.680, 1857.564)</Position><Rotation>(0.000, 0.000, 0.000)</Rotation><Stuck>True</Stuck><Quantity>1</Quantity><ItemExtensions>MultipleUsage;1</ItemExtensions><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: Hp52hpknxE-EPyJFDe2ytA - <?xml version="1.0" encoding="utf-16"?><Item><UID>Hp52hpknxE-EPyJFDe2ytA</UID><ID>5100060</ID><Hierarchy>5/Interaction/Loot</Hierarchy><Position>(1786.264, 5.042, 1804.290)</Position><Rotation>(0.000, 223.092, 106.000)</Rotation><Stuck>True</Stuck><Durability>100</Durability><ItemExtensions>Perishable;</ItemExtensions><IsNew>0</IsNew><LitStatus>Lit</LitStatus><PreviousContainerUID>-</PreviousContainerUID></Item>

id: SMhknC0DCkyEA9hRo10LGQ - <?xml version="1.0" encoding="utf-16"?><Item><UID>SMhknC0DCkyEA9hRo10LGQ</UID><ID>5100060</ID><Hierarchy>5/Interaction/Loot</Hierarchy><Position>(1763.594, 5.363, 1821.614)</Position><Rotation>(0.000, 351.601, 105.999)</Rotation><Stuck>True</Stuck><Durability>100</Durability><ItemExtensions>Perishable;</ItemExtensions><IsNew>0</IsNew><LitStatus>Lit</LitStatus><PreviousContainerUID>-</PreviousContainerUID></Item>

id: -_NsJ1k4_k2czWhMSKIb0Q - <?xml version="1.0" encoding="utf-16"?><Item><UID>-_NsJ1k4_k2czWhMSKIb0Q</UID><ID>2130130</ID><Hierarchy>5/Interaction/Loot</Hierarchy><Position>(1696.065, 5.047, 1791.325)</Position><Rotation>(355.719, 141.946, 192.779)</Rotation><Stuck>True</Stuck><Durability>150</Durability><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: fQ9-JPq3lEGuYssIoHvLgw - <?xml version="1.0" encoding="utf-16"?><Item><UID>fQ9-JPq3lEGuYssIoHvLgw</UID><ID>5100060</ID><Hierarchy>5/Interaction/Loot</Hierarchy><Position>(1805.923, 5.165, 1792.741)</Position><Rotation>(0.000, 0.000, 106.000)</Rotation><Stuck>True</Stuck><Durability>100</Durability><ItemExtensions>Perishable;</ItemExtensions><IsNew>0</IsNew><LitStatus>Lit</LitStatus><PreviousContainerUID>-</PreviousContainerUID></Item>

id: baYLcD0m7Uu12e_RPnh5BA - <?xml version="1.0" encoding="utf-16"?><Item><UID>baYLcD0m7Uu12e_RPnh5BA</UID><ID>4100170</ID><Hierarchy>5/Interaction/Loot</Hierarchy><Position>(1710.663, 11.024, 1875.697)</Position><Rotation>(0.000, 0.000, 0.000)</Rotation><Stuck>True</Stuck><Durability>100</Durability><ItemExtensions>Perishable;</ItemExtensions><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: 8EXZMxYqIkaqMJsDZp_44w_DefaultAddOn - <?xml version="1.0" encoding="utf-16"?><Item><UID>8EXZMxYqIkaqMJsDZp_44w_DefaultAddOn</UID><ID>0</ID><IsParentManaged>1</IsParentManaged><Hierarchy>0Null</Hierarchy><Position>(1713.614, 11.024, 1867.245)</Position><Rotation>(0.000, 0.000, 0.000)</Rotation><Stuck>False</Stuck><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: s3Db1S3AZ0WjmVCly6FuDQ - <?xml version="1.0" encoding="utf-16"?><Item><UID>s3Db1S3AZ0WjmVCly6FuDQ</UID><ID>2000060</ID><Hierarchy>5/Interaction/Loot</Hierarchy><Position>(1647.266, 7.417, 1804.902)</Position><Rotation>(348.510, 147.823, 34.402)</Rotation><Stuck>True</Stuck><Durability>175</Durability><IsNew>0</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: JLQP25kHAkG2v--QgfAxLA - <?xml version="1.0" encoding="utf-16"?><Item><UID>JLQP25kHAkG2v--QgfAxLA</UID><ID>6000424</ID><Hierarchy>1Pouch_XVuyIaCAVkatv89kId9Uqw;0</Hierarchy><IsNew>1</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: dsCOshzHJ0uhOOVoM35L-A - <?xml version="1.0" encoding="utf-16"?><Item><UID>dsCOshzHJ0uhOOVoM35L-A</UID><ID>3000086</ID><Hierarchy>2XVuyIaCAVkatv89kId9Uqw</Hierarchy><Durability>90</Durability><IsNew>1</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: rmRDHBia-kiM8cMj79OuqQ - <?xml version="1.0" encoding="utf-16"?><Item><UID>rmRDHBia-kiM8cMj79OuqQ</UID><ID>3000150</ID><Hierarchy>2XVuyIaCAVkatv89kId9Uqw</Hierarchy><Durability>440</Durability><IsNew>1</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: DSeOGIol00aoz1357isSRA - <?xml version="1.0" encoding="utf-16"?><Item><UID>DSeOGIol00aoz1357isSRA</UID><ID>3000312</ID><Hierarchy>2XVuyIaCAVkatv89kId9Uqw</Hierarchy><Durability>375</Durability><IsNew>1</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: pNTqToXLs0-UP91WPH56ng - <?xml version="1.0" encoding="utf-16"?><Item><UID>pNTqToXLs0-UP91WPH56ng</UID><ID>2000265</ID><Hierarchy>2XVuyIaCAVkatv89kId9Uqw</Hierarchy><Durability>320</Durability><IsNew>1</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>

id: czYJcGBbF0quYdCb_87C4g - <?xml version="1.0" encoding="utf-16"?><Item><UID>czYJcGBbF0quYdCb_87C4g</UID><ID>2300080</ID><Hierarchy>2XVuyIaCAVkatv89kId9Uqw</Hierarchy><Durability>85</Durability><IsNew>1</IsNew><PreviousContainerUID>-</PreviousContainerUID></Item>
```
=> armor is loaded
=> hook `ItemManager.OnReceiveItemSync`, which takes the loaded items

boots: `DSeOGIol00aoz1357isSRA`
weapon: `czYJcGBbF0quYdCb_87C4g`
attire: `3C15kbEIG06czgUz3G28qw`

```cs
    [HarmonyPatch(typeof(ItemManager), nameof(ItemManager.OnReceiveItemSync))]
    static class ItemManagerReceiveItem
    {
        [HarmonyPrefix]
        internal static bool Prefix(ItemManager __instance, string _itemInfos, ItemManager.ItemSyncType _syncType, string _fromChar)
        {
            // INFO: return false => skip original
            if (string.IsNullOrEmpty(_itemInfos))
            {
                return false;
            }
            bool flag = _syncType == ItemManager.ItemSyncType.Normal;
            if (!PhotonNetwork.isNonMasterClientInRoom || (flag && (!NetworkLevelLoader.Instance.IsJoiningWorld || __instance.m_firstSyncCompleted)) || !flag)
            {
                string[] array = _itemInfos.Split(new char[]
                {
                '~'
                });
                string[] array2 = null;
                int num = 0;
                if (_itemInfos.StartsWith("DELETE"))
                {
                    array2 = array[0].Substring("DELETE".Length).Split(new char[]
                    {
                    ';'
                    });
                    num = 1;
                }
                int num2 = array.Length;
                for (int i = num; i < num2; i++)
                {
                    if (!string.IsNullOrEmpty(array[i]))
                    {
                        if (array[i] != "DONE")
                        {
                            string[] array3 = array[i].Split(new char[]
                            {
                            '|'
                            });
                            bool flag2 = true;
                            string itemUIDFromSyncInfo = Item.GetItemUIDFromSyncInfo(array3);
                            Randomizer.Log.LogMessage($"item {itemUIDFromSyncInfo}");
                            Randomizer.Log.LogMessage($"infos:{array3}, 0/uid:{array3[0]},1/id:{array3[1]},parentmanaged:{array3[2]}, hierarch?:{array3[4]}");
                            Item item = null;
                            if (__instance.m_worldItems.TryGetValue(itemUIDFromSyncInfo, out item))
                            {
                                Randomizer.Log.LogMessage("in world items");
                                item.SetRequestSyncInfo(false);
                            }
                            else if (Item.IsNotParentManaged(array3))
                            {
                                Randomizer.Log.LogMessage("not parent managed");
                                if (NetworkLevelLoader.Instance.IsOverallLoadingDone)
                                {
                                    Randomizer.Log.LogMessage($"creating item {itemUIDFromSyncInfo} from data");
                                    flag2 = __instance.CreateItemFromData(itemUIDFromSyncInfo, array3, _syncType, _fromChar);
                                    Item item2;
                                    if (flag2 && !PhotonNetwork.isNonMasterClientInRoom && _syncType == ItemManager.ItemSyncType.SplitRequest && __instance.m_worldItems.TryGetValue(itemUIDFromSyncInfo, out item2))
                                    {
                                        item2.ProcessInit();
                                        __instance.AddItemToSyncToClients(itemUIDFromSyncInfo);
                                        flag2 = false;
                                    }
                                }
                                else if (!__instance.m_pendingItemInstantiation.Contains(itemUIDFromSyncInfo))
                                {
                                    Randomizer.Log.LogMessage("not pending");
                                    if (_syncType == ItemManager.ItemSyncType.SceneLoad)
                                    {
                                        flag2 = (!Item.IsChildOfEquipmentSlot(array3) || Item.IsHandEquipment(array3));
                                        Randomizer.Log.LogMessage($"flag2: {flag2}");
                                    }
                                    if (flag2)
                                    {
                                        __instance.m_pendingItemInstantiation.Add(itemUIDFromSyncInfo);
                                    }
                                }
                            }
                            ItemSyncData itemSyncData = null;
                            if (__instance.m_pendingSync.TryGetValue(itemUIDFromSyncInfo, out itemSyncData))
                            {
                                Randomizer.Log.LogMessage($"refresh sync");
                                itemSyncData.RefreshSyncData(array3);
                            }
                            else if (flag2)
                            {
                                Randomizer.Log.LogMessage($"new itemsyncdata");
                                ItemSyncData itemSyncData2 = new ItemSyncData(array3);
                                itemSyncData2.CharacterOwnerUID = _fromChar;
                                itemSyncData2.SyncType = _syncType;
                                __instance.m_pendingSync.Add(itemUIDFromSyncInfo, itemSyncData2);
                                if (_syncType == ItemManager.ItemSyncType.SplitRequest && !PhotonNetwork.isNonMasterClientInRoom && item)
                                {
                                    item.DoUpdate();
                                    __instance.AddItemToSyncToClients(item.UID);
                                }
                            }
                        }
                        else if (_syncType == ItemManager.ItemSyncType.NetworkInitialization || _syncType == ItemManager.ItemSyncType.SceneLoad)
                        {
                            if (!NetworkLevelLoader.Instance.IsOverallLoadingDone)
                            {
                                UnityEngine.Debug.Log("m_firstSyncCompleted = true | " + _syncType.ToString());
                            }
                            __instance.m_firstSyncCompleted = true;
                        }
                    }
                }
                if (array2 != null)
                {
                    for (int j = 0; j < array2.Length; j++)
                    {
                        if (!string.IsNullOrEmpty(array2[j]))
                        {
                            __instance.DestroyItem(array2[j]);
                        }
                    }
                }
            }
            return false;
        }
    }
```
=> armor isnt loaded because of `flag2 = (!Item.IsChildOfEquipmentSlot(array3) || Item.IsHandEquipment(array3));` => set `flag2` to `true` and it works (seemingly only our items)
=> problem: some enemies dont have weapons + armor if they werent initially generated?
try: go into world, save+exit, rejoin, go to enemies who were far away => no startingequipment
=> also enemies have the spinning problem (need delayedanimfix) => call it in `Character.ProcessInit`?

### Problem: some enemies dont have randomized weapons + armor
reproduce: load level, randomize, save, exit, rejoin, go to previously not loaded enemy => no weapon + armor
cause ideas:
- ~~StartingEquipment.Init isnt run~~ => then they would have the editor weapon
- Is inited but never equipped to the char (because char is inactive?)?
- Equip isnt saved?

test: find bugged char, get uid, look at save if anything is there
uid: `-EFmE0w_EUSB2BghF524Kw`
in save:
```
<CharacterSaveData>

      <NewSave>false</NewSave>

      <VisualData>

        <Gender>Male</Gender>

        <HairStyleIndex>0</HairStyleIndex>

        <HairColorIndex>0</HairColorIndex>

        <SkinIndex>0</SkinIndex>

        <HeadVariationIndex>0</HeadVariationIndex>

      </VisualData>

      <UID>-EFmE0w_EUSB2BghF524Kw</UID>

      <Health>90</Health>

      <ManaPoint>0</ManaPoint>

      <Position>

        <x>1010.6</x>

        <y>-1000.00275</y>

        <z>998</z>

      </Position>

      <Forward>

        <x>0</x>

        <y>0</y>

        <z>1</z>

      </Forward>

      <Money>0</Money>

      <EncounterTime>-1</EncounterTime>

      <IsAiActive>false</IsAiActive>

      <StatusList />

    </CharacterSaveData>
```
=> char but no items
items not saved (because inactive/not equipped)? or not randomized?

test: load world, randomize, save+rejoin, get bugged char uid, see if it was randomized in log
```
[Message:Randomizer] StartingEquipment.InitItems: instance: NewBanditEquip_StandardBasic1_E_5WHJJhJfUUiOHP2nxPqmug (StartingEquipment) for NewBanditEquip_StandardBasic1_E_5WHJJhJfUUiOHP2nxPqmug (Character)

[Message:Randomizer] StartingPouchItems: System.Collections.Generic.List`1[ItemQuantity] (0 Elements)

[Message:Randomizer] startingEquipment: Equipment[]

[Message:Randomizer] Helmet: 3000039_LooterHelm (Armor)

[Message:Randomizer] Generated: Skeleton Helm with Hat, (3200034) for Looter Mask (3000039)

[Message:Randomizer] Deleting editor item: 3000039_LooterMask_5WHJJhJfUU (Armor)

[Message:Randomizer] Chest: 3000038_LooterArmor (Armor)

[Message:Randomizer] Generated: Black Fur Armor, (3000330) for Looter Armor (3000038)

[Message:Randomizer] Deleting editor item: 3000038_LooterArmor_5WHJJhJfUU (Armor)

[Message:Randomizer] Foot: 3000034_ScavengerBoots (Armor)

[Message:Randomizer] Generated: Padded Boots, (3000012) for Scavenger Boots (3000034)

[Message:Randomizer] Deleting editor item: 3000034_ScavengerBoots_5WHJJhJfUU (Armor)

[Message:Randomizer] RightHand: 2000060_Machete (MeleeWeapon)

[Message:Randomizer] Generated: Iron Sword, (2000010) for Machete (2000060)

[Message:Randomizer] Deleting editor item: 2000060_Machete_M2pHiX4QXE (MeleeWeapon)

[Message:Randomizer] StartingEquipment.InitItems: end
```
=> (other bandit but same problem) randomized, but not saved
```
<CharacterSaveData>

      <NewSave>false</NewSave>

      <VisualData>

        <Gender>Male</Gender>

        <HairStyleIndex>0</HairStyleIndex>

        <HairColorIndex>0</HairColorIndex>

        <SkinIndex>0</SkinIndex>

        <HeadVariationIndex>0</HeadVariationIndex>

      </VisualData>

      <UID>5WHJJhJfUUiOHP2nxPqmug</UID>

      <Health>90</Health>

      <ManaPoint>0</ManaPoint>

      <Position>

        <x>1021.506</x>

        <y>-1000.00269</y>

        <z>998.05</z>

      </Position>

      <Forward>

        <x>0</x>

        <y>0</y>

        <z>1</z>

      </Forward>

      <Money>0</Money>

      <EncounterTime>-1</EncounterTime>

      <IsAiActive>false</IsAiActive>

      <StatusList />

    </CharacterSaveData>
```
=> no other item reference

test: see if inactive bandit has randomized items equipped
=> yes (checked both uids previously used) => so the items arent added to the save
idea: look in the save func
`EnvironmentSave.PrepareSave`
```cs
foreach (string key in worldItems.Keys)
{
	if (worldItems.TryGetValue(key, out item) && !item.IsChildToPlayer && !item.NonSavable && !item.IsPendingDestroy && !item.IsInPermanentZone && !item.IsBeingTaken && (item.OwnerCharacter == null || !item.OwnerCharacter.IsItemCharacter) && (!item.OwnerCharacter || !item.OwnerCharacter.NonSavable) && !(item is Quest))
	{
		this.ItemList.Add(new BasicSaveData(item));
	}
}
```
=> no uid, therefore not in worldItems?
test: hook `ItemManager.ItemHasBeenAdded` or `Item.RegisterUID` to see who registers the startingequip => no clue, as its inited after item spawn
=> setting inactive bandit active also creates a uid (and there for in the save)

ideas to fix this:
- include randomized items in the save
- - force init after spawn, therefore generating a uid and being in worldItems
- - rewrite PrepareSave to also include inactive items
- let the enemies not be saved => do they get respawn by the game?
- set the enemies to be saved as "empty" characters (`NewSave`)

test: dont save characters
```cs
[HarmonyPatch(typeof(EnvironmentSave), nameof(EnvironmentSave.PrepareSave))]
    static class EnvironmentSavePrepareSavePatch
    {
        [HarmonyPrefix]
        internal static bool PrepareSave(EnvironmentSave __instance)
        {
            __instance.ItemList.Clear();
            DictionaryExt<string, Item> worldItems = ItemManager.Instance.WorldItems;
            Item item = null;
            foreach (string key in worldItems.Keys)
            {
                if (worldItems.TryGetValue(key, out item) && !item.IsChildToPlayer && !item.NonSavable && !item.IsPendingDestroy && !item.IsInPermanentZone && !item.IsBeingTaken && (item.OwnerCharacter == null || !item.OwnerCharacter.IsItemCharacter) && (!item.OwnerCharacter || !item.OwnerCharacter.NonSavable) && !(item is Quest))
                {
                    __instance.ItemList.Add(new BasicSaveData(item));
                }
            }
            __instance.CharList.Clear();
            for (int i = 0; i < CharacterManager.Instance.Characters.Count; i++)
            {
                if (CharacterManager.Instance.Characters.Values[i].OwnerPlayerSys == null && !CharacterManager.Instance.Characters.Values[i].NonSavable)
                {
                    if (!CharacterManager.Instance.Characters.Values[i].isActiveAndEnabled)
                        continue;

                    CharacterManager.Instance.Characters.Values[i].CheckElevatorPosition();
                    CharacterSaveData characterSaveData = new CharacterSaveData(CharacterManager.Instance.Characters.Values[i]);
                    characterSaveData.NewSave = false;
                    __instance.CharList.Add(characterSaveData);
                }
            }
            __instance.InteractionActivatorList.Clear();
            __instance.InteractionActivatorList.AddRange(SceneInteractionManager.Instance.GetInteractionActivatorsSaveData());
            __instance.DropTablesList.Clear();
            __instance.DropTablesList.AddRange(SceneInteractionManager.Instance.GetDropTablesSaveData());
            __instance.CampingEventSaveData = CampingEventManager.Instance.GetCurrentEventTableSaveData();
            __instance.DefeatScenarioSaveData = DefeatScenariosManager.Instance.GetCurrentEventTableSaveData();
            __instance.UsedSoulSpots = EnvironmentConditions.Instance.GetUsedSoulSpotUIDs();
            __instance.TOD = EnvironmentConditions.Instance.TimeOfDay;
            __instance.GameTime = EnvironmentConditions.GameTime;
            __instance.LastSpawnIDUsed = SpawnPointManager.Instance.LastSpawnPointUsed;
            __instance.PermanentItemZoneList.Clear();
            if (PermanentItemZone.ZoneCount > 0)
            {
                IList<PermanentItemZone> zones = PermanentItemZone.Zones;
                for (int j = 0; j < zones.Count; j++)
                {
                    if (zones[j].ItemsInZone.Count > 0)
                    {
                        __instance.PermanentItemZoneList.Add(new PermanentItemZoneSave(zones[j]));
                    }
                }
            }
            return false; // skip
        }
    }
```
=> enemy reserves seem to be replaced, but idk if there are fewer enemies => saving them as new would probably be safer
=> this also deletes enemies that were randomized but are non inactive
=> need better way to detect that they were never init'ed => look into AISquadManager?
```cs
public void AISquadManager.TryActivateSquad(AISquad _squad, bool _resetPositions = true) {
  if (_squad != null && this.m_squadsInReserve.Contains(_squad)) {
    if (this.CheckActiveSquadsCap()) {
      this.m_squadsInReserve.Remove(_squad);
      this.m_squadsInPlay.Add(_squad);
      _squad.SetSquadActive(true, _resetPositions);
      if (!NetworkLevelLoader.Instance.IsOverallLoadingDone) {
        this.SyncSquadInfo(_squad);
        return;
      }
    } else {
      this.m_lastSpawnCheckTime -= this.SpawnTime / 2 f;
    }
  }
}

public void AISquad.SetSquadActive(bool _active, bool _resetPositions = true) {
  base.gameObject.SetActive(_active);
  if (_resetPositions) {
    for (int i = 0; i < this.m_squadMemberList.Count; i++) {
      if (this.m_squadMemberList[i].CharAI && this.m_squadMemberList[i].CharAI.Character.Alive) {
        this.m_squadMemberList[i].PositionForSpawn();
      }
    }
  }
  Global.RPCManager.photonView.RPC("SendSquadInfo", PhotonTargets.Others, new object[] {
    this.ToNetworkData()
  });
}
```
=> this doesnt help me at all; deactivate also resets everything
=> `CharacterAI.Encounter?` => doesnt mean they werent init'ed, but we can probably delete those? still loss

try: activate all items created, so they will be saved
```cs
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
                        switch (equip.EquipSlot)
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

                        var slot = equipment.EquipmentSlots[(int)equip.EquipSlot];
                        if (slot == null)
                            continue;
                        var realEquipment = slot.EquippedItem;
                        if (realEquipment && !realEquipment.m_initialized)
                            realEquipment.Start();
                    }
                }
            }
            catch (Exception e)
            {
                Randomizer.Log.LogMessage($"Exception during DelayedRegisterUIDFix: {e}");
            }
        }
```
=> causes multiple items with the same uid (cause they're called at the same time?); but i think they will be generated nonetheless

do `newSave=ShouldSave()` with
```cs
bool ShouldSave()
{
return EncounterTime != -1f && EquipmentNotInitialized();
}
```
? (transpiler overwrite)

non humanoid items also arent saved (same problem?)
test: are the multiple items with same uid causes on the same mob?
=> no. but why is the uid the same? => probably cause they're called from diff threads at the same time

test: is encountertime set when first "loaded" or when first meeting the player 
=> encountertime is set when player is hurt

test: hook Item.Start and see if this is called for the instantiated items
=> it is called? then why arent the uids registered
```
[Message:Randomizer] Start item 2400400_CageArmorBoss(Clone) (MeleeWeapon) (0GDSw0WKfEeDbff50VYd8Q) to MantisShrimp_mgDxGpW1iEqreU5vNiEfoQ (Character)
```
=> the uid is already set??

test: hook and log more to see where the start fails
```cs
[HarmonyPatch(typeof(Item), nameof(Item.Start))]
    static class ItemManagerRequestInitPatch
    {
        [HarmonyPrefix]
        internal static void Prefix(Item __instance)
        {
            Randomizer.DebugTrace($"Start item {__instance} ({__instance.UID}) to {__instance.OwnerCharacter}");
            __instance.m_toLogString = __instance.m_name.Replace(" ", string.Empty);
            if (__instance.ScenePersistent || __instance.m_forceAlive || __instance.m_requestSyncInfo || PhotonNetwork.offlineMode || !PhotonNetwork.inRoom || (PhotonNetwork.inRoom && PhotonNetwork.isMasterClient))
            {
                if (!__instance.m_initialized)
                {
                    if (!NetworkLevelLoader.Instance.IsOverallLoadingDone)
                    {
                        Randomizer.DebugTrace($"request item init");
                        ItemManager.Instance.RequestItemInitialization(__instance);
                        return;
                    }
                    Randomizer.DebugTrace($"startinit");
                    __instance.StartInit();
                    return;
                }
            }
            else if (PhotonNetwork.inRoom && PhotonNetwork.isNonMasterClientInRoom)
            {
                if (string.IsNullOrEmpty(__instance.m_UID))
                {
                    Randomizer.DebugTrace($"destroy");
                    UnityEngine.Object.Destroy(__instance.gameObject);
                    return;
                }
                if (__instance.ScenePersistent || __instance.m_forceAlive)
                {
                    Randomizer.DebugTrace($"registeruid");
                    __instance.RegisterUID();
                }
            }
        }
    }
```
=> start isnt called for non active?

in `StartingEquipment.InitItems()`:
```cs
Item item = UnityEngine.Object.Instantiate<Item>(this.StartingPouchItems[i].Item);
item.ChangeParent(this.m_character.Inventory.Pouch.transform);
if (Application.isPlaying)
{
	ItemManager.Instance.RequestItemInitialization(item);
}
```
=> call RequestItemInit also in `InitEquipment`?

test: Item prefabs that have a uid
```cs
foreach (var item in ResourcesPrefabManager.ITEM_PREFABS.Values)
{
	if (!string.IsNullOrEmpty(item.UID))
	{
		Log(item);
	}
}
```
=>
```
[Message:UnityExplorer] 1000040_ChestOrnateA (TreasureChest)

[Message:UnityExplorer] 1000070_ChestTrunk (TreasureChest)

[Message:UnityExplorer] 1000120_ChestEliteA (TreasureChest)

[Message:UnityExplorer] 1001000_JunkPileA (TreasureChest)

[Message:UnityExplorer] 1000130_ChestTrunkCaldera (TreasureChest)

[Message:UnityExplorer] 1000140_ChestLionmanCaldera (TreasureChest)

[Message:UnityExplorer] 1300001_Gatherable_Jade (Gatherable)

[Message:UnityExplorer] 1900001_StaticCookingStation (CraftingStation)

[Message:UnityExplorer] 2400003_HoundTeeth (MeleeWeapon)

[Message:UnityExplorer] 2400100_WendigoClaw (MeleeWeapon)

[Message:UnityExplorer] 2400101_WendigoAccursedClaw (MeleeWeapon)

[Message:UnityExplorer] 2400130_ShellHorrorClaw (MeleeWeapon)

[Message:UnityExplorer] 2400131_ShellHorrorClawWeak (MeleeWeapon)

[Message:UnityExplorer] 2400132_ShellHorrorClawBurning (MeleeWeapon)

[Message:UnityExplorer] 2400142_GalvanicGolemBrokenRapier (MeleeWeapon)

[Message:UnityExplorer] 2400202_ForgeGolemRustLichMinionBeak (MeleeWeapon)

[Message:UnityExplorer] 2400300_PyplaTeethAndTail (MeleeWeapon)

[Message:UnityExplorer] 2400310_BoozuHorn (MeleeWeapon)

[Message:UnityExplorer] 2400311_BoozuProudBeastHorn (MeleeWeapon)

[Message:UnityExplorer] 2400370_TitanGolemHammer (MeleeWeapon)

[Message:UnityExplorer] 2400371_TitanGolemHalberd (MeleeWeapon)

[Message:UnityExplorer] 2400372_TitanGolemSword (MeleeWeapon)

[Message:UnityExplorer] 2400400_CageArmorBoss (MeleeWeapon)

[Message:UnityExplorer] 2400410_TorcrabBeakAndClaw (MeleeWeapon)

[Message:UnityExplorer] 2400411_TorcrabGiantBeakAndClaw (MeleeWeapon)

[Message:UnityExplorer] 5000204_EnchantingGuildTable (EnchantmentTable)

[Message:UnityExplorer] 5000205_EnchantingGuildPillar (SingleItemContainer)
```
=> we can delete the uids and then register all items

Problem: we dont delete the editor item if startingequipment isnt called
=> hook Awake => completely breaks visuals?? => do before `StartingEquip.Init`, which is also called on loaded enemies

### Problem: spinning enemies on load
loaded items are apparently added later than `StartingItem.Init`, so we cant 
test: `Equipment.DataSynced` => called too often, many times mid combat
test: `Item.UpdateParentChange` => also called often, but works if limited to armor (not really called after lvl load)
test: `CharacterEquipment.EquipItem` => called not enough? sometimes doesnt catch loaded items