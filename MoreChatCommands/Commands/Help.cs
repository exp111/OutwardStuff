using System;
using System.Collections.Generic;

namespace MoreChatCommands
{
    public class Help : CustomDebugCmd
    {
        public override string Command => "help";
        public override string HelpText => "Provides help/list of commands.";
        public override string Usage => "/help\n/help <command>";
        public override bool Cheat => false;

        List<(string, string, string, bool)> OriginalCommands =
            new List<(string, string, string, bool)>()
            {
                ("toggleDebug", "Sets debug mode/cheats to the given value.", "/toggleDebug (on|true|off|false)", false),
                ("tp", "Teleports the player to a given position.", "/tp <Vector3>", true),
                ("weather", "Shows/changes the current weather.", "/weather\n/weather <weather>", true),
                ("defeat", "List/force/clear defeat scenarios.", "/defeat list\n/defeat force <defeat>\n/defeat clear", true) //TODO: this right?
            };

        public override bool Run(string[] args)
        {
            if (args.Length == 1) // list all cmds
            {
                //TODO: sort
                // first list all 
                foreach (var cmd in OriginalCommands)
                {
                    // don't show if it's a cheat
                    if (cmd.Item4 && !Global.CheatsEnabled)
                        continue;

                    List(cmd.Item1, cmd.Item2);
                }

                // then list all custom cmds
                foreach (var cmd in MoreChatCommands.DebugCommands)
                {
                    // don't show if it's a cheat
                    if (cmd.Cheat && !Global.CheatsEnabled)
                        continue;

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
                    //TODO: instead use only class or smth
                    PrintUsage(cmd.Item1, cmd.Item2, cmd.Item3, cmd.Item4);
                    return true;
                }
                // then custom ones
                foreach (var cmd in MoreChatCommands.DebugCommands)
                {
                    if (!func.Equals(cmd.Command, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    PrintUsage(cmd.Command, cmd.HelpText, cmd.Usage, cmd.Cheat);
                    return true;
                }
                // havent found cmd if we're here
                ChatError("Command not found!");
                return true;
            }
            return false;
        }

        public void PrintUsage(string name, string helpText, string usage, bool cheat)
        {
            // Print like:
            /*
            help (Cheat) - Provides help/list of commands

            /help
            /help <cmd>
            */
            var cheatText = "";
            if (cheat)
                cheatText = "(Cheat) ";

            ChatPrint($"{name} {cheatText}- {helpText}\n\n{usage}");
        }

        public void List(string name, string helpText)
        {
            ChatPrint($"{name} - {helpText}");
        }
    }
}
