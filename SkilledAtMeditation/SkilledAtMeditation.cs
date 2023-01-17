using BepInEx;
using BepInEx.Logging;
using SideLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static BlastDelayedHits;
using BepInEx.Configuration;

namespace SkilledAtMeditation
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class SkilledAtMeditation : BaseUnityPlugin
    {
        public const string ID = "com.exp111.SkilledAtMeditation";
        public const string NAME = "SkilledAtMeditation";
        public const string VERSION = "1.0";

        public static ManualLogSource Log;

        public const int MeditationSkillID = -46000; // https://github.com/Mefino/ModdingCommunityResources/blob/main/id-reservations/id-reservations.json
        public const int MeditationStatusID = -46001;
        public const string MeditationStatusIdentifier = nameof(Meditation);
        public Skill MeditationSkill;

        public static ConfigEntry<bool> EnableBurntRegen;
        public static ConfigEntry<bool> EnableActiveRegen;
        public static ConfigEntry<float> BurntStaminaRegen;
        public static ConfigEntry<float> BurntHealthRegen;
        public static ConfigEntry<float> BurntManaRegen;
        public static ConfigEntry<float> ActiveStaminaRegen;
        public static ConfigEntry<float> ActiveHealthRegen;
        public static ConfigEntry<float> ActiveManaRegen;

        public void Awake()
        {
            try
            {
                Log = Logger;
                Log.LogMessage("Awake");
                DebugLog("Using a DEBUG build.");

                CreateConfig();
                CreateSkill();
                CreateStatusEffect();
                if (SL.PacksLoaded)
                    OnPackLoaded();
                SL.OnPacksLoaded += OnPackLoaded;
                SL.OnSceneLoaded += OnSceneLoaded;
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during SkilledAtMeditation.Awake: {e}");
            }
        }

        private void CreateConfig()
        {
            EnableBurntRegen = Config.Bind("Burnt Stat Regen",
                             "Enable Burnt Stat Regeneration",
                             true,
                             "Enable or disable the regeneration of burnt stats while meditating");
            EnableActiveRegen = Config.Bind("Active Stat Regen",
                                     "Enable Active Stat Regeneration",
                                     true,
                                     "Enable or disable the regeneration of active (non-burnt) stats while meditating");
            BurntStaminaRegen = Config.Bind("Burnt Stat Regen",
                                     "Burnt Stamina Regeneration Rate",
                                     0.25f,
                                     "How quickly burnt stamina will regen while meditating.");
            BurntHealthRegen = Config.Bind("Burnt Stat Regen",
                                     "Burnt Health Regeneration Rate",
                                     0.25f,
                                     "How quickly burnt health will regen while meditating.");
            BurntManaRegen = Config.Bind("Burnt Stat Regen",
                                     "Burnt Mana Regeneration Rate",
                                     0.25f,
                                     "How quickly burnt Mana will regen while meditating.");
            ActiveStaminaRegen = Config.Bind("Active Stat Regen",
                                     "Active Stamina Regeneration",
                                     0.5f,
                                     "How quickly stamina will regen while meditating.");
            ActiveHealthRegen = Config.Bind("Active Stat Regen",
                                     "Active Health Regeneration",
                                     0.75f,
                                     "How quickly health will regen while meditating.");
            ActiveManaRegen = Config.Bind("Active Stat Regen",
                                     "Active Mana Regeneration",
                                     0.5f,
                                     "How quickly mana will regen while meditating.");
        }

        public void OnDestroy()
        {
            //harmony?.UnpatchSelf();
        }

        // Give our character the skill // TODO: find a better way (let npc sell it?)
        private void OnSceneLoaded()
        {
            try
            {
                foreach (Character chr in CharacterManager.Instance.Characters.Values)
                {
                    if (!chr.IsAI && chr.Inventory != null && !chr.Inventory.LearnedSkill(MeditationSkill))
                    {
                        chr.Inventory.ReceiveSkillReward(MeditationSkill.ItemID);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during SkilledAtMeditation.OnSceneLoaded: {e}");
            }
        }

        private void OnPackLoaded()
        {
            try
            {
                MeditationSkill = ResourcesPrefabManager.Instance.GetItemPrefab(MeditationSkillID) as Skill;
                MeditationSkill.IgnoreLearnNotification = true;
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during SkilledAtMeditation.OnPackLoaded: {e}");
            }
        }

        private void CreateStatusEffect()
        {
            var template = new SL_StatusEffect
            {
                TargetStatusIdentifier = "Burning",
                EffectBehaviour = EditBehaviours.DestroyEffects,
                StatusIdentifier = MeditationStatusIdentifier,
                NewStatusID = MeditationStatusID,
                Name = "Meditating",
                Description = "Slowly restore stats while meditating.",
                SLPackName = ID,
                SubfolderName = "Meditation",

                Lifespan = -1,
                RefreshRate = 1f, // Update once every second
                DisplayedInHUD = true,
                IsMalusEffect = false,
                Purgeable = false,
                VFXInstantiationType = StatusEffect.FXInstantiationTypes.None,
                VFXPrefab = null,
                Effects = new SL_EffectTransform[]
                {
                    new SL_EffectTransform()
                    {
                        TransformName = "Effects",
                        Effects = new SL_Effect[]
                        {
                            new SL_Meditation(),
                        }
                    }
                },
            };
            template.ApplyTemplate();
        }

        private void CreateSkill()
        {
            var item = new SL_Skill()
            {
                Name = "Meditate",
                EffectBehaviour = EditBehaviours.Destroy,
                Target_ItemID = 8100120, // PushKick
                New_ItemID = MeditationSkillID,
                SLPackName = ID,
                SubfolderName = "Meditate",
                Description = "Sit down to slowly restore stats.",

                CastType = Character.SpellCastType.Sit,
                CastModifier = Character.SpellCastModifier.Immobilized,
                CastLocomotionEnabled = true,
                MobileCastMovementMult = -1f,

                EffectTransforms = new SL_EffectTransform[]
                {
                    new SL_EffectTransform()
                    {
                        TransformName = "Effects",
                        Effects = new SL_Effect[]
                        {
                            new SL_AddStatusEffect()
                            {
                                StatusEffect = MeditationStatusIdentifier,
                                ChanceToContract = 100,
                                Delay = 1
                            },
                        }
                    }
                },
                Cooldown = 1,
                StaminaCost = 0,
                HealthCost = 0,
                ManaCost = 0,
            };
            item.ApplyTemplate();
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
    }
}
