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
        // Choose a GUID for your project. Change "myname" and "mymod".
        public const string ID = "com.exp111.SharedMoan";
        // Choose a NAME for your project, generally the same as your Assembly Name.
        public const string NAME = "SharedMoan";
        // Increment the VERSION when you release a new version of your mod.
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

                // Harmony is for patching methods. If you're not patching anything, you can comment-out or delete this line.
                harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ID);

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

        [HarmonyPrefix]
        public static void Postfix(PlaySound __instance)
        {
            try
            {
                SharedMoan.DebugLog($"PlaySound.OnExecute start: {__instance}");
                if (AllowedSounds.ContainsKey(__instance.Sound))
                {
                    SharedMoan.DebugTrace($"Sending sound...");
                    Instance.photonView.RPC(nameof(PlaySoundAt), PhotonTargets.Others, new object[]
                    {
                    __instance.Sound,
                    __instance.PlayAt,
                    __instance.TransPos,
                    __instance.Pos
                    });
                }
            }
            catch (Exception e)
            {
                SharedMoan.Log.LogMessage($"Exception during PlaySound.OnExecute hook: {e}");
            }
        }

        public static readonly Dictionary<GlobalAudioManager.Sounds, bool> AllowedSounds = new()
        {
            { GlobalAudioManager.Sounds.LOC_EXCL_Mumble_QuietWoman01, true },
            { GlobalAudioManager.Sounds.LOC_EXCL_Mumble_QuietWoman02, true },
            { GlobalAudioManager.Sounds.LOC_EXCL_Mumble_QuietWoman03, true }
        };

        [PunRPC]
        private void PlaySoundAt(GlobalAudioManager.Sounds sound, PlaySound.PositionType playAt, BBParameter<Transform> transPos, Vector3 pos)
        {
            SharedMoan.DebugLog($"PlaySoundAt: {sound}, {playAt}, {transPos}, {pos}");
            if (AllowedSounds.ContainsKey(sound))
            {
                SharedMoan.DebugTrace($"Playing sound...");
                OnExecute(sound, playAt, transPos, pos);
            }
        }

        // Stolen from PlaySound.OnExecute
        private void OnExecute(GlobalAudioManager.Sounds Sound, PlaySound.PositionType PlayAt, BBParameter<Transform> TransPos, Vector3 Pos)
        {
            if (PlayAt == PlaySound.PositionType.Transform && TransPos.value != null)
            {
                Global.AudioManager.PlaySoundAndFollow(Sound, TransPos.value, 0f, 1f, 1f, 1f, 1f);
                return;
            }
            else if (PlayAt == PlaySound.PositionType.Pos)
            {
                Global.AudioManager.PlaySoundAtPosition(Sound, Pos, 0f, 1f, 1f, 1f, 1f);
                return;
            }

            Global.AudioManager.PlaySound(Sound, 0f, 1f, 1f, 1f, 1f);
        }
    }
}
