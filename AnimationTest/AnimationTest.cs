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

        public void Awake()
        {
            Instance = this;
            Logger.LogInfo("Awake");
            var basePath = Paths.PluginPath;
            try
            {
                var path = Path.Combine(basePath, "AnimationTest", "com.exp111.animationtest");
                Logger.LogInfo(path);
                var asset = AssetBundle.LoadFromFile(path);
                Logger.LogInfo(asset);
                var assets = asset.LoadAllAssets();
                foreach (var assetName in asset.GetAllAssetNames())
                {
                    Logger.LogInfo($"Found asset: {assetName}");
                }
                foreach (var assetName in assets)
                {
                    Logger.LogInfo($"Found asset: {assetName}, type: {assetName.GetType()}");
                }
                Animator = assets.FirstOrDefault();
            }
            catch (Exception e)
            {
                Logger.LogInfo($"Fucky wucky: {e.Message}");
            }

            var harmony = new Harmony(ID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void Log(object data)
        {
            Logger.LogInfo(data);
        }
    }

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
