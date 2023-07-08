using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;

namespace Tutorial
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class Tutorial : BaseUnityPlugin
    {
        public const string ID = "com.exp111.Tutorial";
        public const string NAME = "Tutorial";
        public const string VERSION = "1.0";

        public static ManualLogSource Log;
        private static Harmony Harmony;

        public void Awake()
        {
            try
            {
                Log = Logger;
                Log.LogMessage("Awake");
                DebugLog("Using a DEBUG build.");

#if DEBUG
                // wait for debugger, or 10 seconds
                var timeout = 0;
                while (!Debugger.IsAttached || timeout < 10)
                {
                    timeout++;
                    Thread.Sleep(1000);
                }
#endif

                // Init Harmony
                Harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ID);
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during Tutorial.Awake: {e}");
            }
        }

        public void OnDestroy()
        {
            // Delete your stuff
            Harmony?.UnpatchSelf();
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
}
