using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DebugMode
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class DebugMode : BaseUnityPlugin
    {
        // Choose a GUID for your project. Change "myname" and "mymod".
        public const string ID = "com.exp111.DebugMode";
        // Choose a NAME for your project, generally the same as your Assembly Name.
        public const string NAME = "DebugMode";
        // Increment the VERSION when you release a new version of your mod.
        public const string VERSION = "1.1";

        public static ConfigEntry<bool> EnableDebug;
        public static ConfigEntry<bool> ShowHierarchyViewer;
        public static ConfigEntry<bool> ShowPhotonStats;

        public static ManualLogSource Log;
        public static List<CustomDebugCmd> DebugCommands;

        void Awake()
        {
            try
            {
                Log = Logger;
                Log.LogMessage("Awake");

                // Setup Config
                EnableDebug = Config.Bind("General", "Enable Debug", true);
                ShowHierarchyViewer = Config.Bind("General", "Show Hierarchy Viewer", false);
                ShowPhotonStats = Config.Bind("General", "Show Photon Stats", false);

                Config.SettingChanged += (_, e) => ApplyConfig();

                LoadDebugCommands();

                // Harmony is for patching methods. If you're not patching anything, you can comment-out or delete this line.
                var harmony = new Harmony(ID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during DebugMode.Awake: {e}");
            }
        }

        private void LoadDebugCommands()
        {
            //TODO: LoadDebugCommands();
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(CustomDebugCmd)))
                    DebugCommands.Add((CustomDebugCmd)Activator.CreateInstance(type));
            }
        }

        public static void ApplyConfig()
        {
            try
            {
                Global.CheatsEnabled = EnableDebug.Value;
                SetHierarchyViewer(ShowHierarchyViewer.Value);
                SetPhotonStats(ShowPhotonStats.Value);
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during DebugMode.ApplyConfig: {e}");
            }
        }

        public static void SetHierarchyViewer(bool val)
        {
            var hierarchy = GameObject.Find("HierarchyViewer");
            if (val)
            {
                if (hierarchy == null)
                {
                    GameObject gameObject = new GameObject("HierarchyViewer");
                    DontDestroyOnLoad(gameObject);
                    gameObject.AddComponent<SceneHierarchyViewer>();
                }
            }
            else if (hierarchy != null)
            {
                Destroy(hierarchy);
            }
        }

        public static void SetPhotonStats(bool val)
        {
            var photon = GameObject.Find("PhotonStats");
            if (val)
            {
                if (photon == null)
                {
                    GameObject gameObject2 = new GameObject("PhotonStats");
                    DontDestroyOnLoad(gameObject2);
                    gameObject2.AddComponent<PhotonStatsGui>().statsWindowOn = true;
                }
            }
            else if (photon != null)
            {
                Destroy(photon);
            }
        }
    }

    [HarmonyPatch(typeof(Global), nameof(Global.Awake))] // Can't use ProcessDebug cause it may not be called if we dont have a DEBUG.txt
    class Global_Awake_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                DebugMode.ApplyConfig();
            }
            catch (Exception e)
            {
                DebugMode.Log.LogMessage($"Exception during Global.Awake hook: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(Global), nameof(Global.ProcessDebug))]
    class Global_ProcessDebug_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            // Skip original debug function if its called
            return false;
        }
    }

    // Hooks into the 
    [HarmonyPatch(typeof(ChatPanel), nameof(ChatPanel.CheckForDebugCommand))]
    class ChatPanel_DebugCommand_Patch
    {
        [HarmonyPostfix]
        public static void Prefix(ChatPanel __instance, bool __result)
        {
            if (!__result) // false = cmd found
                return; // no need for us 

            var command = __instance.m_chatEntry.text;
            if (!command.StartsWith("/")) // not a chat cmd
                return;

            var args = command.Split(' ');

            var func = args[0].Substring(1); // remove /
            foreach (var cmd in DebugMode.DebugCommands)
            {
                if (!func.Equals(cmd.Command, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if (!Global.CheatsEnabled && cmd.Cheat)
                    continue;

                cmd.Run(args);
            }
        }
    }
}
