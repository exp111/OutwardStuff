- `DialogueTreeController` is the `GraphOwner` of the `DialogueTreeExt`
- is `MonoBehaviour` => therefore probably in assets
- Contains `boundGraphSerialization` => use that json object to render?

Files contain `_serializedGraph`:
JSON that contains `nodes` and `connections`:
```json
{
        "_condition": {
            "checkMode": "AnyTrueSuffice",
            "conditions": [{
                "QuestEventRef": {
                    "m_eventUID": "XCz0EvYeAkulk2po_pxKWg"
                },
                "$type": "NodeCanvas.Tasks.Conditions.Condition_QuestEventOccured"
            }, {
                "QuestEventRef": {
                    "m_eventUID": "SQ29t58YcUWxaqsGjiJ-sw"
                },
                "$type": "NodeCanvas.Tasks.Conditions.Condition_QuestEventOccured"
            }],
            "$type": "NodeCanvas.Framework.ConditionList"
        },
        "_position": {
            "x": 4755.0,
            "y": 4875.0
        },
        "$type": "NodeCanvas.BehaviourTrees.ConditionNode",
        "$id": "4"
    }
```
```json
{
	"_sourceNode": {
		"$ref": "2"
	},
	"_targetNode": {
		"$ref": "3"
	},
	"$type": "NodeCanvas.BehaviourTrees.BTConnection"
}
```
Different types of trees:
- `DialogueTreeExt`: Contain dialogues, in `Resources/_npcs/_dialogues`
- `QuestTree`: Contain Quests, in `Resources/_npcs/_quests` and `_questsbackups`
- `BehaviourTree`: ?? Generic actions?, only seem to be referenced in the scenes and `Monobehaviour` folder, starting with `Tree`

Quests are referenced by eventUID, which are deserialized by `QuestEventDictionary.Load` from `Resources/_Items/_EditorData/QuestEvents.xml`:
```xml
<QuestEventSignature>
  <EventUID>OSTwu7_j90SObKiW9pkC6Q</EventUID>
  <EventName>TalkToDude</EventName>
  <Description>hdsfkjsdsfhs</Description>
  <Savable>true</Savable>
  <IsStackable>true</IsStackable>
  <IsTimedEvent>false</IsTimedEvent>
  <IsEphemeral>false</IsEphemeral>
  <DLCId>0</DLCId>
  <IsHideEvent>false</IsHideEvent>
</QuestEventSignature>
```