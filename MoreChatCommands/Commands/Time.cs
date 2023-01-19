using System;
using UnityEngine;
using static EnvironmentConditions;

namespace MoreChatCommands
{
    public class Time : CustomDebugCmd
    {
        public override string Command => "time";

        public override string HelpText => "Shows/sets the time of day.";

        public override string Usage => "/time\n/time set (morning|noon|afternoon|evening|night)\n/time set <hour>";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            if (args.Length == 1) // Show current TOD
            {
                var time = TOD_Sky.Instance.Cycle.DateTime;
                ChatPrint($"Current Time: {time:hh:mm:ss}");
                return true;
            }
            
            // look at subcommands
            switch (args[1].ToLower())
            {
                case "set":
                    return Set(args);
            }
            return false;
        }

        private bool Set(string[] args)
        {
            if (args.Length < 3)
                return false;

            int? wantedHour = null;
            if (Enum.TryParse<TimeOfDayTimeSlot>(args[2], true, out var enumedHour)) // first try to parse as enum slot
            {
                wantedHour = (int)enumedHour;
            }
            else if (int.TryParse(args[2], out var intHour)) // then try to parse as int hour
            {
                wantedHour = intHour;
            }

            if (wantedHour == null)
            {
                ChatError($"Invalid time ({args[2]})");
                return true;
            }
            Instance.SetTODNoGameTime((float)wantedHour);
            ChatPrint($"Time set to: {args[2]} ({wantedHour}:00).");
            return true;
        }
    }
}
