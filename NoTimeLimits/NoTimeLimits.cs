using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NoTimeLimits
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class NoTimeLimits : BaseUnityPlugin
    {
        public const string ID = "com.exp111.QuestGraphDumper";
        public const string NAME = "QuestGraphDumper";
        public const string VERSION = "1.0";

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
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during SharedMoan.Awake: {e}");
            }
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
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
