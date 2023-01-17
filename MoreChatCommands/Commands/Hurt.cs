using System;
using UnityEngine;
using static EnvironmentConditions;

namespace MoreChatCommands
{
    public class Hurt : CustomDebugCmd
    {
        public override string Command => "hurt";

        public override string HelpText => "Hurt yourself.";

        public override string Usage => "/hurt health <amount>\n/hurt stamina <amount>\n/hurt mana <amount>";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            if (args.Length < 3) 
                return false;

            var localPlayer = GetLocalPlayer();
            if (localPlayer == null)
            {
                ChatError("Couldn't get local player.");
                return true;
            }
            var stats = localPlayer.Stats;
            if (stats == null)
            {
                ChatError("Couldn't get local player stats.");
                return true;
            }
            // look at subcommands
            switch (args[1].ToLower())
            {
                case "health":
                    return HurtStat(ref stats.m_health, args[2], "Health");
                case "stamina":
                    return HurtStat(ref stats.m_stamina, args[2], "Stamina");
                case "mana":
                    return HurtStat(ref stats.m_mana, args[2], "Mana");
            }
            return false;
        }

        private bool HurtStat(ref float stat, string amount, string name)
        {
            if (!float.TryParse(amount, out var am))
            {
                ChatError($"Invalid amount ({amount})");
                return true;
            }

            stat = Mathf.Max(stat - am, 0f); // clamp to 0
            ChatPrint($"Hurt {name} by {amount} (now {stat}).");
            return true;
        }
    }
}
