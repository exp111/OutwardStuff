## Item Spawn Randomizer
## TODO
- fix enemies sometimes being saves/spawned with the default weapon
- transmorphic dies in chersonne loading screen?
- randomizes ultimate backpack items (held by the backpack) => probably because of no localplayer check in `StartingEquipmentInitPatch`
- true random should maybe randomize everytime you pick something up. that would require hooks at the pick up points. it's cbt

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
