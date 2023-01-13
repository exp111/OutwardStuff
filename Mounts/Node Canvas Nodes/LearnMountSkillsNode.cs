using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using System.Linq;
using UnityEngine;

namespace Mounts
{
    public partial class Mounts
    {
        public class LearnMountSkillsNode : ActionNode
        {
            public override Status OnExecute(Component agent, IBlackboard bb)
            {
                static void TryLearnSkill(Character character, string skillName)
                {
                    var skill = Skills.Values.FirstOrDefault(s => s.Name == skillName);
                    if (skill == null)
                    {
                        Log.LogMessage($"Did not find '{skillName}' skill!");
                    }
                    else
                    {
                        if (character.Inventory.SkillKnowledge.GetItemFromItemID(skill.New_ItemID) == null)
                        {
                            character.Inventory.ReceiveSkillReward(skill.New_ItemID);
                        }
                    }
                }

                Character PlayerTalking = bb.GetVariable<Character>("gInstigator").GetValue();

                // Learn despawn skill
                TryLearnSkill(PlayerTalking, "Despawn Mount");
                TryLearnSkill(PlayerTalking, "Mount Knowledge");

                return Status.Success;
            }
        }
    }
}
