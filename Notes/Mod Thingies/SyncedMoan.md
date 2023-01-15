Old woman in berg moans when interacted, but thats not
ideas:
- look at interaction func => see how moan is called => then send rpc

uid `ylgRdqNWKkWGHOdGllIABA`
`NPCInteraction` in NPC > `DialogueTemplate` > `NPC` => contains `DialogueTreeExt` in `ExtTree` or `DialogueTree.graph` => 
- `Graph.allNodes[1]` => `PlayAudio LOC_EXCL_Mumble_QuietWoman02`
- `Graph.allNodes[4]` => `PlayAudio LOC_EXCL_Mumble_QuietWoman01`
- `Graph.allNodes[5]` => `PlayAudio LOC_EXCL_Mumble_QuietWoman03`
Action => `NodeCanvas.Tasks.Actions.PlaySound` => doesn't seem to be played by anthing else => to be safe still only send those 3 events