namespace MoreChatCommands
{
    public class Unstuck : CustomDebugCmd
    {
        public override string Command => "unstuck";

        public override string HelpText => "Unstucks the player.";

        public override string Usage => "/unstuck";

        public override bool Cheat => false;

        public override bool Run(string[] args)
        {
            var localPlayer = GetLocalPlayer();
            if (localPlayer == null)
            {
                ChatError("Couldn't get local player.");
                return true;
            }

            //DT_CharacterCheats::OnStuck
            localPlayer.ForceCancel(false, true);
            localPlayer.ResetPosition();
            return true;
        }
    }
}
