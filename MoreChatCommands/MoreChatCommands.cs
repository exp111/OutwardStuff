using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MoreChatCommands
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class MoreChatCommands : BaseUnityPlugin
    {
        // Choose a GUID for your project. Change "myname" and "mymod".
        public const string ID = "com.exp111.MoreChatCommands";
        // Choose a NAME for your project, generally the same as your Assembly Name.
        public const string NAME = "MoreChatCommands";
        // Increment the VERSION when you release a new version of your mod.
        public const string VERSION = "1.0";

        public static ManualLogSource Log;
        public static List<CustomDebugCmd> DebugCommands;

        void Awake()
        {
            try
            {
                Log = Logger;
                Log.LogMessage("Awake");

                LoadDebugCommands();

                // Harmony is for patching methods. If you're not patching anything, you can comment-out or delete this line.
                var harmony = new Harmony(ID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during MoreChatCommands.Awake: {e}");
            }
        }

        // Finds all debug commands and adds them into a list
        private void LoadDebugCommands()
        {
            DebugCommands = new List<CustomDebugCmd>();
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(CustomDebugCmd)))
                    DebugCommands.Add((CustomDebugCmd)Activator.CreateInstance(type));
            }
        }
    }

    // Hooks into the chat cmd check function and checks our commands
    [HarmonyPatch(typeof(ChatPanel), nameof(ChatPanel.CheckForDebugCommand))]
    class ChatPanel_DebugCommand_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ChatPanel __instance, bool __result)
        {
            // INFO: return true to run original function; false => skip original
            // INFO: __result = true, is no command; false => cmd, no chat msg
            try
            {
                var command = __instance.m_chatEntry.text;
                if (!command.StartsWith("/")) // not a chat cmd
                {
                    __result = true; // no cmd
                    return false; // no need to run as we've already checked
                }

                // split cmd into arguments
                var args = command.Split(' ');
                // the command we wanna call
                var func = args[0].Substring(1); // remove /
                foreach (var cmd in MoreChatCommands.DebugCommands)
                {
                    if (!func.Equals(cmd.Command, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    if (!Global.CheatsEnabled && cmd.Cheat)
                        continue;

                    cmd.Run(args);
                    __result = false; // we found a cmd
                    return false; // no need to run anymore
                }
            }
            catch (Exception e)
            {
                MoreChatCommands.Log.LogMessage($"Exception during ChatPanel.CheckForDebugCommand: {e}");
            }
            return true; // run original as we either found nothing or something went wrong
        }
    }
}
