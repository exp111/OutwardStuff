namespace MoreChatCommands
{
    public class Clear : CustomDebugCmd
    {
        public override string Command => "clear";

        public override string HelpText => "Clears the chat.";

        public override string Usage => "/clear";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            ClearChat();
            return true;
        }
    }
}
