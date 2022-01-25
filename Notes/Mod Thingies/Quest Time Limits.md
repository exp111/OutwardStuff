Idea:
- remove time limit from harmattan parallel quest 
- also maybe from the main quest line?

## Details:
- Quest Progression is handled through the FSM Tree
- Conditions are defined in `QuestSystem.NodeObjectives.QuestAction_...` (that's not the main namespace)
- Objectives for the conditions are in `QuestSystem.QuestObjectives`
- `QuestAction_WaitGameTime` has `WaitGameTime`, which checks `QuestEventManager.CheckEventExpire`, which checks `QuestEventData.HasExpired`
- `QuestEventSignature`s are saved in `QuestEventDictionary` and contain general info (are also used by `QuestEventData`, contain `IsTimedEvent` flag)
- TODO: diff between `WaitGameTime` and `Wait` objectives?
- `TreeOwner_ActiveTriggerParallelQuest BehaviourTree` graph checks for `General_DoneQuest1` in first task

=> hook into `?`? and check for 