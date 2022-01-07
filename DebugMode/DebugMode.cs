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
        public const string VERSION = "1.0.0";

        public static ConfigEntry<bool> EnableTPCommand;
        public static ConfigEntry<bool> ShowHierarchyViewer;
        public static ConfigEntry<bool> ShowPhotonStats;

        public static ManualLogSource Log;

        void Awake()
        {
            try
            {
                Log = Logger;
                Log.LogMessage("Awake");

                // Setup Config
                EnableTPCommand = Config.Bind("General", "Enable Teleport Command", false);
                ShowHierarchyViewer = Config.Bind("General", "Show Hierarchy Viewer", false);
                ShowPhotonStats = Config.Bind("General", "Show Photon Stats", false);

                Config.SettingChanged += (_, e) => ApplyConfig();

                // Harmony is for patching methods. If you're not patching anything, you can comment-out or delete this line.
                var harmony = new Harmony(ID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Log.LogMessage($"Exception during DebugMode.Awake: {ex}");
            }
        }

        public static void ApplyConfig()
        {
            Global.TeleportCommandEnabled = EnableTPCommand.Value;
            SetHierarchyViewer(ShowHierarchyViewer.Value);
            SetPhotonStats(ShowPhotonStats.Value);
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
    class Global_ProcessDebug_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                Global.CheatsEnabled = true;
                DebugMode.ApplyConfig();
            }
            catch (Exception e)
            {
                DebugMode.Log.LogMessage($"Exception during Global.Awake hook: {e}");
            }
        }
    }
}
