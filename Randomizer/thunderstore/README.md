Randomizes item drops and such.
A list of things that are randomized:
- Treasure Chests and other containers like junk piles
- Enemy drops
- Enemy armor and weapons
- Merchant inventories
- Gatherables like berry bushes or mining spots

The randomization of the drop tables is deterministic. That means that every enemy type should theoretically get their own new drop table which should be shared across that type. Same applies for enemy weapons. This can be disabled in the config (under "true random"), if you like chaos.

By default a random seed is generated that affects the tables. You can set it yourself if you want to play together or something.


You can enable/disable most stuff in the config. Some changes won't be made directly but rather on a area reload/merchant refresh/respawn.

Known issues:
- Enemies sometimes still have their original weapon in their inventory (even though I overwrite that)
- Enemies sometimes spawn with their original weapon/armor after being loaded from a save (feel like I can't do much. Outward's save system is weird)

If you have any other issue, please open an issue on GitHub (linked above)

Changelog: 
1.1.2:
- Limited the quantity of randomized non stackable items, so they don't fill up inventories that much anymore

1.1.1:
- Fixed monsters getting non monster weapons and thereby causing freezes during loading
- Modded items are now generated again
- Filtered some additional dev items
- Mod now logs seed for debug purposes

1.1: (Sinai did the heavy lifting here, so thanks for that <3)
- True Random now works even without changing areas
- Items are now filtered so you (hopefully) won't get broken items anymore
- The mod should run and load faster now