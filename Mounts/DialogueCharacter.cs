using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using SideLoader;
using System.Collections.Generic;
using UnityEngine;

namespace Mounts
{
    public class DialogueCharacter
    {
        public string UID { get; set; }
        public string Name { get; set; }

        public string SpawnSceneBuildName { get; set; }
        public Vector3 SpawnPosition { get; set; }
        public Vector3 SpawnRotation { get; set; }
        public Character.SpellCastType StartingPose { get; set; }

        public SL_Character.VisualData CharVisualData { get; set; } = new()
        {
            Gender = Character.Gender.Male,
            SkinIndex = 0,
            HairColorIndex = 0,
            HairStyleIndex = 0,
            HeadVariationIndex = 0,
        };

        public int HelmetID = -1;
        public int ChestID = -1;
        public int BootsID = -1;
        public int WeaponID = -1;
        public int OffhandID = -1;
        public int BackpackID = -1;

        public event Action<DialogueTree, Character> OnSetupDialogueGraph;

        public SL_Character CreateAndApplyTemplate()
        {
            SL_Character template = new()
            {
                UID = UID,
                Name = Name,
                Faction = Character.Factions.NONE,
                StartingPose = StartingPose,
                SaveType = CharSaveType.Scene,
                SceneToSpawn = SpawnSceneBuildName,
                SpawnPosition = SpawnPosition,
                SpawnRotation = SpawnRotation,
                CharacterVisualsData = CharVisualData,
                Helmet_ID = HelmetID,
                Chest_ID = ChestID,
                Boots_ID = BootsID,
                Weapon_ID = WeaponID,
                Shield_ID = OffhandID,
                Backpack_ID = BackpackID,
            };

            template.ApplyTemplate();
            template.OnSpawn += Template_OnSpawn;

            return template;
        }

        private void Template_OnSpawn(Character character, string rpcData)
        {
            GameObject dialogueTemplate = GameObject.Instantiate(Resources.Load<GameObject>("editor/templates/DialogueTemplate"));
            dialogueTemplate.transform.parent = character.transform;
            dialogueTemplate.transform.position = character.transform.position;

            // set Dialogue Actor name
            DialogueActor ourActor = character.GetComponentInChildren<DialogueActor>();
            ourActor.SetName(Name);

            // setup dialogue tree
            DialogueTreeController graphController = character.GetComponentInChildren<DialogueTreeController>();
            Graph graph = graphController.graph;

            // the template comes with an empty ActorParameter, we can use that for our NPC actor.
            List<DialogueTree.ActorParameter> actors = (graph as DialogueTree)._actorParameters;
            actors[0].actor = ourActor;
            actors[0].name = ourActor.name;

            OnSetupDialogueGraph?.Invoke(graph as DialogueTree, character);
        }
    }
}
