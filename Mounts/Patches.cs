using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
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
}
