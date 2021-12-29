using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Randomizer
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class Randomizer : BaseUnityPlugin
    {
        public const string ID = "com.exp.randomizer";
        public const string NAME = "Randomizer";
        public const string VERSION = "1.0.0";

        public static ManualLogSource Log;

        public static ConfigEntry<bool> ExampleConfig;

        // Droptable name to existing droptable //TODO: save as what? string array? ItemDrop list?
        public static Dictionary<string, string> DropTableMap = new Dictionary<string, string>();

        // Awake is called when your plugin is created. Use this to set up your mod.
        internal void Awake()
        {
            Log = this.Logger;
            Log.LogMessage($"Hello world from {NAME} {VERSION}!");

            ExampleConfig = Config.Bind("ExampleCategory", "ExampleSetting", false, "This is an example setting.");

            var harmony = new Harmony(ID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        // Update is called once per frame. Use this only if needed.
        // You also have all other MonoBehaviour methods available (OnGUI, etc)
        /*internal void Update()
        {

        }

        [HarmonyPatch(typeof(ResourcesPrefabManager), nameof(ResourcesPrefabManager.Load))]
        public class ResourcesPrefabManager_Load
        {
            static void Postfix()
            {
            }
        }*/
    }

    [HarmonyPatch(typeof(Merchant), "Initialize")]
    public class MerchantInitializePatch
    {
        static AccessTools.FieldRef<Merchant, Transform> prefabRef = AccessTools.FieldRefAccess<Merchant, Transform>("m_merchantInventoryTablePrefab");

        [HarmonyPrefix]
        public static void Prefix(Merchant __instance)
        {
            //TODO: change m_merchantInventoryTablePrefab prefab here
            Randomizer.Log.LogMessage($"merchant.initialize {__instance}");
            var prefabTransform = prefabRef(__instance);
            var dropable = prefabTransform.GetComponent<Dropable>();

            //TODO: can we even use these fields? or do we have to iterate over all childs of the transform
            var guaranteedDrops = (List<GuaranteedDrop>)AccessTools.Field(typeof(Dropable), "m_allGuaranteedDrops").GetValue(dropable);
            Randomizer.Log.LogMessage(guaranteedDrops);
            foreach (var drop in guaranteedDrops)
            {
                var drops = (List<BasicItemDrop>)AccessTools.Field(typeof(GuaranteedDrop), "m_itemDrops").GetValue(drop);
                var str = "";
                foreach (var item in drops)
                {
                    if (item != -1)
                    {
                        str += item.DroppedItem.DisplayName + ",";
                    }
                }
                Randomizer.Log.LogMessage($"- {str}");
            }
            //TODO: m_mainDropTables
            var dropTables = (List<GuaranteedDrop>)AccessTools.Field(typeof(Dropable), "m_mainDropTables").GetValue(dropable);
            Randomizer.Log.LogMessage(dropTables);
            //TODO: m_conditionalDropTables
            var conditionalTables = (List<GuaranteedDrop>)AccessTools.Field(typeof(Dropable), "m_conditionalDropTables").GetValue(dropable);
            Randomizer.Log.LogMessage(conditionalTables);
            //TODO: m_conditionalGuaranteedDrops
            var conditionalGuaranteedTables = (List<GuaranteedDrop>)AccessTools.Field(typeof(Dropable), "m_conditionalGuaranteedDrops").GetValue(dropable);
            Randomizer.Log.LogMessage(conditionalGuaranteedTables);

            //__instance.
        }
    }
}
