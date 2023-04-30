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
- Enemies sometimes spawn with their original weapon/armor (or no weapon equipped) after being loaded from a save

If you have any other issue, please open an issue on GitHub (linked above)