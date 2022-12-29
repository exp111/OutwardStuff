using HarmonyLib;

namespace MoreChatCommands
{
    public class Skill : CustomDebugCmd
    {
        public override string Command => "skill";

        public override string HelpText => "Manages skills.";

        public override string Usage => "/skill learn <skillID>\n/skill unlearn/forget <skillID>\n/skill list";

        public override bool Cheat => true;

        public override bool Run(string[] args)
        {
            if (args.Length < 2)
                return false;

            // look at subcommands
            switch (args[1].ToLower())
            {
                case "learn":
                    return Learn(args, true);
                case "unlearn":
                case "forget":
                    return Learn(args, false);
                case "list":
                    return List(args);
            }
            return false;
        }

        private bool List(string[] args)
        {
            if (SplitScreenManager.Instance.LocalPlayers.Count == 0)
            {
                ChatError("No players found.");
                return true;
            }
            var localPlayer = SplitScreenManager.Instance.LocalPlayers[0].AssignedCharacter;
            if (localPlayer == null)
            {
                ChatError("Player has no character.");
                return true;
            }

            var skills = localPlayer.Inventory.SkillKnowledge.m_learnedItems;
            if (localPlayer.Inventory.SkillKnowledge.m_learnedItems.Count > 0)
            {
                var txt = skills.Join(s => $"- {s.DisplayName} ({s.ItemID})", "\n");
                ChatPrint($"Currently learned skills:\n{txt}");
            }
            else
            {
                ChatPrint("No known skills.");
            }
            return true;
        }

        private bool Learn(string[] args, bool learn)
        {
            if (args.Length < 3)
                return false;

            // Parse item id
            if (!int.TryParse(args[2], out var id))
            {
                ChatError($"Not a valid skill id ({args[2]}).");
                return true;
            }

            // Check if item id exists
            if (!ResourcesPrefabManager.ITEM_PREFABS.TryGetValue(args[2], out var item))
            {
                ChatError($"This skill does not exist ({args[2]}).");
                return true;
            }

            if (item is not global::Skill)
            {
                ChatError($"This skill does not exist ({args[2]}).");
                return true;
            }
            var skill = item as global::Skill;

            //TODO: move into helper func?
            if (SplitScreenManager.Instance.LocalPlayers.Count == 0)
            {
                ChatError("No players found.");
                return true;
            }
            var localPlayer = SplitScreenManager.Instance.LocalPlayers[0].AssignedCharacter;
            if (localPlayer == null)
            {
                ChatError("Player has no character.");
                return true;
            }

            var knowsSkill = localPlayer.Inventory.SkillKnowledge.IsItemLearned(id);
            if (learn) // Learn
            {
                if (knowsSkill) 
                {
                    ChatError($"Player knows skill {skill.DisplayName} ({args[2]}) already.");
                    return true;
                }

                // Taken from DT_SkillProficiencyCheats.OnSkillKnownledgeChanged
                var newSkill = ItemManager.Instance.CloneItem(skill); 
                newSkill.ChangeParent(localPlayer.Inventory.SkillKnowledge.transform);
                //INFO: skill should automatically add itself to knowledge
                ChatPrint($"Learned skill {skill.DisplayName} ({skill.ItemID})");
            }
            else // Forget
            {
                if (!knowsSkill)
                {
                    ChatError($"Player doesn't know skill {skill.DisplayName} ({args[2]}).");
                    return true;
                }

                // Taken from DT_SkillProficiencyCheats.OnSkillKnownledgeChanged
                var knownSkill = localPlayer.Inventory.SkillKnowledge.GetItemFromItemID(id);
                ItemManager.Instance.DestroyItem(knownSkill);
                //TODO: does it remove itself or do we need to help?
                ChatPrint($"Forgot skill {skill.DisplayName} ({skill.ItemID})");
            }
            return true;
        }
    }
}
