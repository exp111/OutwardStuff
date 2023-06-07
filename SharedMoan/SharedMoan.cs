using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NodeCanvas.Framework;
using NodeCanvas.Tasks.Actions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;

namespace SharedMoan
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class SharedMoan : BaseUnityPlugin
    {
        public const string ID = "com.exp111.SharedMoan";
        public const string NAME = "SharedMoan";
        public const string VERSION = "1.0";

        public const string RPCObjectName = "SharedMoanRPC";
        public const int VIEW_ID = 961; // reserved on https://github.com/Mefino/ModdingCommunityResources/blob/main/id-reservations/photon-viewid-reservations.json


        public static ManualLogSource Log;
        private static Harmony harmony;

        public void Awake()
        {
            try
            {
                Log = Logger;
                Log.LogMessage("Awake");
                DebugLog("Using a DEBUG build.");

                harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ID);

                // Init rpc object
                var obj = new GameObject(RPCObjectName);
                DontDestroyOnLoad(obj);
                obj.hideFlags |= HideFlags.HideAndDontSave;

                obj.AddComponent<PlaySound_OnExecute_Patch>();

                var view = obj.AddComponent<PhotonView>();
                view.viewID = VIEW_ID;
                Log.LogMessage($"Registered SharedMoanRPC with ViewID {view.viewID}");
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during SharedMoan.Awake: {e}");
            }
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
            var rpc = GameObject.Find(RPCObjectName);
            if (rpc) Destroy(rpc);
        }

        [Conditional("DEBUG")]
        public static void DebugLog(string message)
        {
            Log.LogMessage(message);
        }

        [Conditional("TRACE")]
        public static void DebugTrace(string message)
        {
            Log.LogMessage(message);
        }
    }

    // Hooks into the chat cmd check function and checks our commands
    [HarmonyPatch(typeof(PlaySound), nameof(PlaySound.OnExecute))]
    public class PlaySound_OnExecute_Patch : Photon.MonoBehaviour
    {
        public static PlaySound_OnExecute_Patch Instance;
        public void Awake()
        {
            Instance = this;
        }

        // Whitelisted sounds that can be played
        public static readonly Dictionary<GlobalAudioManager.Sounds, bool> AllowedSounds = new()
        {
            { GlobalAudioManager.Sounds.LOC_EXCL_Mumble_QuietWoman01, true },
            { GlobalAudioManager.Sounds.LOC_EXCL_Mumble_QuietWoman02, true },
            { GlobalAudioManager.Sounds.LOC_EXCL_Mumble_QuietWoman03, true }
        };

        [HarmonyPrefix]
        public static void Prefix(PlaySound __instance)
        {
            try
            {
                SharedMoan.DebugLog($"PlaySound.OnExecute start: {__instance}");
                if (AllowedSounds.ContainsKey(__instance.Sound))
                {
                    SharedMoan.DebugTrace($"Sending sound...");
                    Instance.photonView.RPC(nameof(PlaySoundAt), PhotonTargets.Others, new object[]
                    {
                        __instance.Sound
                    });
                }
            }
            catch (Exception e)
            {
                SharedMoan.Log.LogMessage($"Exception during PlaySound.OnExecute hook: {e}");
            }
        }

        [PunRPC]
        private void PlaySoundAt(GlobalAudioManager.Sounds sound)
        {
            try
            {
                SharedMoan.DebugLog($"PlaySoundAt: {sound}");
                if (AllowedSounds.ContainsKey(sound))
                {
                    SharedMoan.DebugTrace($"Playing sound...");
                    OnExecute(sound);
                }
            }
            catch (Exception e)
            {
                SharedMoan.Log.LogMessage($"Exception during PlaySoundAt RPC: {e}");
            }
        }

        // Stolen from PlaySound.OnExecute
        private void OnExecute(GlobalAudioManager.Sounds sound)
        {
            // currently this is playing at the camera, but imo thats fine atm
            Global.AudioManager.PlaySound(sound, 0f, 1f, 1f, 1f, 1f);
        }
    }
}
