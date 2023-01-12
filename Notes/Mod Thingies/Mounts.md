base: https://github.com/Grim-/Outward.Mount

### Technical
Mounts as items (whistles) or skills (various pro cons)
WoW Style summoning (use to summon and ride, when attacked force dismount, use again to unsummon)
No mount inventory => maybe carry bonus (as the animal carries you) => ignore "overloaded" status (under specific weight, bonus depends on mount?)
Mount summon needs like 2-3 seconds to prevent abuse

### Content
Common mounts progressionwise from special skill trainers
Rare mounts dropped from enemies (rare drop) or bosses (less rare) => More rarer = more speed/health/special abilities (attack/jump?)
Traders sell mounts (one in each city?)
Mount is the "regional animal" (chersonesse = pearlbird)
Quests:
- Czierzo: Hurt pearlbird for doing favour (starting mount, worse than bought ones)
- Egg (sold or through quest) that needs to be bred
- Evolve mounts by crafting/npc

## Dismounting with quickslot skills
Problem: Mod disables `CharacterControl`, which calls `LocalCharacterControl.UpdateQuickSlots` to call the skill
Ideas:
- hook control to only let the unsummon skill be called
- Disable control and run the functions we wanna run => incompatibility?

Don't disable, set `InputBlocked`? => cant move, trying to cast skill results in `You cannot do this now` (Loc'ed `Notification_Action_Invalid`)=> caused by `Skill.HasAllRequirements`. override this specifically for our skill?