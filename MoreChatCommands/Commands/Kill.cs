namespace MoreChatCommands
{
    public class Kill : CustomDebugCmd
    {
        public override string Command => "kill";

        public override string HelpText => "Kills the player.";

        public override string Usage => "/kill";

        public override bool Cheat => false;

        public override bool Run(string[] args)
        {
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
            stats.SetHealth(0f);
            return true;
        }
    }
}
