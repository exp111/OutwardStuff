using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using NodeCanvas.DialogueTrees;
using SideLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mounts
{
    [BepInPlugin(ID, NAME, VERSION)]
    public partial class Mounts : BaseUnityPlugin
    {
        public const string ID = "com.exp111.Mounts";
        public const string NAME = "ExpMounts";
        public const string VERSION = "1.0.3";

        public const string MOUNT_SPAWN_KEY = $"{NAME}_Spawn";
        public const string MOUNT_DESPAWN_KEY = $"{NAME}_Despawn";

        public static float SCENE_LOAD_DELAY = 10f;

        public static ManualLogSource Log;
        private static Harmony harmony;


        public static MountManager MountManager
        {
            get; private set;
        }

        public static SLPack SLPack;
        public static Dictionary<int, SL_Skill> Skills = new();

        public static ConfigEntry<float> WorldDropChanceThreshold;
        public static ConfigEntry<float> WorldDropChanceMinimum;
        public static ConfigEntry<float> WorldDropChanceMaximum;

        public static ConfigEntry<bool> EnableWeightLimit;
        // Awake is called when your plugin is created. Use this to set up your mod.
        internal void Awake()
        {
            try
            {
                Log = this.Logger;
                Log.LogMessage("Awake");
                DebugLog("Using a DEBUG build.");
                //TODO: log build time?

                //TODO: instead use skills
                CustomKeybindings.AddAction(MOUNT_SPAWN_KEY, KeybindingsCategory.CustomKeybindings, ControlType.Both);
                CustomKeybindings.AddAction(MOUNT_DESPAWN_KEY, KeybindingsCategory.CustomKeybindings, ControlType.Both);

                // if SLPacks are already loaded (happens when reloading with scriptengine, initialize now)
                if (SL.PacksLoaded)
                    SL_OnSLPacksLoaded();
                SL.OnPacksLoaded += SL_OnSLPacksLoaded;
                SceneManager.sceneLoaded += SceneManager_SceneLoaded;

                WorldDropChanceThreshold = Config.Bind<float>(NAME, "Drop Threshold", 1, "You need to roll this number or less in order for a whistle to drop.");
                WorldDropChanceMinimum = Config.Bind<float>(NAME, "Drop Chance Range Minimum", 0, "Minimum number to roll between");
                WorldDropChanceMaximum = Config.Bind<float>(NAME, "Drop Chance Range Maximum", 500, "Maximum number to roll between");

                EnableWeightLimit = Config.Bind<bool>(NAME, "Enable Weight Limits", true, "Enables the Mount weight limit system.");

                SetupNPCs();
                harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ID);
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during Mounts.Awake: {e}");
            }
        }

        // Used for ScriptEngine
        internal void OnDestroy()
        {
            harmony?.UnpatchSelf();
            //TODO: unload resources?
        }

        public static void SpawnMount(CharacterMount characterMount, string speciesName)
        {
            try
            {
                Mounts.DebugLog($"spawning mount {speciesName}");
                MountSpecies mountSpecies = Mounts.MountManager.GetSpeciesDefinitionByName(speciesName);
                var character = characterMount.Character;

                if (characterMount.HasActiveMount)
                {
                    Mounts.DebugLog($"Character {character} already has active mount {characterMount.ActiveMount}. Deleting");
                    DespawnMount(characterMount);
                }

                if (mountSpecies != null)
                {
                    BasicMountController basicMountController = Mounts.MountManager.CreateMountFromSpecies(character, mountSpecies, OutwardHelpers.GetPositionAroundCharacter(character), character.transform.rotation);

                }
                else
                {
                    Log.LogMessage($"Could not find Species with Species Name: {speciesName}, in the list of definitions.");
                }
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during Mounts.SpawnMount: {e}");
            }
        }

        public static void DespawnMount(CharacterMount characterMount)
        {
            try
            {
                Mounts.DebugLog($"destroying active mount {characterMount.ActiveMount}");
                Mounts.MountManager.DestroyActiveMount(characterMount.Character);
                characterMount.SetActiveMount(null);
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during Mounts.DespawnMount: {e}");
            }
        }

        [Conditional("DEBUG")]
        public static void DebugLog(string message)
        {
            Log.LogMessage(message);
        }

        [Conditional("TRACE")]
        public static void DebugTrace(string message)
        {
            Log.LogMessage(message);
        }

        private void SL_OnSLPacksLoaded()
        {
            SLPack = SL.GetSLPack(ID); // defined via SideLoader/manifest.txt
            DebugTrace($"SLPack {SLPack.Name}:");
            // Cache skills
            var skills = SLPack.GetContentOfType<SL_Skill>();
            DebugTrace("Skills:");
            foreach (var skill in skills)
            {
                DebugTrace($"- {skill}");
                Skills.Add(skill.Value.New_ItemID, skill.Value);
            }
            DebugTrace("AssetBundles:");
            foreach (var pack in SLPack.AssetBundles)
                DebugTrace($"- {pack}");
            MountManager = new MountManager(Directory.GetParent(SLPack.FolderPath).FullName); // mountspecies are saved next to the sideloader folder, not inside  
        }

        // Delete mounts when going into the menu
        private void SceneManager_SceneLoaded(Scene Scene, LoadSceneMode LoadMode)
        {
            if (Scene.name == "MainMenu_Empty")
            {
                MountManager.DestroyAllMountInstances();
            }
        }

        private void SetupNPCs()
        {
            SetupLevantNPC();
            SetupBergNPC();
            SetupCierzoNPC();
            SetupMonsoonNPC();
        }

        private void SetupLevantNPC()
        {
            ///levant
            DialogueCharacter levantGuard = new()
            {
                UID = "emomount.mountcharacterlevant",
                Name = "Ianis, Levant Stable Master",
                SpawnSceneBuildName = "Levant",
                SpawnPosition = new(-39.7222f, 0.2239f, 120.0354f),
                SpawnRotation = new(0, 218f, 0),
                HelmetID = 3100091,
                ChestID = 3100090,
                BootsID = 3100092,
                WeaponID = 2100030,
                StartingPose = Character.SpellCastType.IdleAlternate,
            };


            // Create and apply the template
            var template = levantGuard.CreateAndApplyTemplate();

            // Add a listener to set up our dialogue
            levantGuard.OnSetupDialogueGraph += TestCharacter_OnSetupDialogueGraph;

            // Add this func to determine if our character should actually spawn
            template.ShouldSpawn = () => true;
        }
        private void SetupBergNPC()
        {
            ///berg
            DialogueCharacter BergNPC = new()
            {
                UID = "emomount.mountcharacterberg",
                Name = "Iggy the Wild, Berg Stable Master",
                SpawnSceneBuildName = "Berg",
                SpawnPosition = new(1191.945f, -13.7222f, 1383.581f),
                SpawnRotation = new(0, 72f, 0),
                HelmetID = 3100091,
                ChestID = 3100090,
                BootsID = 3100092,
                WeaponID = 2100030,
                StartingPose = Character.SpellCastType.IdleAlternate,
            };


            // Create and apply the template
            var bergTemplate = BergNPC.CreateAndApplyTemplate();
            // Add a listener to set up our dialogue
            BergNPC.OnSetupDialogueGraph += TestCharacter_OnSetupDialogueGraph;

            // Add this func to determine if our character should actually spawn
            bergTemplate.ShouldSpawn = () => true;
        }
        private void SetupCierzoNPC()
        {
            ///ciezro
            DialogueCharacter CierzoNPC = new()
            {
                UID = "emomount.mountcharactercierzo",
                Name = "Emo, Cierzo Stable Master",
                SpawnSceneBuildName = "CierzoNewTerrain",
                SpawnPosition = new(1421.29f, 5.5604f, 1686.195f),
                SpawnRotation = new(0, 270f, 0),
                HelmetID = 3100091,
                ChestID = 3100090,
                BootsID = 3100092,
                WeaponID = 2100030,
                StartingPose = Character.SpellCastType.IdleAlternate,
            };


            // Create and apply the template
            var cierzotemplate = CierzoNPC.CreateAndApplyTemplate();

            // Add a listener to set up our dialogue
            CierzoNPC.OnSetupDialogueGraph += TestCharacter_OnSetupDialogueGraph;

            // cierzotemplate this func to determine if our character should actually spawn
            cierzotemplate.ShouldSpawn = () => true;
        }
        private void SetupMonsoonNPC()
        {

            ///monsoon
            DialogueCharacter MonsoonNPC = new()
            {
                UID = "emomount.mountcharactermonsoon",
                Name = "Faeryn, Monsoon Stable Master",
                SpawnSceneBuildName = "Monsoon",
                SpawnPosition = new(82.0109f, -5.1698f, 140.1947f),
                SpawnRotation = new(0, 254.089f, 0),
                HelmetID = 3100091,
                ChestID = 3100090,
                BootsID = 3100092,
                WeaponID = 2100030,
                CharVisualData =
                {
                    Gender =  Character.Gender.Female
                },
                StartingPose = Character.SpellCastType.IdleAlternate,
            };


            // Create and apply the template
            var monsoontemplate = MonsoonNPC.CreateAndApplyTemplate();

            // Add a listener to set up our dialogue
            MonsoonNPC.OnSetupDialogueGraph += TestCharacter_OnSetupDialogueGraph;

            // cierzotemplate this func to determine if our character should actually spawn
            monsoontemplate.ShouldSpawn = () => true;
        }

        private void TestCharacter_OnSetupDialogueGraph(DialogueTree graph, Character character)
        {
            BuildDialouge(graph, character);
        }

        private void BuildDialouge(DialogueTree graph, Character character)
        {
            var ourActor = graph.actorParameters[0];

            // Add our root statement
            var InitialStatement = graph.AddNode<StatementNodeExt>();
            InitialStatement.statement = new($"Welcome, can I talk to you about mounts?");
            InitialStatement.SetActorName(ourActor.name);

            // Add a multiple choice
            var multiChoice1 = graph.AddNode<MultipleChoiceNodeExt>();

            MultipleChoiceNodeExt.Choice LearnSkillsChoice = new()
            {
                statement = new Statement("Can you teach me how to handle mounts?")
            };

            MultipleChoiceNodeExt.Choice ExitChoice = new()
            {
                statement = new Statement("No, thanks.")
            };


            multiChoice1.availableChoices.Add(LearnSkillsChoice);
            multiChoice1.availableChoices.Add(ExitChoice);

            // Add our answers
            var exitAnswer = graph.AddNode<StatementNodeExt>();
            exitAnswer.statement = new("Take care of your mounts.");
            exitAnswer.SetActorName(ourActor.name);

            var learnAnswer = graph.AddNode<StatementNodeExt>();
            learnAnswer.statement = new("So here's how you do it...");
            learnAnswer.SetActorName(ourActor.name);

            LearnMountSkillsNode learnMountSkillsNode = new();


            // ===== finalize nodes =====
            graph.allNodes.Clear();


            // add ALLL the nodes we want to use, remember this is a literal graph, the nodes must be on the graph to draw connections between them
            graph.allNodes.Add(InitialStatement);
            graph.primeNode = InitialStatement;
            graph.allNodes.Add(multiChoice1);
            graph.allNodes.Add(exitAnswer);
            graph.allNodes.Add(learnAnswer);
            graph.allNodes.Add(learnMountSkillsNode);



            // setup our connections
            graph.ConnectNodes(InitialStatement, multiChoice1);    // Connect Initial node to MultiChoice node

            graph.ConnectNodes(multiChoice1, learnAnswer, 0);
            graph.ConnectNodes(learnAnswer, learnMountSkillsNode);
            graph.ConnectNodes(learnAnswer, InitialStatement);

            graph.ConnectNodes(multiChoice1, exitAnswer, 1); // unconnected node finishes dialogue
        }
    }
}
