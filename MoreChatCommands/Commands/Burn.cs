using System;
using UnityEngine;
using static EnvironmentConditions;

namespace MoreChatCommands
{
    public class Burn : CustomDebugCmd
    {
        public override string Command => "burn";

        public override string HelpText => "Burn a stat.";

        public override string Usage => "/burn health <amount>\n/burn stamina <amount>\n/burn mana <amount>";

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
                    return HurtStat(ref stats.m_burntHealth, args[2], stats.MaxHealth, "Health");
                case "stamina":
                    return HurtStat(ref stats.m_burntStamina, args[2], stats.MaxStamina, "Stamina");
                case "mana":
                    return HurtStat(ref stats.m_mana, args[2], stats.MaxMana, "Mana");
            }
            return false;
        }

        private bool HurtStat(ref float stat, string amount, float max, string name)
        {
            if (!float.TryParse(amount, out var am))
            {
                ChatError($"Invalid amount ({amount})");
                return true;
            }

            stat = Mathf.Clamp(stat + am, 0f, max); // clamp to 0
            ChatPrint($"Burnt {name} by {amount} (now {stat}).");
            return true;
        }
    }
}
