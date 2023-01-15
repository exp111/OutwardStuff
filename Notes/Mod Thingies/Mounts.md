base: https://github.com/Grim-/Outward.Mount

### TODO:
- add new textures for items + skills
- change npcs (clothing, names, position)
- add quests
- add mounts
- remove unused old mounts
- change item/skill ids
- fix item still doing anim when they dont have knowledge

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

Don't disable, set `InputBlocked`? => cant move, trying to cast skill results in `You cannot do this now` (Loc'ed `Notification_Action_Invalid`)=> caused by `Skill.HasAllRequirements`. override this specifically for our skill? => would also need to hook `ItemDisplay.TryUse`

## Skill is getting called twice
Problem: Toggle skill was called twice. This is because i called `character.SpellCastAnim`
Temp solution: use an item to summon and skill to desummon

## Skill Effect not getting called
Problem: skill not called while mounted, even when calling `OnUse` manually (which works for other skills)
Usual path:
```cs
at Mounts.Custom_SL_Effect.DespawnMount.ActivateLocally (Character _affectedCharacter, System.Object[] _infos) [0x00000] in <de3cb7ba264b44df8167f31aa9ceaa9d>:0 
  at Effect.DMD<Effect::TryActivateLocally> (Effect , Character , System.Object[] ) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Effect.OnReceiveActivation (Character _affectedCharacter, System.String[] _networkInfos) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at EffectSynchronizer.OnReceiveActivatedEffects (EffectActivationStack _activationStack, Character _targetCharacter, System.String[] _infos) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Item.OnReceiveActivatedEffects (EffectActivationStack _activationStack, Character _targetCharacter, System.String[] _infos) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at EffectSynchronizer.OnReceiveActivatedEffects (Character _targetCharacter, System.String _concatActivatedEffectsInfos) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Item.OnReceiveActivatedEffects (Character _targetCharacter, System.String _concatActivatedEffectsInfos) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Skill.OnReceiveActivatedEffects (Character _targetCharacter, System.String _concatActivatedEffectsInfos) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at RPCManager.SendSkillActivatedEffectsRPCTrivial (System.String _playerUID, System.String _skillUID, System.String _activatedSkillInfos) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at System.Reflection.MonoMethod.InternalInvoke (System.Object obj, System.Object[] parameters, System.Exception& exc) [0x00000] in <df7127ba07dc446d9f5831a0ec7b1d63>:0 
  at System.Reflection.MonoMethod.Invoke (System.Object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, System.Object[] parameters, System.Globalization.CultureInfo culture) [0x00000] in <df7127ba07dc446d9f5831a0ec7b1d63>:0 
  at System.Reflection.MethodBase.Invoke (System.Object obj, System.Object[] parameters) [0x00000] in <df7127ba07dc446d9f5831a0ec7b1d63>:0 
  at NetworkingPeer.ExecuteRpc (ExitGames.Client.Photon.Hashtable rpcData, System.Int32 senderID) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at NetworkingPeer.RPC (PhotonView view, System.String methodName, PhotonTargets target, PhotonPlayer player, System.Boolean encrypt, System.Object[] parameters) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at PhotonNetwork.RPC (PhotonView view, System.String methodName, PhotonTargets target, System.Boolean encrypt, System.Object[] parameters) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at PhotonView.RPC (System.String methodName, PhotonTargets target, System.Object[] parameters) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at RPCManager.SendSkillActivatedEffects (System.String _characterUID, System.String _skillUID, System.String _activatedSkillInfos) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Item.SendSyncEffects (Character _affectedCharacter, System.String _infos) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at EffectSynchronizer.SynchronizeEffects (Character _affectedCharacter, System.Collections.Generic.IList`1[T] _effects, UnityEngine.Vector3 _pos, UnityEngine.Vector3 _dir) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at EffectSynchronizer.SynchronizeEffects (EffectSynchronizer+EffectCategories _category, Character _affectedCharacter, UnityEngine.Vector3 _pos, UnityEngine.Vector3 _dir) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Skill.SynchronizeEffects (EffectSynchronizer+EffectCategories _category, Character _targetCharacter, UnityEngine.Vector3 _pos, UnityEngine.Vector3 _dir) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at EffectSynchronizer.SynchronizeEffects (EffectSynchronizer+EffectCategories _category, Character _affectedCharacter) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at EffectSynchronizer.SynchronizeEffects (EffectSynchronizer+EffectCategories _category) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Skill.SkillStarted () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Character.SpellCastAnim (Character+SpellCastType _type, Character+SpellCastModifier _modifier, System.Int32 _sheatheRequired) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Character.PerformSpellCast (Character+SpellCastModifier _modifier) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Character.SpellCastProcess (System.Int32 _spellCastTypeID, System.Int32 _modifier, System.Int32 _sheatheRequired, System.Single _mobileCastMoveMult) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Character.SendPerformSpellCastTrivial (System.Int32 _spellCastTypeID, System.String _eventReceivePath, System.Int32 _modifier, System.Int32 _sheatheRequired, System.Single _mobileCastMoveMult) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at System.Reflection.MonoMethod.InternalInvoke (System.Object obj, System.Object[] parameters, System.Exception& exc) [0x00000] in <df7127ba07dc446d9f5831a0ec7b1d63>:0 
  at System.Reflection.MonoMethod.Invoke (System.Object obj, System.Reflection.BindingFlags invokeAttr, System.Reflection.Binder binder, System.Object[] parameters, System.Globalization.CultureInfo culture) [0x00000] in <df7127ba07dc446d9f5831a0ec7b1d63>:0 
  at System.Reflection.MethodBase.Invoke (System.Object obj, System.Object[] parameters) [0x00000] in <df7127ba07dc446d9f5831a0ec7b1d63>:0 
  at NetworkingPeer.ExecuteRpc (ExitGames.Client.Photon.Hashtable rpcData, System.Int32 senderID) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at NetworkingPeer.RPC (PhotonView view, System.String methodName, PhotonTargets target, PhotonPlayer player, System.Boolean encrypt, System.Object[] parameters) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at PhotonNetwork.RPC (PhotonView view, System.String methodName, PhotonTargets target, System.Boolean encrypt, System.Object[] parameters) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at PhotonView.RPC (System.String methodName, PhotonTargets target, System.Object[] parameters) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Character.CastSpell (Character+SpellCastType _type, UnityEngine.GameObject _eventReceiver, Character+SpellCastModifier _modifier, System.Int32 _sheatheRequired, System.Single _mobileCastMoveMult) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Item.StartEffectsCast (Character _targetChar) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Skill.StartEffectsCast (Character _targetChar) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Item.Use (Character _character) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Item.QuickSlotUse () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Skill.QuickSlotUse () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at Item.TryQuickSlotUse () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at QuickSlot.Activate () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at CharacterQuickSlotManager.QuickSlotInput (System.Int32 _index) [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at LocalCharacterControl.UpdateQuickSlots () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
  at LocalCharacterControl.Update () [0x00000] in <d5e04b413fbc401e8fbb9a9c16a31f0f>:0 
```
=> hook with unityexplorer => calls `Character.SpellCastProcess`, sometimes doesnt??
=> calls `Character.CastSpell`
=> works if we force the spell => cant use `NONE` cast type? => cause `StartEffectsCast` isnt called
=> force call during `Skill.QuickSlotUse` => still doesnt work? => `Character.SendPerformSpellCastTrivial` works => lmao just set it to `Sit` and we'll just reverse our sitting => cast modif `Immobilized` runs anim, ie `Attack` doesnt  

## Item still playing animation
Problem: Item plays animation even when effectcondition is false
Ideas:
- Set base animation to `NONE`, then play animation `SummonGhost` in effect with `SL_PlayAnimation` => doesn't play any animation
- Add `SL_PlayAnimation` with Anim `NONE` on inverted effectcondition => plays `SummonGhost` instead
- 