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

namespace DisableDesertHaze
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class DisableDesertHaze : BaseUnityPlugin
    {
        public const string ID = "com.exp111.DisableDesertHaze";
        public const string NAME = "DisableDesertHaze";
        public const string VERSION = "1.0.0";

        public static ManualLogSource Log;
        public static ConfigEntry<bool> Enabled;

        void Awake()
        {
            try
            {
                Log = this.Logger;
                Log.LogMessage("Awake");

                Enabled = Config.Bind("General", "Enabled", true, "Disables the desert heat haze effect.");

                // Harmony is for patching methods. If you're not patching anything, you can comment-out or delete this line.
                var harmony = new Harmony(ID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Log.LogMessage($"Exception during DisableDesertHaze.Awake: {ex}");
            }
        }

        [HarmonyPatch(typeof(DepthDistort), nameof(DepthDistort.OnRenderImage))]
        public class DepthDistort_OnRenderImage_Patch
        {
            // Returns if original is executed (true) or not (false) 
            static bool Prefix(DepthDistort __instance, RenderTexture source, RenderTexture destination)
            {
                if (!Enabled.Value)
                    return true;

                // Copies source into destination without changing anything // https://docs.unity3d.com/ScriptReference/Graphics.Blit.html
                Graphics.Blit(source, destination);

                return false;
            }
        }
    }
}
