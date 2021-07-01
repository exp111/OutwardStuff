using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace RagdollCanon
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class RagdollCanon : BaseUnityPlugin
    {
        const string ID = "com.exp111.ragdollcanon";
        const string NAME = "RagdollCanon";
        const string VERSION = "1.0";


        public static RagdollCanon Instance;

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
        }
    }

    // Patches need to be outside of the BepInExPlugin
    [HarmonyPatch(typeof(LocalCharacterControl), "UpdateInteraction")]
    class UpdateInteraction_Patch
    {
        static void Postfix(LocalCharacterControl __instance)
        {
            //RagdollCanon.Instance.Log("Calling UpdateInteraction:Postfix");
            if (__instance.InputLocked || __instance.Character.CharacterUI.ChatPanel.IsChatFocused)
            {
                return;
            }
            if (RagdollCanon.launchKey.Value.IsDown())
            {
                //RagdollCanon.Instance.Log($"UpdateInteraction:Postfix: Key is down! Setting ragdoll to {!__instance.Character.RagdollActive}");
                try
                {
                    var value = !__instance.Character.RagdollActive;
                    /* __instance.Character.SetRagdollActive(!__instance.Character.RagdollActive); */
                    RagdollCanon.methodRagdollActive.Invoke(__instance.Character, 
                        new object[] { value });

                    if (value)
                    {
                        // FIXME: doesnt work that great on the y(/z?) axis, instead mostly a shallow launch. maybe get that axis from Global.MainCamera?
                        __instance.Character.RagdollRigidbody.AddForce(
                            __instance.Character.CharacterCamera.transform.forward * RagdollCanon.launchStrength.Value, 
                            ForceMode.Impulse);
                    }
                }
                catch (Exception e)
                {
                    RagdollCanon.Instance.Log($"We done fucked up: {e.Message}");
                }
            }
        }
    }
}
