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
- `Condition_CheckQuestEventExpiry` also exists (which is checked for the )

=> hook into `WaitGameTime` and override `HasExpired()?`

### TimedEvents:
https://outward.fandom.com/wiki/Quests list
`SA_TimerEnds_Parallel` Rust and Vengeance Timer (100 days)
`CallToAdventure_Expired` Main quest timer (3 days)
`Crafting_*BlacksmithTimer` crafting timers => maybe optional skippable
`SideQuests_SmugglerTimer` Find lost merchant timer


dump:
```cs
foreach (var ev in QuestEventDictionary.m_questEvents.Values)
{
	if (ev.IsTimedEvent)
		Log($"{ev.EventName}");
}
```
=>
```
WT_QuestTimerStart
AncPeace_Timeout
AshGiants_CyrCountdown
MixedLegacies_Expired
MixedLegacies_SpyLeaderDeath
WhispBones_GabAwayTime
WhispBones_RolandDeath
WhispBones_StartTimer
CA_Q1_D2Reseted
CA_Q1_Timer_Start
CA_Q2_Timer_Start
CA_Q3_EmissaryDefeated
CA_Q3_Timer_Start
CA_Q4_AvatarDead
CA_Q4_Timer_Start
_Defeat_CaravanerRescueTimer
_Defeat_SwampSavior_Triggered
_Defeat_UndercityGoonsActive
LiquidCooledGolemDeath
Bosses_BrandHeroDefeat
Bosses_CalixaDefeat
Bosses_EliteBeasGolemDefeat
Bosses_EliteBurnManDefeat
Bosses_EliteGiantsDefeat
Bosses_EliteImmaculateDefeat
Bosses_EliteLichesDefeat
Bosses_ElitePriestDefeat
Bosses_EliteSharkDefeat
Bosses_EliteShrimpDefeat
Bosses_EliteTuanoDefeat
Bosses_TrogQueenDefeat
BossesDLC2_CrimsonDefeat
BossesDLC2_GargoylesDefeat
BossesDLC2_GrandmotherDefeat
BossesDLC2_TorcrabDefeat
DLC2Artifacts_GepBladeTimer
DLC2Artifacts_GrindKill
Caldera_ArenaTimerA
Caldera_ArenaTimerB
Caldera_ArenaTimerC
Caldera_ArenaTimerD
Caldera_ArenaTimerE
Caldera_BlackSmith_Timer
Caldera_Courier_Exotic_Shop_Tracker
Caldera_FortressDungeonExit
Caldera_FortressTimer
Caldera_Giant_Inn_Access
Caldera_ResetChest
Caldera_SampleTimer
Caldera_SmallDungeonReset
Caldera_Wine_Dispenser_A
Caldera_Wine_Dispenser_B
Caldera_Wine_Dispenser_C
HeroPeace_CyreneTimer
HeroPeace_DawneDefeat
HeroPeace_DawneTask
HeroPeace_TimerA
HeroPeace_TimerB
MouthFeed_MofatTimer
MouthFeed_QuestTimer
MouthFeed_ResearchTimer
SandCorsairs_Timer
TendFlame_Timer
Doubts_Timer
HallowPeace_TimerA
HallowPeace_TimerB
Questions_Timer
Truth_Timer
Inn_Berg_RoomAvailable
Inn_Caldera_RoomAvailable
Inn_Cierzo_RoomAvailable
Inn_Harmatan_RoomPoorAvailable
Inn_Harmatan_RoomRichAvailable
Inn_Levant_RoomAvailable
Inn_Monsoon_RoomAvailable
CallToAdventure_Expired
Crafting_BergBlacksmithTimer
Crafting_CierzoBlacksmithTimer
Crafting_HarmattanBlacksmithTimer
Crafting_LevantBlacksmithTimer
Crafting_MonsoonBlacksmithTimer
Fraticide_Reminder
Fraticide_Timer
General_DoneQuest0
General_DoneQuest1
General_DoneQuest2
General_DoneQuest3
General_DoneQuest4
General_GuardJustWarned
General_PlayerGameTimer
General_RoyalHunter
Harmadung_D2_J_RustLich
Lich_GoldTimer
Lich_JadeTimer
Purifier_Timer
SideQuests_AlchemistTimer
SideQuests_AssassinTimer
SideQuests_BossBandit
SideQuests_BossGolemGarden
SideQuests_CookTimer
SideQuests_FoodStoreMonsoonTimer
SideQuests_FoodStoreMonsoonTimerWait
SideQuests_GreenLord
SideQuests_LetResearcherDied
SideQuests_RedLichWendigoKilled
SideQuests_SmugglerTimer
SideQuests_SmugglerTimerWait
SideQuests_TsarGhost
_Vendavel_CookTimer
Vendavel_BaliraDeath
Vendavel_CierzoDestroyed
Vendavel_CookJobDone
Vendavel_CrockDeath
Vendavel_CrockDrunk
Vendavel_FeedTheOldLady
Vendavel_GateKeeperRefused
Vendavel_GavePickaxe
Vendavel_Nurse_BandageGiven
Vendavel_Nurse_PotionGiven
Vendavel_QuestTimer
Vendavel_RissaWarnCierzo
Vendavel_RospaDeath
Vendavel_RospaWaitingForEto
Vendavel_WendigoKilled
SA_MissionStart_Q3
SA_SabotageMercA
SA_SabotageMercB
SA_Arcane_TaskHasBegun_Q4
SA_TimerEnds_Q4
Dynamic_LichTimer_update
SA_TimerEnds_Parallel
Shop_Task_ArmorTimer
Shop_Task_CampTimer
Shop_Task_FoodTimer
Shop_Task_GeneralTimer
Shop_Task_WeaponTimer
_Expedition_Timer
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