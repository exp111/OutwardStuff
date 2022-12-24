using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;

namespace DebugMode
{
    public abstract class CustomDebugCmd
    {
        public abstract string Command { get; }
        public abstract string HelpText { get; }
        public abstract string Usage { get; }
        public abstract bool Cheat { get; }

        // Returns true if cmd was run correctly else should print usage
        public abstract bool Run(string[] args);

        public static void ChatPrint(string text)
        {
            //TODO: get chatpanel instance
            //ChatPanel.ChatMessageReceived("System", Global.SetTextColor(text, Global.LIGHT_GREEN));
        }
    }

    public class Help : CustomDebugCmd
    {
        public override string Command => "help";
        public override string HelpText => "Provides help/list of commands.";
        public override string Usage => "/help\n/help <command>";
        public override bool Cheat => false;

        List<(string, string, string)> OriginalCommands =
            new List<(string, string, string)>()
            {
                ("toggleDebug", "Toggles debug.", "/toggleDebug"),
                ("tp", "Teleports the player to a given position.", "/tp <Vector3>"),
                ("weather", "Shows/changes the current weather.", "/weather\n/weather <weather>"),
                ("defeat", "List/run/clear defeat scenarios.", "/defeat list\n/defeat force <defeat>\n/defeat clear") //TODO: this right?
            };

        public override bool Run(string[] args)
        {
            if (args.Length == 1) // list all cmds
            {
                //TODO: sort
                // first list all 
                foreach (var cmd in OriginalCommands)
                {
                    //TODO: dont list if cheat + !CheatsEnabled?
                    List(cmd.Item1, cmd.Item2);
                }

                // then list all custom cmds
                foreach (var cmd in DebugMode.DebugCommands)
                {
                    //TODO: dont list if cheat + !CheatsEnabled?
                    List(cmd.Command, cmd.HelpText);
                }
                return true;
            }
            else if (args.Length == 2) // show info about cmd
            {
                //TODO: dont list if cheat + !CheatsEnabled?
                var func = args[1];
                // first search original cmds
                foreach (var cmd in OriginalCommands)
                {
                    if (!func.Equals(cmd.Item1, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    PrintUsage(cmd.Item1, cmd.Item2, cmd.Item3);
                    return true;
                }
                // then custom ones
                foreach (var cmd in DebugMode.DebugCommands)
                {
                    if (!func.Equals(cmd.Command, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    PrintUsage(cmd.Command, cmd.HelpText, cmd.Usage);
                    return true;
                }
                // havent found cmd if we're here
                ChatPrint("Command not found!");
                return true;
            }
            return false;
        }

        public void PrintUsage(string name, string helpText, string usage)
        {
            // Print like:
            /*
            help - Provides help/list of commands

            /help
            /help <cmd>
            */
            ChatPrint($"{name} - {helpText}");
            ChatPrint($"\n{usage}");
        }

        public void List(string name, string helpText)
        {
            ChatPrint($"{name} - {helpText}");
        }
    }
}
