using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace RagdollCannon
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class RagdollCannon : BaseUnityPlugin
    {
        const string ID = "com.exp111.ragdollcanon";
        const string NAME = "RagdollCanon";
        const string VERSION = "1.0";


        public static RagdollCannon Instance;

        // Consume Items when crafting fails: enable for Alchemy / Cooking
        public static ConfigEntry<KeyboardShortcut> launchKey;
        public static ConfigEntry<float> launchStrength;
        public static MethodInfo methodRagdollActive = null;

        /// <summary>
        /// Set up Mod Configuration
        /// </summary>
        void SetupConfig()
        {
            launchKey = Config.Bind("General", "launchKey", new KeyboardShortcut(KeyCode.Delete), "The key which launches the player");
            launchStrength = Config.Bind("General", "launchStrength", 500f, "The strength with which the player is launched");
        }

        public void Log(object data)
        {
            Logger.LogInfo(data);
        }

        /// <summary>
        /// Initialization
        /// </summary>
        private void Awake()
        {
            Instance = this;
            Log("Awake");

            // Initialize Settings
            SetupConfig();

            // Initialize Methods
            methodRagdollActive = typeof(Character).GetMethod("SetRagdollActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Let Harmony Patch Outward's Behavior
            var harmony = new Harmony(ID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            LocalCharacterControl_Patch.Init();
        }
    }


    class LocalCharacterControl_Patch : Photon.MonoBehaviour
    {
        internal static LocalCharacterControl_Patch Instance;
        internal const int VIEW_ID = 981; //TODO: find out how tf those are used by mods. we need a viewID for photon to work. currently it seems like we just hope for the best
        internal static void Init()
        {
            var obj = new GameObject("RagdollCannonRPC");
            DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;
            Instance = obj.AddComponent<LocalCharacterControl_Patch>();
            var view = obj.AddComponent<PhotonView>();
            view.viewID = VIEW_ID;
        }

        [PunRPC]
        public void RagdollCannon_SetRagdoll(string charUID, bool value)
        {
            try
            {
                var character = CharacterManager.Instance.GetCharacter(charUID);
                //RagdollCannon.Instance.Log($"RPC Called: Setting ragdoll to {value} on {character}");
                /* __instance.Character.SetRagdollActive(!__instance.Character.RagdollActive); */
                RagdollCannon.methodRagdollActive.Invoke(character,
                    new object[] { value });
            }
            catch (Exception e)
            {
                RagdollCannon.Instance.Log($"RPC: We done fucked up: {e.Message}");
            }
        }

        // Patches need to be outside of the BepInExPlugin
        [HarmonyPatch(typeof(LocalCharacterControl), "UpdateInteraction")]
        class LocalCharacterControl_UpdateInteraction
        {
            static void Postfix(LocalCharacterControl __instance)
            {
                //FIXME: move to own update or prefix to allow using other buttons
                //RagdollCanon.Instance.Log("Calling UpdateInteraction:Postfix");
                if (__instance.InputLocked || __instance.Character.CharacterUI.ChatPanel.IsChatFocused)
                {
                    return;
                }
                if (RagdollCannon.launchKey.Value.IsDown())
                {
                    //RagdollCannon.Instance.Log($"UpdateInteraction:Postfix: Key is down! Setting ragdoll to {!__instance.Character.RagdollActive}");
                    try
                    {
                        var value = !__instance.Character.RagdollActive;
                        //RagdollCannon.Instance.Log($"photonView:{Instance.photonView}");
                        Instance.photonView.RPC(nameof(RagdollCannon_SetRagdoll), PhotonTargets.All, new object[]
                        {
                        __instance.Character.UID.ToString(),
                        value
                        });
                        //RagdollCannon.Instance.Log("Called RPC!");

                        if (value)
                        {
                            // FIXME: doesnt work that great on the y(/z?) axis, instead mostly a shallow launch. maybe get that axis from Global.MainCamera?
                            __instance.Character.RagdollRigidbody.AddForce(
                                __instance.Character.CharacterCamera.transform.forward * RagdollCannon.launchStrength.Value,
                                ForceMode.Impulse);
                        }
                        else
                        {
                            // To fix the character from spaghetting out, reset animation/locomotion
                            // this may cause some side effects
                            // TODO: actually caused by this.InLocomotion being false, so maybe check this in if (value)
                            // because spazzing out also causes RagdollActive to being false, needs more debugging though
                            __instance.Character.ForceBackToLocomotion();
                        }
                    }
                    catch (Exception e)
                    {
                        RagdollCannon.Instance.Log($"We done fucked up: {e.Message}");
                    }
                }
            }
        }
    }
}
