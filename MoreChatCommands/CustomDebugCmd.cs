using UnityEngine;

namespace MoreChatCommands
{
    public abstract class CustomDebugCmd
    {
        public abstract string Command { get; }
        public abstract string HelpText { get; }
        // <> for a required argument, [] for optional args, (a|b) pick one, [a|b] pick one optional
        public abstract string Usage { get; }
        public abstract bool Cheat { get; }

        // Returns true if cmd was run correctly else should print usage
        public abstract bool Run(string[] args);

        public static void ChatError(string text) => ChatPrint(text, Global.LIGHT_RED);
        public static void ChatPrint(string text) => ChatPrint(text, Global.LIGHT_GREEN);
        public static void ChatPrint(string text, Color clr)
        {
            MoreChatCommands.DebugTrace(text);
            //TODO: cache somewhere?
            var chat = GetChatPanel();
            if (chat != null)
            {
                chat.ChatMessageReceived("System", Global.SetTextColor(text, clr));
            }
        }
        public static void ClearChat()
        {
            var chat = GetChatPanel();
            if (chat != null)
            {
                // for all chat entries
                foreach (var entry in chat.m_messageArchive)
                {
                    // hide + destroy entry
                    entry.Hide();
                    Object.Destroy(entry);
                }
                // then clear the history
                chat.m_messageArchive.Clear();
                // scroll to the top
                chat.Invoke("DelayedScroll", 0.1f);
            }
        }
        private static ChatPanel GetChatPanel()
        {
            CharacterUI characterUI = SplitScreenManager.Instance.GetCharacterUI(0);
            return characterUI?.ChatPanel;
        }

        protected static Character GetLocalPlayer()
        {
            if (SplitScreenManager.Instance.LocalPlayers.Count == 0)
            {
                MoreChatCommands.DebugTrace($"No local players exist.");
                return null;
            }
            var localPlayer = SplitScreenManager.Instance.LocalPlayers[0].AssignedCharacter;
            if (localPlayer == null)
            {
                MoreChatCommands.DebugTrace($"Local player has no character.");
                return null;
            }
            return localPlayer;
        }
    }
}
