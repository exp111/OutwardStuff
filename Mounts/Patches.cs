using HarmonyLib;
using System;
using UnityEngine;

namespace Mounts
{
    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    public class CharacterAwakePatch
    {
        static void Postfix(Character __instance)
        {
            Mounts.DebugTrace($"Adding CharacterMount to {__instance.Name}");
            __instance.gameObject.AddComponent<CharacterMount>();
        }
    }

    // Dismount mount before dying
    [HarmonyPatch(typeof(DefeatScenariosManager), nameof(DefeatScenariosManager.ActivateDefeatScenario))]
    public class DefeatScenarioPatch
    {
        static void Prefix(DefeatScenariosManager __instance, DefeatScenario _scenario)
        {
            Mounts.DebugLog($"Destroying all mounts before defeat scenario.");
            Mounts.MountManager.DestroyAllMountInstances();
        }
    }

    // Patch to warp mount with character
    [HarmonyPatch(typeof(Character), nameof(Character.Teleport), new Type[] { typeof(Vector3), typeof(Vector3) })]
    public class CharacterTeleport
    {
        static void Postfix(Character __instance, Vector3 _pos, Vector3 _rot)
        {
            CharacterMount characterMount = __instance.gameObject.GetComponent<CharacterMount>();

            if (characterMount != null && characterMount.HasActiveMount)
            {
                Mounts.DebugLog($"Warping {characterMount.ActiveMount} with {characterMount.Character.Name}");
                characterMount.ActiveMount.Teleport(_pos, Quaternion.Euler(_rot));
            }
        }
    }

    //TODO: do the same for itemdisplays
    
    public class SkillOverridePatches
    {
        static bool ForceCastSkill(Skill __instance)
        {
            var characterMount = __instance.m_ownerCharacter.GetComponent<CharacterMount>();
            if (characterMount != null && characterMount.HasActiveMount && characterMount.ActiveMount.IsMounted)
            {
                Mounts.DebugTrace($"Checking if skill is ours");
                if (!Mounts.Skills.ContainsKey(__instance.ItemID))
                    return true; // dont skip

                //check cooldown // no need to check conditions as we only have the unsummon skill and that checks if we're mounted
                if (__instance.InCooldown())
                {
                    if (__instance.m_ownerCharacter && __instance.m_ownerCharacter.CharacterUI)
                    {
                        __instance.m_ownerCharacter.CharacterUI.ShowInvalidActionNotification(__instance.gameObject, "Notification_Skill_Cooldown");
                    }
                    return false; // skip
                }

                Mounts.DebugTrace($"Skill {__instance} is ours, forcing");
                // INFO: we're forcing the skill because it wont work otherwise. idk why
                // TODO: find out why
                __instance.m_ownerCharacter.SetLastUsedSkill(__instance);
                __instance.m_ownerCharacter.ForceCastSpell(__instance.ActivateEffectAnimType,
                    __instance.gameObject,
                    __instance.CastModifier,
                    __instance.GetCastSheathRequired(), __instance.MobileCastMovementMult);
                return false; // dont run original
            }
            return true;
        }

        [HarmonyPatch(typeof(ItemDisplay), nameof(ItemDisplay.TryUse))]
        class ItemDisplay_TryUse
        {
            static bool Prefix(ItemDisplay __instance)
            {
                try
                {
                    Mounts.DebugLog($"ItemDisplay.TryUse hook for {__instance}");
                    if (__instance.RefItem is Skill)
                        return ForceCastSkill((Skill)__instance.RefItem);
                    
                }
                catch (Exception e)
                {
                    Mounts.Log.LogMessage($"Exception during ItemDisplay.TryUse hook: {e}");
                }
                return true; // dont skip
            }
        }

        [HarmonyPatch(typeof(Skill), nameof(Skill.QuickSlotUse))]
        class Skill_QuickSlotUse
        {
            static bool Prefix(Skill __instance)
            {
                try
                {
                    Mounts.DebugLog($"Skill.HasAllRequirements hook for {__instance}");
                    return ForceCastSkill(__instance);
                }
                catch (Exception e)
                {
                    Mounts.Log.LogMessage($"Exception during Skill.HasAllRequirements hook: {e}");
                }
                return true; // dont skip
            }
        }
    }
}
