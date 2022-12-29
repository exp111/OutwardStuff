using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AnimationTest
{
    // Replaces the trog character animator with a custom one from a asset bundle
    [BepInPlugin(ID, NAME, VERSION)]
    public class AnimationTest : BaseUnityPlugin
    {
        const string ID = "com.exp111.animationtest";
        const string NAME = "AnimationTest";
        const string VERSION = "1.0";

        public static AnimationTest Instance;
        public object Animator = null;
        private static Harmony harmony;

        public void Awake()
        {
            Instance = this;
            Logger.LogInfo("Awake");

            try
            {
                var basePath = Paths.PluginPath;
                // loads the bundle "com.exp111.animationtest" from "BepInEx/plugins/AnimationTest/"
                var path = Path.Combine(basePath, "AnimationTest", "com.exp111.animationtest");
                Logger.LogInfo(path);
                var assetBundle = AssetBundle.LoadFromFile(path);
                Logger.LogInfo(assetBundle);
                var assets = assetBundle.LoadAllAssets();
                foreach (var asset in assets)
                {
                    Logger.LogInfo($"Found asset: {asset}, type: {asset.GetType()}");
                    if (asset is RuntimeAnimatorController)
                    {
                        Animator = asset;
                        Logger.LogInfo($"Found RuntimeAnimatorController: {asset}");
                    }
                }
                //Animator = assets.FirstOrDefault();
            }
            catch (Exception e)
            {
                Logger.LogInfo($"Something fucked up during loading: {e.Message}");
            }

            // Apply Patches
            harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ID);
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }

        public void Log(object data)
        {
            Logger.LogInfo(data);
        }
    }

    // Override the trogPlayer AnimatorController with our own
    [HarmonyPatch(typeof(Character), "Awake")]
    class Character_AwakePatch
    {
        [HarmonyPostfix]
        public static void Patch(Character __instance)
        {
            AnimationTest.Instance.Log($"AnimatorName for {__instance}: {__instance.Animator.runtimeAnimatorController.name}");
            if (__instance.Animator.runtimeAnimatorController.name == "ac_trogPlayer" 
                    && AnimationTest.Instance.Animator != null)
            {
                AnimationTest.Instance.Log($"Replacing animator for {__instance} with {(RuntimeAnimatorController)AnimationTest.Instance.Animator}");
                __instance.Animator.runtimeAnimatorController = (RuntimeAnimatorController)AnimationTest.Instance.Animator;
            }
        }
    }

}
