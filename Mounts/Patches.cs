using HarmonyLib;
using SideLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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
                Mounts.Log.LogMessage($"Warping {characterMount.ActiveMount} with {characterMount.Character.Name}");
                characterMount.ActiveMount.Teleport(_pos, Quaternion.Euler(_rot));
            }
        }
    }

    [HarmonyPatch(typeof(Skill), nameof(Skill.HasAllRequirements))]
    public class SkillHasAllReqs
    {
        static bool Prefix(Skill __instance, bool _tryingToActivate, ref bool __result)
        {
            try
            {
                Mounts.DebugLog($"Skill.HasAllRequirements hook for {__instance}");
                var characterMount = __instance.m_ownerCharacter.GetComponent<CharacterMount>();
                if (characterMount != null && characterMount.HasActiveMount && characterMount.ActiveMount.IsMounted)
                {
                    Mounts.DebugTrace($"Checking if skill is ours");
                    if (!Mounts.Skills.ContainsKey(__instance.ItemID))
                        return true; // dont skip

                    //check cooldown + conditions
                    if (__instance.InCooldown())
                    {
                        __result = false; // will be false
                        return false; // skip
                    }

                    Mounts.DebugTrace($"Skill {__instance} is ours, bypassing");
                    //__instance.SkillUsed();
                    __instance.QuickSlotUse();
                    __result = true;
                    return false; // dont run original
                }
            }
            catch (Exception e)
            {
                Mounts.Log.LogMessage($"Exception during Skill.HasAllRequirements hook: {e}");
            }
            return true; // dont skip
        }
    }
}
