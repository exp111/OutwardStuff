## Musket/2 Handed Guns
### Design Idea:
- 2 handed gun (as opposed to the pistols being in one hand)
- more damage than a pistol
- pistol doesnt feel like a good "main" weapon (and more like a one handed addon)
- more ammo than a pistol? ~4? not really a flintlock but we aint judging
- also long reload
- can be manually aimed (like bows)

**Ideas**:
- uses bow skills? or just add new one, but like a charged/focused shot would be cog güzel
- pistol reload skill, but shot through left click? => maybe new skill, to not need to override fire mechanics

### code
- Bows and Pistols are `ProjectileWeapons`
- differentiated by `WeaponType`=`Pistol_OH`? (https://sinai-dev.github.io/OSLDocs/#/API/Enums/WeaponType) => maybe need to add own?
- TODO: find out where the right click aim is handled => `Weapon.SpecialIsZoom`?
- more than 1 ammo is already coded (throug `ItemExtensions`: `SL_WeaponLoadoutItem.MaxProjectileLoaded`)
- 


https://outward.thunderstore.io/package/stormcancer/Crossbow_Master/??