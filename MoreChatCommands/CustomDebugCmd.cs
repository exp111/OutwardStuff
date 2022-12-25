using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MapMagic.Layout;
using UnityEngine;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;
using static ParadoxNotion.Services.Logger;

namespace MoreChatCommands
{
    public abstract class CustomDebugCmd
    {
        public abstract string Command { get; }
        public abstract string HelpText { get; }
        public abstract string Usage { get; }
        public abstract bool Cheat { get; }

        // Returns true if cmd was run correctly else should print usage
        public abstract bool Run(string[] args);

        public static void ChatError(string text) => ChatPrint(text, Global.LIGHT_RED);
        public static void ChatPrint(string text) => ChatPrint(text, Global.LIGHT_GREEN);
        public static void ChatPrint(string text, Color clr)
        {
            //TODO: cache somewhere?
            CharacterUI characterUI = SplitScreenManager.Instance.GetCharacterUI(0);
            if (characterUI)
            {
                characterUI.ChatPanel.ChatMessageReceived("System", Global.SetTextColor(text, clr));
            }
        }
    }

    public class Help : CustomDebugCmd
    {
        public override string Command => "help";
        public override string HelpText => "Provides help/list of commands.";
        public override string Usage => "/help\n/help <command>";
        public override bool Cheat => false;

        List<(string, string, string, bool)> OriginalCommands =
            new List<(string, string, string, bool)>()
            {
                ("toggleDebug", "Sets debug mode/cheats to the given value.", "/toggleDebug <on/true/off/false>", false),
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

    public class Spawn : CustomDebugCmd
    {
        public override string Command => "spawn";

        public override string HelpText => "Spawn items";

        public override string Usage => "/spawn <itemID> <(amount)>";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            if (args.Length < 2) // not enough args
                return false;

            // Parse item id
            if (!int.TryParse(args[1], out var id))
            {
                ChatError($"Not a valid item id ({args[1]}).");
                return true;
            }

            // Parse amount
            var amount = 1;
            if (args.Length > 2)
            {
                if (!int.TryParse(args[2], out amount) || amount <= 0)
                {
                    ChatError($"Not a valid amount ({args[2]}).");
                    return true;
                }
            }

            // Check if item id exists
            if (!ResourcesPrefabManager.ITEM_PREFABS.ContainsKey(args[1]))
            {
                ChatError($"This item does not exist ({args[1]}).");
                return true;
            }

            SpawnItem(id, amount);
            return true;
        }

        public static void SpawnItem(int itemID, int amount)
        {
            if (SplitScreenManager.Instance.LocalPlayers.Count == 0)
            {
                ChatError("No players found.");
                return;
            }
            var localPlayer = SplitScreenManager.Instance.LocalPlayers[0].AssignedCharacter;
            if (localPlayer == null)
            {
                ChatError("Player has no character.");
                return;
            }

            // stolen from DT_ItemSpawner.SpawnItem
            Vector3 vector = localPlayer.CenterPosition + localPlayer.transform.forward * 1.5f;
            if (Physics.Raycast(vector, localPlayer.transform.up * -1f, out _, 10f, Global.LargeEnvironmentMask))
            {
                Quaternion localRotation = localPlayer.transform.localRotation;
                Item item = ItemManager.Instance.GenerateItemNetwork(itemID);
                item.transform.position = vector;
                item.transform.rotation = localRotation;
                if (item.HasMultipleUses) //TODO: else spawn more items?
                    item.GetComponent<MultipleUsage>().RemainingAmount = amount;
                item.gameObject.AddComponent<SafeFalling>();
            }
        }
    }

    public class Give : CustomDebugCmd
    {
        public override string Command => "give";

        public override string HelpText => "Give items into inventory";

        public override string Usage => "/give <itemID> <(amount)>";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            if (args.Length < 2) // not enough args
                return false;

            // Parse item id
            if (!int.TryParse(args[1], out var id))
            {
                ChatError($"Not a valid item id ({args[1]}).");
                return true;
            }

            // Parse amount
            var amount = 1;
            if (args.Length > 2)
            {
                if (!int.TryParse(args[2], out amount) || amount <= 0)
                {
                    ChatError($"Not a valid amount ({args[2]}).");
                    return true;
                }
            }

            // Check if item id exists
            if (!ResourcesPrefabManager.ITEM_PREFABS.ContainsKey(args[1]))
            {
                ChatError($"This item does not exist ({args[1]}).");
                return true;
            }

            GiveItem(id, amount);
            return true;
        }

        public static void GiveItem(int itemID, int amount)
        {
            if (SplitScreenManager.Instance.LocalPlayers.Count == 0)
            {
                ChatError("No players found.");
                return;
            }
            var localPlayer = SplitScreenManager.Instance.LocalPlayers[0].AssignedCharacter;
            if (localPlayer == null)
            {
                ChatError("Player has no character.");
                return;
            }

            //TODO: instead use GenerateItem?
            localPlayer.Inventory.ReceiveItemReward(itemID, amount, false);
        }
    }

    public class PhotonStats : CustomDebugCmd
    {
        public override string Command => "togglePhoton";

        public override string HelpText => "Toggles the photon stats overlay.";

        public override string Usage => "/togglePhoton";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            var photon = GameObject.Find("PhotonStats");
            if (photon == null)
            {
                GameObject gameObject = new GameObject("PhotonStats");
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                gameObject.AddComponent<PhotonStatsGui>().statsWindowOn = true;
            }
            else
            {
                UnityEngine.Object.Destroy(photon);
            }
            return true;
        }
    }

    public class HierarchyViewer : CustomDebugCmd
    {
        public override string Command => "toggleHierarchy";

        public override string HelpText => "Toggles the hierarchy viewer overlay.";

        public override string Usage => "/toggleHierarchy";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            var hierarchy = GameObject.Find("HierarchyViewer");
            if (hierarchy == null)
            {
                GameObject gameObject = new GameObject("HierarchyViewer");
                UnityEngine.Object.DontDestroyOnLoad(gameObject);
                gameObject.AddComponent<SceneHierarchyViewer>();
            }
            else
            {
                UnityEngine.Object.Destroy(hierarchy);
            }
            return true;
        }
    }
}
