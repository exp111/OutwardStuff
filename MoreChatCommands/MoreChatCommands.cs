using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            DebugLog($"Found {DebugCommands.Count} custom commands ({DebugCommands.Join(c => c.Command)}).");
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

    // Hooks into the chat cmd check function and checks our commands
    [HarmonyPatch(typeof(ChatPanel), nameof(ChatPanel.CheckForDebugCommand))]
    class ChatPanel_DebugCommand_Patch
    {
        [HarmonyPrefix]
        public static bool Prefix(ChatPanel __instance, ref bool __result)
        {
            // INFO: return true to run original function; false => skip original
            // INFO: __result = true, is no command; false => cmd, no chat msg
            MoreChatCommands.DebugTrace($"ChatPanel.CheckForDebugCommand start");
            try
            {
                var command = __instance.m_chatEntry.text.Trim();
                if (!command.StartsWith("/")) // not a chat cmd
                {
                    MoreChatCommands.DebugTrace("Not a command.");
                    __result = true; // not a cmd
                    return false; // no need to run anymore
                }

                // split cmd into arguments
                var args = command.Split(' ');
                // the command we wanna call
                var func = args[0].Substring(1); // remove /
                // check through the commands if ones matches
                foreach (var cmd in MoreChatCommands.DebugCommands)
                {
                    if (!func.Equals(cmd.Command, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    MoreChatCommands.DebugTrace($"Found command: {cmd.Command}.");

                    if (!Global.CheatsEnabled && cmd.Cheat)
                    {
                        __result = false; // no chat msg
                        return false; // found cmd but its disabled, stop here
                    }

                    MoreChatCommands.DebugTrace("Running command.");
                    try
                    {
                        if (!cmd.Run(args))
                        {
                            // used cmd incorrectly, show usage
                            CustomDebugCmd.ChatError($"Usage:\n{cmd.Usage}");
                        }
                    }
                    catch (Exception e)
                    {
                        CustomDebugCmd.ChatError($"Something went wrong! See the log for more information.");
                        MoreChatCommands.Log.LogMessage($"Exception while running Command {cmd.Command}: {e}");
                    }
                    __result = false; // we found a cmd
                    return false; // no need to run anymore
                }
            }
            catch (Exception e)
            {
                MoreChatCommands.Log.LogMessage($"Exception during ChatPanel.CheckForDebugCommand hook: {e}");
            }
            MoreChatCommands.DebugTrace("Found no valid command.");
            return true; // run original as we either found nothing or something went wrong
        }
    }
}
