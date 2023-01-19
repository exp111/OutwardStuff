using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using NodeCanvas.StateMachines;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace QuestGraphDumper
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class QuestGraphDumper : BaseUnityPlugin
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

                //TODO: quests are only loaded ingame, so we need to either find a better point or manually trigger this per config button/keypress
                DumpQuests();
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during QuestGraphDumper.Awake: {e}");
            }
        }

        public void DumpQuests()
        {
            // gets all objects
            var questTrees = Resources.FindObjectsOfTypeAll<QuestTree>();
            DebugLog($"Found {questTrees.Length} quest trees");
            foreach (var questTree in questTrees) 
            {
                DebugLog($"Dumping {questTree}");
                DumpTree(questTree);
            }
        }

        public void DumpTree(QuestTree tree)
        {
            var filename = $"{tree.name}.xml";
            //INFO: as we use scriptengine the plugininfo location isnt set
            var path = "E:\\D\\Visual Studio\\Projects\\OutwardMods\\QuestGraphDumper\\out";
            //path = Directory.GetParent(path).FullName;
            path = Path.Combine(path, filename);
            DebugLog($"Dumping to {path}");
            var graph = new Graph(tree);
            DebugLog($"Created {graph}");
            // Write all nodes
            System.Xml.Serialization.XmlSerializer writer = new System.Xml.Serialization.XmlSerializer(typeof(Graph));
            FileStream file = File.Create(path);
            writer.Serialize(file, graph);
            file.Close();
            DebugLog($"Written");
        }

        public void OnDestroy()
        {
            try
            {
                harmony?.UnpatchSelf();
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during QuestGraphDumper.OnDestroy: {e}");
            }
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
