Idea:
- remove time limit from harmattan parallel quest (Rust and Vengeance, `SA_SideQuest`)
- also maybe from the main quest line?

## Details:
* quests saved in `Character` > `CharacterInventory` > `CharacterQuestKnowledge` > `Quest`
- Quest Progression is handled through the FSM Tree (`Quest.m_questProgress.m_questSequence`)
- Conditions are defined in `QuestSystem.NodeObjectives.QuestAction_...` (that's not the main namespace)
- Objectives for the conditions are in `QuestSystem.QuestObjectives`
- `QuestAction_WaitGameTime` has `WaitGameTime`, which checks `QuestEventManager.CheckEventExpire`, which checks `QuestEventData.HasExpired`
- `QuestEventSignature`s are saved in `QuestEventDictionary` and contain general info (are also used by `QuestEventData`, contain `IsTimedEvent` flag)
- TODO: diff between `WaitGameTime` and `Wait` objectives? => `Wait` is "Wait for x seconds/hours" while `WaitGameTime` is a negative thing?
- `TreeOwner_ActiveTriggerParallelQuest BehaviourTree` graph checks for `General_DoneQuest1` in first task
- QuestEventData saved in: `QuestEventDictionary` (contains all quests) > `QuestEventFamily` (the "quest") > `QuestEventSignature` (contains the log entry with name and description) > `QuestEventData` (contains data such as time)
- `Condition_CheckQuestEventExpiry` also exists
- some timers arent mentioned in quest trees (not polled constantly) but rather checked othertimes (ie when starting dialogue with Cyr)

=> hook into `QuestEventData` and override `HasExpired()?` or hook into `WaitGameTime.HasExpired` + `Condition_CheckQuestEventExpiry`? => both just call `QuestEventManager.CheckEventExpire`, so hook that. check the `QuestEventManager.m_questEvents[_eventUID]` for white/blacklist

### TimedEvents:
https://outward.fandom.com/wiki/Quests list
`SA_TimerEnds_Parallel` Rust and Vengeance Timer (100 days)
`CallToAdventure_Expired` Main quest timer (3 days)
`Crafting_*BlacksmithTimer` crafting timers => maybe optional skippable
`SideQuests_SmugglerTimer` Find lost merchant timer
`General_DoneQuest*` End of main quest timers => optional skippable
`MixedLegacies_Expired` => Mixed Legacies optional captain timer
`Vendavel_QuestTimer` => Vendavel quest, not referenced in my script but works
`AshGiants_CyrCountdown` => ash giants negotiation countdown (starts when spoken to cyr)

"Purify the Water" (`Neutral_WaterMachine_Quest`) is not timed, but rather dependant on the Vendavel quest


dump:
```cs
foreach (var ev in QuestEventDictionary.m_questEvents.Values)
{
	if (ev.IsTimedEvent)
		Log($"{ev.EventName} ({ev.EventUID}): {ev.Description}");
}
```
=>
```
WT_QuestTimerStart (_fJzHVekQ0qYzYIK9vZBCQ): QuestTimer_Start
AncPeace_Timeout (Ut_b4qIDZUmEcxTjirr7JA): 7 days timer
AshGiants_CyrCountdown (Rf6m7-vRqkeZhm32Up6zcw): Player found Cyr and was given a month to get arguments
MixedLegacies_Expired (0E_zfvHCk0iTjC26hXxnug): Check for 7 or 14 days
MixedLegacies_SpyLeaderDeath (Eki_dZ_ABE-jdqqeuuc0Mg): Player killed spy leader
WhispBones_GabAwayTime (hAlacUNhR0u3HHxX8F66Tw): Timer for Gabriella (2 days)
WhispBones_RolandDeath (vuuNZxv5f0-2FA0LvIfGRA): Roland is no more within the living
WhispBones_StartTimer (5NfRrLWmaUqlWrev4DCIYg): Big timer (40 days)
CA_Q1_D2Reseted (IpnZOK0R80qnf7Kz2kWcAQ): Check if dungeon reseted while noble acressive
CA_Q1_Timer_Start (6Kx88F4WvUKcHTf-pMf4CQ): Quest 1 Construction phase started
CA_Q2_Timer_Start (X8HNRwUVzEmJWyKBVsOcBg): Quest 2 Construction phase started
CA_Q3_EmissaryDefeated (QMJbRRZarE2-p5RCPuzAFQ): Killed Emissary
CA_Q3_Timer_Start (iZ5BbRMU_UyM9DBZQ8hiEQ): Timer Start
CA_Q4_AvatarDead (iQsYJddTjkWvDjthKbWEIg): Need Track his death
CA_Q4_Timer_Start (3YcM09jl00ylFqSd9dt7Qg): Quest Event
_Defeat_CaravanerRescueTimer (ivZiQK_N-kynGo5WByVkXA): Quest Event
_Defeat_SwampSavior_Triggered (yX2fvNy5Ak2qAYZOas7v-g): Quest Event
_Defeat_UndercityGoonsActive (7a1tA5Ecs0eovw8aX6P9rA): Quest Event
LiquidCooledGolemDeath (Aj3DmHz1ckmR76ISoWDWpg): Quest Event
Bosses_BrandHeroDefeat (29P6K0yqC0e0kSLEvjN9Gg): Loot accessible
Bosses_CalixaDefeat (D1tmLZmFREKtnBbunfH8Lw): Loot accessible
Bosses_EliteBeasGolemDefeat (KEZJM24S40KH5_pZoVjdtg): Loot accessible
Bosses_EliteBurnManDefeat (q06I_M_dW0C_LnH01AlcCg): Loot accessible
Bosses_EliteGiantsDefeat (in8wPr6TwUGR-8pVJ3weFA): Loot accessible
Bosses_EliteImmaculateDefeat (VpZXfLR-nE28EhqZCuIkWg): Loot accessible
Bosses_EliteLichesDefeat (1iKdNwqb3kO9rDnlmTzAyw): Loot accessible
Bosses_ElitePriestDefeat (ZUm3uQfEQU27VPTc4QupWg): Loot accessible
Bosses_EliteSharkDefeat (hVQfbH-2KEGdcLY6X2HVAw): Loot accessible
Bosses_EliteShrimpDefeat (Tr5ghMJXx0uRi8mfSem64w): Loot accessible
Bosses_EliteTuanoDefeat (uN6GT7burE6lKbfwuTYEuw): Loot accessible
Bosses_TrogQueenDefeat (SFxLOR5n6UWHrN96etxNWQ): Loot accessible
BossesDLC2_CrimsonDefeat (cYo22Tcr1keKmmXwSme14g): Loot accessible
BossesDLC2_GargoylesDefeat (DA291kIoUEC8-iL5InPWLQ): Loot Accessible
BossesDLC2_GrandmotherDefeat (aofDMt9K3USEr6XZfyEFNQ): Loot Accessible
BossesDLC2_TorcrabDefeat (NZb4gOMxZ02app3uNVNj6A): Loot accessible
DLC2Artifacts_GepBladeTimer (7YhAYR7NFEyEP9ZzcE4SDA): Randomize Position of the sword artifact
DLC2Artifacts_GrindKill (2r2HboU4eEexYbx5bSoPYg): Killed the Calygrey for 2H axe artifact
Caldera_ArenaTimerA (lWtrihXdvUK90tUzS7t9bg): Need Wait 24h
Caldera_ArenaTimerB (P2V_pqPU6ECFnorC2-YINQ): Need Wait 24h
Caldera_ArenaTimerC (I72vdNcIx0Oeb22_2o6T5Q): Need Wait 24h
Caldera_ArenaTimerD (807loDqdiEu9VU-HAIorng): Need Wait 24h
Caldera_ArenaTimerE (ZlFi-7qMWka5h32x_OX6vA): Need wait 24h
Caldera_BlackSmith_Timer (Y-fk7tCY3EqMfzDw2-3WqQ): Checks for when you can pick up Custom Gear
Caldera_Courier_Exotic_Shop_Tracker (xzCupALfTkuvyP9lPcGwhg): Track the Shop upgrad that buys from other cities
Caldera_FortressDungeonExit (2CGh_ps9n0GR6Ixv1GsDZg): Was in forteress so no reset.
Caldera_FortressTimer (J9Bp3-074UKoUVTZ1fiQ-w): Used as timer and check if position set
Caldera_Giant_Inn_Access (5kQe3jKdy0S4boyxL2qSmQ): Opens the curtains at the Giant Sauna to the Beds
Caldera_ResetChest (rcgfpuhGG0OkgwbylGlD6Q): WeeklyFood
Caldera_SampleTimer (PiV00ww0S0CZek5a5Kvf3g): The Timer for samples
Caldera_SmallDungeonReset (-rF_Ai5TFkS2eTbLxwU6bQ): Check if Dungeon small have reseted
Caldera_Wine_Dispenser_A (TublU0cUJ0eAFMsQbH63rQ): Tracks when you can get a Wine from the Distillery
Caldera_Wine_Dispenser_B (YM3CajlDL0iYMEEV19SC4w): Tracks when you can get a Wine from the Distillery
Caldera_Wine_Dispenser_C (3jRmPDAD9kKnfHfyxlESdA): Tracks when you can get a Wine from the Distillery
HeroPeace_CyreneTimer (k0ASFQgkVkChs7qUzG_W2A): Check if in time for Cyrene
HeroPeace_DawneDefeat (vaMMrEVQF06jsrEgNE-T3A): KilledHer
HeroPeace_DawneTask (Q0K3MQlrMk6I0hUp3hY-ug): Dawne asked money in exchange for the palace access
HeroPeace_TimerA (pmstAGccfEy1_LMDa1QgOw): Can be 7 or 14 depending on Yzan Friendship
HeroPeace_TimerB (Ms7EFdanpkSMYvTHsIlGuQ): Cyrene time late or not (5 or 10 days)
MouthFeed_MofatTimer (tphucYeJeUSUr6nvcWPUeg): Time needed for Mofat for his ingredients and potion
MouthFeed_QuestTimer (sOtZIuc_90moMYkseHl24w): Big timer- 25 , 35 or 50 days
MouthFeed_ResearchTimer (LWSgVDVV8E6o2bEsBBidrg): need 12 hours
SandCorsairs_Timer (lfDzVmxQiUWbdXxE58zANg): 25 days timer
TendFlame_Timer (Hx4xNbnWMEqWRrPlkXqZUw): Is 3 or 5 days depending on player actions with bomb
Doubts_Timer (KLbV0k6idU-JeYtY-oKj_g): 30 or 33 days, that is the question.
HallowPeace_TimerA (5ZVhOFKFN0mWU_3iJo0b0g): Have 30 days to make both faction peacefull
HallowPeace_TimerB (QTAby206b0-hzKvu3XlhPg): Have 7 days to go to council in Monsoon
Questions_Timer (oG0XANjA70aa6MMKqpVzOQ): Reached the corrupted tomb altar within 40 days
Truth_Timer (5khZdi32TkucAsBpFJCfTQ): The timer (40 days)
Inn_Berg_RoomAvailable (klxo5f5B1kSLie07umtopw): Player can rest at Berg s Inn for 12h
Inn_Caldera_RoomAvailable (TD3INfzB7UaUtrJRou00ig): Player can rest at Caldera s inn for 12h
Inn_Cierzo_RoomAvailable (yIodGB0Erkyny0mtzhVrfw): Player can rest at Cierzo s Inn for 12h
Inn_Harmatan_RoomPoorAvailable (vFyEJACDuUinN-Go6f2lnQ): Player can rest at Harmattan s Inn for 12h
Inn_Harmatan_RoomRichAvailable (MPDpYTTdKkKtHkRzQE12KQ): Player can rest at Harmattan s Inn for 12h
Inn_Levant_RoomAvailable (HH4bWup4TkORnQGwGKFwDA): Player can rest at Levant s Inn for 12h
Inn_Monsoon_RoomAvailable (EiTTfHmko0-hOi4CnzGMgQ): Player can rest at Monsoon s Inn for 12h
CallToAdventure_Expired (zoKR1_bqiUiAetJ3uxw-Ug): Check for 5 days
Crafting_BergBlacksmithTimer (L7Jlkq3OgUC9mO_U99I_DQ): Quest Event
Crafting_CierzoBlacksmithTimer (zFBSWcCLrUWrz8HX8aEk9Q): Quest Event
Crafting_HarmattanBlacksmithTimer (KhdoHX6qZUKTZo1PkHmuSA): CraftingTime
Crafting_LevantBlacksmithTimer (K8G1Y3-wRUKe6paT6WTWXw): Quest Event
Crafting_MonsoonBlacksmithTimer (MKE65alIjkOr9IZrVPPGsw): Quest Event
Fraticide_Reminder (p9-fznNDZkKu4UOBY8yMhg): For reward
Fraticide_Timer (kosJVi3DhU-qUk2s3_6C2Q): 40 days
General_DoneQuest0 (P2rqNERqN0O1RhkD1ff7_w): Has complete Faction Commitment quest
General_DoneQuest1 (HbTd6ustoU-VhQeidJcAEw): Has completed quest 1 from a questline
General_DoneQuest2 (Wl08NWMJokemPVfTEyT3UA): Has completed quest 2 from a questline
General_DoneQuest3 (Og71f8G5a0eVmLxZB0yOKg): Has completed quest 3 from a questline
General_DoneQuest4 (1nGk1TyMbUi3VmSdn32zCg): Timed, Has completed quest 4 from a questline
General_GuardJustWarned (3KMtKt0yLkyV8Z2URqrsUg): 4 hours delay
General_PlayerGameTimer (ht5HlA1m9Uif9DskmRGBVg): Generic timer got at start of the game
General_RoyalHunter (b9WP9IVA3UyVxj6GlVpPJA): Killed Royal manticore
Harmadung_D2_J_RustLich (qdB7OLm36Uu67pP7uu-mgg): Rust Lich killed, when destroy phylactery
Lich_GoldTimer (Bgs-ZpNzNUCFQz30pIjqAQ): Timer for gold lich, before give more stuff
Lich_JadeTimer (IKx3MlkeYUux7-nXbDzKYA): Timer for jade lich
Purifier_Timer (Y8ICyKjtt0KLxVhzltmngQ): The core timer
SideQuests_AlchemistTimer (rPiRNjNqvEiD2_tYN34ogA): Removed after 3 days
SideQuests_AssassinTimer (MCQfl9YVT02YwqT99A_Krw): Removed after 3 days
SideQuests_BossBandit (hYb46M9290iKocIzxRU0tA): Killed Him
SideQuests_BossGolemGarden (zfjLZslCBUeCTVjj9YPByA): Killed Him
SideQuests_CookTimer (8d2Q9dyi3E-I8RbjeUEWiQ): Removed after 3 days
SideQuests_FoodStoreMonsoonTimer (tiVatD-xzkGhlcKd0ZpFvA): Timer to save Lost Merchant in Hallowed Marsh
SideQuests_FoodStoreMonsoonTimerWait (ah8Pkjbg002go06D42xVyw): Timer until can do quest again
SideQuests_GreenLord (FzJDXRw_ckO2upDHEhCjug): Killed him
SideQuests_LetResearcherDied (y8_brnf7F0SclbpGLXWxEQ): He went himself to get the statue, didn't make it.
SideQuests_RedLichWendigoKilled (B1t0PCHKsE2zfnwBQ-DSUg): Quest Event
SideQuests_SmugglerTimer (HYkCAaUKbkelQjoGJu-v_g): Timer to save Lost Merchant in Abrassar
SideQuests_SmugglerTimerWait (fXICR4YvV0-v-fjujtU4tA): Timer until can do quest again
SideQuests_TsarGhost (DeZnnhe4G0eZ6dmLLmRkhg): Quest Event
_Vendavel_CookTimer (zhwGCJsVS0G7sPmm831D1w): Quest Event
Vendavel_BaliraDeath (iBXvhnEkykqE6iiex4MOvw): Been killed by player
Vendavel_CierzoDestroyed (lDHL_XMS7kKEs0uOqrLQjw): Got from failed quest
Vendavel_CookJobDone (-nA6ucSjrU-UmlQ_04FiWg): Can do twice every 12 hours
Vendavel_CrockDeath (_v46xbZBvkazYLDKHuGEDA): Been killed by player
Vendavel_CrockDrunk (oiYkaBuDyUWOzIyUbDpX8Q): Gave some gep potion to Crock need to wait till effect wear off.
Vendavel_FeedTheOldLady (e8QVzvNG402rjprg0l5U_Q): Gave her an omelette
Vendavel_GateKeeperRefused (HIC0i4lBG02eGNPmQ9Uw9g): For not being able to just change clothes in front of guard
Vendavel_GavePickaxe (2tMMvOMwHEWSRMemsMfGGw): Break after 24 hours
Vendavel_Nurse_BandageGiven (NPOHuIyyqEq304lFGVel_Q): Quest Event
Vendavel_Nurse_PotionGiven (J41GXrR08ke29mjl3mGMLQ): Quest Event
Vendavel_QuestTimer (bm3rB3abI0KFok2x5P0lrg): Timer before Cierzo get destroyed.
Vendavel_RissaWarnCierzo (-vFSY-MNoUuLH1XXBkcqvQ): Timer, she tell only once every 3 days
Vendavel_RospaDeath (fUvpH2pEGkiK_Vce0Qxutg): Been killed by player
Vendavel_RospaWaitingForEto (8RwMhRkHuE2bOpogmohRog): Expect player to deliver Eto
Vendavel_WendigoKilled (nXC9x2NZrEmNUqP2R_UkMQ): Wendigo in chersonese D6 as been killed
SA_MissionStart_Q3 (Rm_iNfgyeUS-5bUEJOQvow): (Timed 20 d) The player has spoken with the Arcane Dean in the Hidden Lab in Harmatthan.
SA_SabotageMercA (3NFsM2eM5EKiE9sTChN2Uw): If killed
SA_SabotageMercB (QpQ3uy5X70iVpUlrcvmgTg): If Killed
SA_Arcane_TaskHasBegun_Q4 (lB7Q5JETxUW7g7vJ3EcYrQ): The the task begins after the player has talked to Arcane Dean.
SA_TimerEnds_Q4 (8ixdRVp9Z0mChR3cow3mgg): The Quest timer ends.
Dynamic_LichTimer_update (h1vJqBSQqEmfymjJ0MADaw): If Player has finished Quest 2, give 60 day time limit instead of 100
SA_TimerEnds_Parallel (vlc8XUcycUqm4OEvgmjm1Q): The timer starts.
Shop_Task_ArmorTimer (se9URZKO0UyhmjwfH-S6Pg): Puts 3 day repeatable timer on task
Shop_Task_CampTimer (c9qz4tn2qUalYWzJ8szi_w): Puts 3 day repeatable timer on task
Shop_Task_FoodTimer (AT51E9xY5U2HIQcYAP4gSQ): Puts 3 day repeatable timer on task
Shop_Task_GeneralTimer (3s3nzp9sX0e06yjZGwOmPQ): Puts 3 day repeatable timer on the task
Shop_Task_WeaponTimer (o4nyUp5kbk6xvog9mglz6Q): Puts 3 day repeatable timer on task
_Expedition_Timer (oJmIo-MhpEqmY_RsK9U6mw): Quest Event
```

### QuestTree
```cs
using NodeCanvas.StateMachines;
using NodeCanvas.Framework;
using static UnityExplorer.ExplorerCore;

public class QuestTreeHelper
{
	public static void PrintTree(QuestTree tree)
	{
		if (tree.primeNode != null)
		{
			Log("PrimeNode:");
			PrintNodeRec(tree.primeNode);
		}

		//TODO: find loose nodes
		foreach (var node in tree.allNodes)
		{
			if (node is ConcurrentState)
			{
				Log("Concurrent:");
				PrintNodeRec(node);
			}
			else if (node is AnyState)
			{
				Log("AnyState:");
				PrintNodeRec(node);
			}
		}
	}
	
	public static void PrintNodeRec(Node node, int depth = 0)
	{
		var d = new String('-', depth);
		Log($"{d}Node: {node.name}");
		if (node is ActionState)
		{
			Log($"{d}{((ActionState)node).task}");
		}
		else if (node is ConcurrentState)
		{
			Log($"Conditions: {((ConcurrentState)node).conditionList}");
			Log($"Actions: {((ConcurrentState)node).actionList}");
		}
		Log($"{d}Out:");
		foreach (FSMConnection con in node.outConnections)
		{
			Log($"{d}{con.task}");
			PrintNodeRec(con.targetNode, depth + 1);
		}
	}
}
```

```cs
var tree = (QuestTree)Paste();
Log(tree);
QuestTreeHelper.PrintTree(tree);
```


=> for rust and vengeance
```
7011406_Soroborian_QuestParallel (NodeCanvas.StateMachines.QuestTree)
Node: ▪<b>Add Log Entry</b>
"<i> check the busted Golem</i> "

Out:
If Has quest event <color=#00D5FF>SA_Golem_Early_Activated</color> occured 1 times
Node: <b>(In Parallel)</b>
▪<b>Add Log Entry</b>
"<i> NoLoc - You have touched a strange
Rune ...</i> "

► Check <color=#D6A260>SA_TimerEnds_Parallel</color> for expiry: 1 Month(s), 4 Week(s), 1 Day(s), 0 Hour(s)  - Remaining: 1 Month(s), 3 Week(s), 6 Day(s), 14 Hour(s) 
▪Send Quest Event : SA_TimerEnds_Parallel
▪<b>Add Log Entry</b>
"<i> LOC IN PROGRESS - There should
be a way ...</i> "

▪<b>Update Log Entry</b>
"<i> check the busted Golem</i> "

Out:
If Has quest event <color=#00D5FF>SA_LookFor4Keys_ParallelQuest</color> occured 1 times
Node: <b>(In Parallel)</b>
▪<b>Update Log Entry</b>
"<i> NoLoc - You have touched a strange
Rune ...</i> "

▪<b>Add Log Entry</b>
"<i> NoLoc - Find 4 Gemstone Keys to
activate...</i> "

Out:
If Has quest event <color=#00D5FF>SA_MainGateFourKeysOpened_ParallelQuest</color> occured 1 times
Node: <b>(In Parallel)</b>
▪<b>Update Log Entry</b>
"<i> NoLoc - Find 4 Gemstone Keys to
activate...</i> "

▪<b>Add Log Entry</b>
"<i> NoLoc - Find the “Forge Master”
and defe...</i> "

Out:
If Has quest event <color=#00D5FF>SA_BossLichBeaten_ParallelQuest</color> occured 1 times
Node: <b>(In Parallel)</b>
▪<b>Update Log Entry</b>
"<i> NoLoc - Find the “Forge Master”
and defe...</i> "

▪<b>Add Log Entry</b>
"<i> NoLoc -Destroy the “Forge Master’s”
Phyl...</i> "

▪<b>Update Log Entry</b>
"<i> NoLoc - You have delayed the Forge
Maste...</i> "

Out:
If Has quest event <color=#00D5FF>Harmadung_D2_J_RustLich</color> occured 1 times
Node: <b>(In Parallel)</b>
▪<b>Update Log Entry</b>
"<i> NoLoc -Destroy the “Forge Master’s”
Phyl...</i> "

▪<b>Add Log Entry</b>
"<i> NoLoc -Deal with the dying Forge
Master.</i> "

Out:
If Has quest event <color=#00D5FF>SA_LichConversationEnds_ParallelQuest</color> occured 1 times
Node: <b>(In Parallel)</b>
▪<b>Update Log Entry</b>
"<i> NoLoc -Deal with the dying Forge
Master.</i> "

▪<b>Add Log Entry</b>
"<i> NoLoc-Speak to Headmaster Raul
Salaberry...</i> "

Out:
If <b>(ANY True)</b>
▪If Has quest event <color=#00D5FF>SA_HeadMaster_RewardGiven_QuestParallel</color> occured 1 times
▪If Has quest event <color=#00D5FF>SA_HeadMaster_SmallRewardGiven_QuestParallel</color> occured 1 times
Node: <b>(In Parallel)</b>
▪<b>Update Log Entry</b>
"<i> NoLoc-Speak to Headmaster Raul
Salaberry...</i> "

▪Complete Quest : Success
```
(incomplete dump)