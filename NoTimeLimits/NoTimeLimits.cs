using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using NodeCanvas.Tasks.Actions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NoTimeLimits
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class NoTimeLimits : BaseUnityPlugin
    {
        public const string ID = "com.exp111.NoTimeLimits";
        public const string NAME = "NoTimeLimits";
        public const string VERSION = "1.0";

        public static ManualLogSource Log;
        private static Harmony harmony;

        // Expire
        public static ConfigEntry<bool> DontExpireMainQuestTimers;
        public static ConfigEntry<bool> DontExpireParallelQuestTimers;
        public static ConfigEntry<bool> DontExpireMinorQuestTimers;

        // Skips
        public static ConfigEntry<int> ChangeBlacksmithTimers;
        public static ConfigEntry<int> ChangeBetweenQuestTimers;
        //TODO: caldera timers?
        //TODO: SkipInQuestTimers like "MouthFeed_ResearchTimer" or "WhispBones_GabAwayTime"
        //TODO: SkipMinor/RepeatableQuestTimers like "SideQuests_SmugglerTimerWait" or "Vendavel_CookJobDone"
        public static ConfigEntry<int> ChangeBuildingTimers;

        public void Awake()
        {
            try
            {
                Log = Logger;
                Log.LogMessage("Awake");
                DebugLog("Using a DEBUG build.");

                SetupConfig();

                harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ID);
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during NoTimeLimits.Awake: {e}");
            }
        }

        private void SetupConfig()
        {
            DontExpireMainQuestTimers = Config.Bind("Expire Timers", "Main Quest Timers", true, "Don't let main quest timers expire.");
            DontExpireParallelQuestTimers = Config.Bind("Expire Timers", "Parallel Quest Timers", true, "Don't let parallel quest timers expire. They still fail if you reach the 3rd faction quest.");
            DontExpireMinorQuestTimers = Config.Bind("Expire Timers", "Minor Quest Timers", true, "Don't let minor quest timers expire.");

            var suffix = "(-1 to use original, 0 to skip timer)";
            var changeTimersHours = "Change Timers (in hours)";
            ChangeBlacksmithTimers = Config.Bind(changeTimersHours, "Blacksmith Timers", -1,
                new ConfigDescription($"Set Blacksmith crafting timers {suffix}.", new AcceptableValueRange<int>(-1, 24)));
            ChangeBetweenQuestTimers = Config.Bind(changeTimersHours, "Between Quest Timers", -1,
                new ConfigDescription($"Set timers that happen between main quests {suffix}.", new AcceptableValueRange<int>(-1, 72)));

            var changeTimersDays = "Change Timers (in days)";
            ChangeBuildingTimers = Config.Bind(changeTimersDays, "Building Timers", -1,
                new ConfigDescription($"Set Building construction timers (per construction phase). Construction starts after 1 day {suffix}.", new AcceptableValueRange<int>(-1, 5)));
        }

        public void OnDestroy()
        {
            harmony?.UnpatchSelf();
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

#if DEBUG
    [HarmonyDebug]
#endif
    [HarmonyPatch(typeof(QuestEventManager), nameof(QuestEventManager.CheckEventExpire))]
    public class QuestEventManager_CheckEventExpire
    {
        //TODO: make into <string, Func<bool>>? so you could do {"Main_Quest_Event", () => DontExpireMainQuestEvents.Value }
        static readonly Dictionary<string, Func<bool>> ShouldntExpire = new()
        {
            // Main Quests
            { "CallToAdventure_Expired", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Call To Adventure
            //// Blue Chamber
            { "MixedLegacies_Expired", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Mixed Legacies
            { "AshGiants_CyrCountdown", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Ash Giants
            { "WhispBones_StartTimer", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Whispering Bones
            //// Heroic Kingdom
            { "MouthFeed_QuestTimer", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Mouths to feed
            { "HeroPeace_CyreneTimer", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Heroic Peacemaker
            { "HeroPeace_TimerA", () => {NoTimeLimits.DebugLog("HeroPeace_TimerA"); return NoTimeLimits.DontExpireMainQuestTimers.Value; } },
            { "HeroPeace_TimerB", () => {NoTimeLimits.DebugLog("HeroPeace_TimerB"); return NoTimeLimits.DontExpireMainQuestTimers.Value; } },
            //// Holy Mission
            { "Questions_Timer", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Questions and Corruption
            { "Doubts_Timer", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Doubts and Secrets
            { "Truth_Timer", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Truth and Purpose
            { "HallowPeace_TimerA", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Hallowed Peacemaker
            { "HallowPeace_TimerB", () => NoTimeLimits.DontExpireMainQuestTimers.Value },
            //// Sorobean
            // TODO: havent found any timers for "Up The Ladder" and "A Knife in the Back" even though the wiki says they are "likely timed" wtf does this mean
            { "SA_MissionStart_Q3", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Cloak and Dagger
            { "SA_SabotageMercA", () => {NoTimeLimits.DebugLog("SA_SabotageMercA"); return NoTimeLimits.DontExpireMainQuestTimers.Value; } }, //TODO: check what those are
            { "SA_SabotageMercB", () => {NoTimeLimits.DebugLog("SA_SabotageMercB"); return NoTimeLimits.DontExpireMainQuestTimers.Value; } },
            { "SA_Arcane_TaskHasBegun_Q4", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // A House Divided
            //// Three Brothers
            { "CA_Q1_Timer_Start", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // From the Ashes
            { "CA_Q2_Timer_Start", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Stealing Fire
            { "CA_Q3_Timer_Start", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Liberate the Sun
            { "CA_Q4_Timer_Start", () => NoTimeLimits.DontExpireMainQuestTimers.Value }, // Vengeful Ouroboros
            // Parallel Quests
            { "SA_TimerEnds_Parallel", () => NoTimeLimits.DontExpireParallelQuestTimers.Value }, // Rust and Vengeance
            { "Purifier_Timer", () => NoTimeLimits.DontExpireParallelQuestTimers.Value }, // Purifier
            { "Vendavel_QuestTimer", () => NoTimeLimits.DontExpireParallelQuestTimers.Value }, // Vendavel
            { "Fraticide_Timer", () => NoTimeLimits.DontExpireParallelQuestTimers.Value }, // Blood under the Sun
            // Minor Quests
            { "SideQuests_SmugglerTimer", () => NoTimeLimits.DontExpireMinorQuestTimers.Value }, // Lost Merchant
            //TODO: SideQuests_LetResearcherDied? SideQuests_FoodStoreMonsoonTimer?
        };
        static readonly Dictionary<string, Func<int>> ChangeTime = new()
        {
            // Crafting
            {"Crafting_BergBlacksmithTimer", () => NoTimeLimits.ChangeBlacksmithTimers.Value },
            {"Crafting_CierzoBlacksmithTimer", () => NoTimeLimits.ChangeBlacksmithTimers.Value },
            {"Crafting_HarmattanBlacksmithTimer", () => NoTimeLimits.ChangeBlacksmithTimers.Value },
            {"Crafting_LevantBlacksmithTimer", () => NoTimeLimits.ChangeBlacksmithTimers.Value },
            {"Crafting_MonsoonBlacksmithTimer", () => NoTimeLimits.ChangeBlacksmithTimers.Value },
            // Quest Timers
            {"General_DoneQuest0", () => NoTimeLimits.ChangeBetweenQuestTimers.Value },
            {"General_DoneQuest1", () => NoTimeLimits.ChangeBetweenQuestTimers.Value },
            {"General_DoneQuest2", () => NoTimeLimits.ChangeBetweenQuestTimers.Value },
            {"General_DoneQuest3", () => NoTimeLimits.ChangeBetweenQuestTimers.Value },
            {"General_DoneQuest4", () => NoTimeLimits.ChangeBetweenQuestTimers.Value },
        };

        static bool Prefix(QuestEventManager __instance, string _eventUID, ref int _gameHourAllowed, ref bool __result)
        {
            try
            {
                //NoTimeLimits.DebugTrace($"QuestEventManager.CheckEventExpire checking {_eventUID} with time limit {_gameHourAllowed} hours.");
                if (QuestEventManager.m_questEvents.TryGetValue(_eventUID, out var eventData))
                {
                    //NoTimeLimits.DebugLog($"Event: {eventData}, Name {eventData.Name}, Age {eventData.Age}");
                    if (ShouldntExpire.TryGetValue(eventData.Name, out var shouldntExpire))
                    {
                        if (!shouldntExpire())
                            return true; // run original

                        //NoTimeLimits.DebugTrace($"Forcing {eventData.Name} not to be expired");
                        __result = false;
                        return false; // skip
                    }

                    if (ChangeTime.TryGetValue(eventData.Name, out var shouldSkip))
                    {
                        var time = shouldSkip();
                        if (time == -1)
                            return true; // run original

                        if (time > 0)
                        {
                            _gameHourAllowed = time;
                            return true; // run original
                        }

                        //NoTimeLimits.DebugTrace($"Forcing {eventData.Name} to be expired");
                        __result = true;
                        return false; // skip
                    }
                }
            }
            catch (Exception e)
            {
                NoTimeLimits.Log.LogMessage($"Exception during QuestEventManager.CheckEventExpire prefix: {e}");
            }
            return true; // dont skip
        }
    }

#if DEBUG
    [HarmonyDebug]
#endif
    [HarmonyPatch(typeof(Building.ConstructionPhase), nameof(Building.ConstructionPhase.GetConstructionTime))]
    public class ConstructionPhase_GetConstructionTime
    {
        [HarmonyPrefix]
        static void Prefix(Building.ConstructionPhase __instance)
        {
            if (NoTimeLimits.ChangeBuildingTimers.Value != -1)
                __instance.m_constructionTime = NoTimeLimits.ChangeBuildingTimers.Value;
        }
    }
}
