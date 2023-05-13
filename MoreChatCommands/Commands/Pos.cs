namespace MoreChatCommands
{
    public class Pos : CustomDebugCmd
    {
        public override string Command => "pos";

        public override string HelpText => "Prints the current position of the player into the chat.";

        public override string Usage => "/pos";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            var localPlayer = GetLocalPlayer();
            if (localPlayer == null)
            {
                ChatError("Couldn't get local player.");
                return true;
            }
            ChatPrint($"Position: {localPlayer.transform.position}");
            return true;
        }
    }
}
