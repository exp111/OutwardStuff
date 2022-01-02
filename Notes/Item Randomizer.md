## Item Spawn Randomizer
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
- referenced by `TreasureChest.DropPrefabsGen` and `Gatherable/SelfFilledItemContainer.DropPrefabs` => change those? TODO: check where the dropables are generated (or if they are only generated on new saves and then loaded from saves)
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
- `WaitForItemBundleLoaded`: waits for resourcemanager, then calls init. actual set of the prefab isn't done here (probably done through resourcemanager or some unity shittery). TODO: debug to look if prefab is set at this point or even where it's set. `Initialize` is probably a good hooking point to replace the prefabs
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
