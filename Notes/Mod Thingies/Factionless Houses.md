Controlled through BehaviourTrees
Search for `PlayerHouse_<City>_HouseAvailable` in nodeviewer

Berg:
`Tree Behaviour UNPC BehaviourTree_18`: 2 Binary Selectors, 3 Action Nodes
```
if (Faction_BlueChamber && HouseAvailable) // Check if we are berg faction
{
	Activate HouseSeller
}
else
{
	if (DoneQuest4 && HouseAvailable) // Check if we are any other faction
	{
		Activate HouseSeller
	}
	else // House sold
	{
		Deactivate HouseSeller
	}
}
```
Monsoon: 
`Tree Behavior UNPC BehaviourTree_10`: 2 Binary Selectors, 3 Action Nodes
Same shit as with berg

=> just override Faction and DoneQuest4 Binary Selectors with true statement

Harmattan:
`Tree_Behavior_Repeat BehaviourTree_19`: 2 Binary, 3 Actions
```
if (FactionSorobore && DoneQuest4)
{
	if (HouseAvailable)
	{
		Activate HouseSeller
		Deactivate WarpPlayerHouse
	}
	else // house sold?
	{
		Deactivate HouseSeller
		Activate WarpPlayerHouse
	}
}
else // house cant be bought yet
{
	Deactivate HouseSeller
	Deactivate WarpPlayerHouse
}
```

Levant:
You get the house when you clear the quest perfectly, else buy it for 500 silver
`Tree Behavior_HMDisabledLevant BehaviourTree`: 3 Binary, 5 actions
```
if (Faction_HK && IsQuestCompleted? && HouseAvailable)
{
	Activate HouseSeller
}
else
{
	if (MouthFeed_QuestTimer && HouseAvailable) // quest not done perfectly
	{
		Activate HouseSeller
	}
	else
	{
		if (QuestDone4 && HouseAvailable) // other faction
		{
			Activate HouseSeller
		}
		else
		{
			Deactivate HouseSeller	
		}
	}
}
```

Probably best to do it manually for each?

Code to hook BehaviourTrees at start: (called on city load)
```c#
[HarmonyPatch(typeof(BehaviourTree), nameof(BehaviourTree.OnGraphStarted))]
public static class BehaviourTree_OnGraphStarted_Patch
{
	static Dictionary<string, bool> HouseTrees = new Dictionary<string, bool>()
	{
		{ "Tree Behaviour UNPC BehaviourTree", true }
	};

	[HarmonyPostfix]
	public static void Postfix(BehaviourTree __instance)
	{
		if (HouseTrees.ContainsKey(__instance.name))
		{
			Tutorial.DebugLog(__instance.ToString());
		}
	}
}
```
=> tree file is called `Tree Behaviour UNPC BehaviourTree_18`, but tree is called `Tree Behaviour UNPC BehaviourTree` => no easy way to distinguish
