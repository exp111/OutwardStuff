using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using NodeCanvas.DialogueTrees;
using NodeCanvas.StateMachines;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace GraphDumper
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class GraphDumper : BaseUnityPlugin
    {
        public const string ID = "com.exp111.GraphDumper";
        public const string NAME = "GraphDumper";
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
                //INFO: as we use scriptengine the plugininfo location isnt set
                var path = "E:\\D\\Visual Studio\\Projects\\OutwardMods\\QuestGraphDumper\\out";
                DumpQuests(path);
                DumpDialogues(path);
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during QuestGraphDumper.Awake: {e}");
            }
        }

        public void DumpQuests(string path)
        {
            var outFolder = Path.Combine(path, "quests");
            if (Directory.Exists(outFolder)) // first delete the folder and any content inside
                Directory.Delete(outFolder, true);
            Directory.CreateDirectory(outFolder);
            // gets all quests
            var questTrees = Resources.FindObjectsOfTypeAll<QuestTree>();
            DebugLog($"Found {questTrees.Length} quest trees");
            foreach (var questTree in questTrees) 
            {
                DebugLog($"Dumping quest {questTree}");
                DumpTree(questTree, outFolder);
            }
        }

        public void DumpDialogues(string path)
        {
            var outFolder = Path.Combine(path, "dialogues");
            if (Directory.Exists(outFolder)) // first delete the folder and any content inside
                Directory.Delete(outFolder, true);
            Directory.CreateDirectory(outFolder);
                
            // gets all dialogues
            var dialogueTrees = Resources.FindObjectsOfTypeAll<DialogueTreeExt>();
            DebugLog($"Found {dialogueTrees.Length} dialogues");
            foreach (var dialogueTree in dialogueTrees)
            {
                DebugLog($"Dumping dialogue {dialogueTree}");
                DumpTree(dialogueTree, outFolder);
            }
        }

        private static string IterateFileName(string fileName)
        {
            if (!File.Exists(fileName)) 
                return fileName;

            FileInfo fi = new FileInfo(fileName);
            string ext = fi.Extension;
            string name = fi.FullName.Substring(0, fi.FullName.Length - ext.Length);

            int i = 2;
            while (File.Exists($"{name}_{i}{ext}"))
            {
                i++;
            }

            return $"{name}_{i}{ext}";
        }

        public void DumpTree(NodeCanvas.Framework.Graph tree, string folder)
        {
            var filename = $"{tree.name}.xml";
            var path = Path.Combine(folder, filename);
            path = IterateFileName(path); // if a file exists, append a file
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
