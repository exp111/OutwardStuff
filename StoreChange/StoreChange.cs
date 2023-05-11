using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using NodeCanvas.Tasks.Actions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using static MapMagic.ObjectPool;

namespace StoreChange
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class StoreChange : BaseUnityPlugin
    {
        public const string ID = "com.exp111.StoreChange";
        public const string NAME = "StoreChange";
        public const string VERSION = "1.0";

        public static ManualLogSource Log;
        private static Harmony harmony;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<OTWStoreAPI.StoreIDs> Store;
        public static ConfigEntry<bool> ForceOnline;

        public void Awake()
        {
            try
            {
                Log = Logger;
                Log.LogMessage("Awake");
                DebugLog("Using a DEBUG build.");

                SetupConfig();

                harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ID);
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during StoreChange.Awake: {e}");
            }
        }

        private void SetupConfig()
        {
            Enabled = Config.Bind("General", "Enable", true, "Enabled");
            Store = Config.Bind("General", "Store", OTWStoreAPI.StoreIDs.GOG, "Selected store");
            Store.SettingChanged += Store_SettingChanged;
            ForceOnline = Config.Bind("General", "Force Online", true);
        }

        private void Store_SettingChanged(object sender, EventArgs e)
        {
            Log.LogMessage($"Changed Store to {Store.Value}");
            if (StoreManager.Instance)
            {
                DebugLog($"Calling LoadBundle.");
                StartCoroutine(StoreManager.Instance.LoadBundle());
            }
            else
            {
                Log.LogMessage("No StoreManager Instance");
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

#if DEBUG
    [HarmonyDebug]
#endif
    [HarmonyPatch(typeof(StoreManager), nameof(StoreManager.LoadBundle), MethodType.Enumerator)]
    public class StoreManager_LoadBundle
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var cur = new CodeMatcher(instructions);
            /*
                Find end of store set:
                this.m_storeRecovered = true;
		        if (this.m_started)
		        {
			        this.InitManager();
		        }
                ---
                IL_012C: ldloc.1
                IL_012D: ldc.i4.1
                IL_012E: stfld     bool StoreManager::m_storeRecovered

                Add:
                OurFunction(storeManager)
                ---
                ldloc.1
                OurFunction()
            */
            /*foreach (var code in instructions)
            {
                StoreChange.DebugLog(code.ToString());
            }*/
            var storeRecoveredField = AccessTools.Field(typeof(StoreManager), nameof(StoreManager.m_storeRecovered));

            // find the m_storeRecovered set
            StoreChange.DebugTrace($"trying to find match with field {storeRecoveredField}");
            cur.MatchForward(true, // start after the set
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Stfld, storeRecoveredField)
                ).Advance(1); // skip the stfld
            StoreChange.DebugTrace($"found match at: {cur.Pos}, op: {cur.Instruction}");

            // insert our function
            StoreChange.DebugTrace($"inserting function");
            cur.Insert(
                new CodeInstruction(OpCodes.Ldloc_1), // put the store manager on the stack
                Transpilers.EmitDelegate<Action<StoreManager>>((manager) =>
                {
                    StoreChange.Log.LogMessage($"Changing store {manager.m_loadedStore} to {StoreChange.Store.Value}");
                    manager.m_loadedStore = StoreChange.Store.Value;
                })
            );
            StoreChange.DebugTrace($"inserted function at {cur.Pos}");

            var e = cur.InstructionEnumeration();
            foreach (var code in e)
            {
                StoreChange.DebugLog(code.ToString());
            }
            return e;
        }
    }
    //TODO: InitManager hook?

    [HarmonyPatch(typeof(OTWStoreAPI), nameof(OTWStoreAPI.OnUserRetrieved))]
    public class OTWStoreAPI_OnUserRetrieved
    {
        [HarmonyPrefix]
        public static void Prefix(OTWStoreAPI __instance, ref bool _retrieved)
        {
            try
            {
                StoreChange.DebugLog($"OTWStoreAPI.OnUserRetrieved start: {__instance}, _retrieved: {_retrieved}");
                if (StoreChange.ForceOnline.Value)
                    _retrieved = true;
            }
            catch (Exception e)
            {
                StoreChange.Log.LogMessage($"Exception during OTWStoreAPI.OnUserRetrieved hook: {e}");
            }
        }
    }

    [HarmonyPatch(typeof(OTWStoreAPI), nameof(OTWStoreAPI.OnGameEntitlementRetrieved))]
    public class OTWStoreAPI_OnGameEntitlementRetrieved
    {
        [HarmonyPrefix]
        public static void Prefix(OTWStoreAPI __instance, ref bool _retrieved)
        {
            try
            {
                StoreChange.DebugLog($"OTWStoreAPI.OnGameEntitlementRetrieved start: {__instance}, _retrieved: {_retrieved}");
                if (StoreChange.ForceOnline.Value)
                    _retrieved = true;
            }
            catch (Exception e)
            {
                StoreChange.Log.LogMessage($"Exception during OTWStoreAPI.OnGameEntitlementRetrieved hook: {e}");
            }
        }
    }

    
}
