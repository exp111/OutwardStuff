using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace FailedRecipes
{
    /// <summary>
    /// Failed Recipes Do Not Consume Ingredients
    /// </summary>
	[BepInPlugin(ID, NAME, VERSION)]
    public class FailedRecipes : BaseUnityPlugin
    {
		const string ID      = "com.outward.urbanvibes.failedrecipes";
        const string NAME    = "FailedRecipes";
        const string VERSION = "1.5";


        // Consume Items when crafting fails: enable for Alchemy / Cooking
        public static ConfigEntry<bool> bDontConsumeItemsFailedAlchemy;
        public static ConfigEntry<bool> bDontConsumeItemsFailedCooking;
        private static MethodInfo methodTryCraft = null;

         /// <summary>
        /// Set up Mod Configuration
        /// </summary>
        void SetupConfig()
		{
            bDontConsumeItemsFailedAlchemy = Config.Bind("General", "bDontConsumeItemsFailedAlchemy", true, "Don't consume Ingredients on fail for Alchemy");
            bDontConsumeItemsFailedCooking = Config.Bind("General", "bDontConsumeItemsFailedCooking", true, "Don't consume Ingredients on fail for Cooking");
		}


        /// <summary>
        /// Initialization
        /// </summary>
        private void Awake()
        {
            // Initialize Settings
            SetupConfig();

            // Initialize Methods
            methodTryCraft = typeof(CraftingMenu).GetMethod("TryCraft", AccessTools.all);

            // Let Harmony Patch Outward's Behavior
            var harmony = new HarmonyLib.Harmony(ID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }


        /// <summary>
        /// For some reason, the Crafting button does not work anymore after a failed attempt.Therefore, listen to the
        /// crafting button in the Update method as well
        /// </summary>
        [HarmonyPatch(typeof(CraftingMenu), "Update")]
        class CraftingMenu_Update
        {
            [HarmonyPostfix]
            public static void Patch(CraftingMenu __instance)
            {
                // Repeat crafting after a failed attempt, when the T-button is pressed
                if (Input.GetKeyDown(KeyCode.T))
                { methodTryCraft.Invoke(__instance, new object[0]); }

                // Repeat crafting after a failed attempt, when the T-button is pressed
                if (Input.GetKeyDown(KeyCode.Escape))
                { __instance.gameObject.SetActive(false); }
            }
        }

        /// <summary>
        /// For some reason, the Crafting button does not work anymore after a failed attempt. Therefore, listen to the
        /// crafting button in the Update method as well
        /// </summary>
        [HarmonyPatch(typeof(CraftingMenu), "OnInfoInput")]
        class CraftingMenu_OnInfoInput
        {
            [HarmonyPostfix]
            public static void Patch(CraftingMenu __instance)
            { methodTryCraft.Invoke(__instance, new object[0]); }
        }

        /// <summary>
        /// Overwrite the crafting process
        /// </summary>
        [HarmonyPatch(typeof(CraftingMenu), "CraftingDone")]
        class CraftingMenu_CraftingDone
        {
            [HarmonyPrefix]
            public static bool Patch(
                CraftingMenu                     __instance,
                float                            ___m_craftingTimer,
                int                              ___m_lastRecipeIndex,
                int                              ___m_lastFreeRecipeIndex,
                List<KeyValuePair<int, Recipe>>  ___m_complexeRecipes,
                List<KeyValuePair<string, int>>  ___reducedItems,
                List<int>                        ___usedIngredients,
                Recipe.CraftingType              ___m_craftingStationType,
                IngredientSelector[]             ___m_ingredientSelectors,
                int[]                            ___m_lastFreeRecipeIngredientIDs,
                List<Recipe>                     ___m_allRecipes,
                bool                             ___m_refreshComplexeRecipeRequired,
                bool                             ___m_simpleMode,
                GameObject                       ___m_craftProgressPanel,
                Slider                           ___m_sldCraftProgress,
                IList<KeyValuePair<string, int>> ___lastConsumedIngredients
                )
            {
                // This is all original code, except for the commented lines below
                ___m_craftingTimer = -999f;
                int num = (___m_lastRecipeIndex != -1) ? ___m_complexeRecipes[___m_lastRecipeIndex].Key : ___m_lastFreeRecipeIndex;
                bool useMultipler = false;
                int resultMultiplier = 1;
                int num2 = 0;
                if (num != -1 || ___m_craftingStationType != Recipe.CraftingType.Survival)
                {
                    ___reducedItems.Clear();
                    ___usedIngredients.Clear();
                    if (num != -1 && ___m_allRecipes[num].IngredientCount == 1 && ResourcesPrefabManager.Instance.IsWaterItem(___m_allRecipes[num].Results[0].RefItem.ItemID))
                    { useMultipler = true; }

                    for (int i = 0; i < ___m_ingredientSelectors.Length; i++)
                    {
                        if (___m_ingredientSelectors[i].AssignedIngredient != null)
                        {
                            num2++;
                            CompatibleIngredient assignedIngredient = ___m_ingredientSelectors[i].AssignedIngredient;
                            if (!___usedIngredients.Contains(assignedIngredient.ItemID))
                            {
                                ___usedIngredients.Add(assignedIngredient.ItemID);
                                ___lastConsumedIngredients = assignedIngredient.GetConsumedItems(useMultipler, out resultMultiplier);

						        if (___lastConsumedIngredients.Count > 0)
                                { ___reducedItems.AddRange(___lastConsumedIngredients); }
                            }

                            // If crafting succeeded, or the consumption is enabled for Alchemy or Cooking, then consume ingredients
                            if ((num == -1)
                            && ((___m_craftingStationType == Recipe.CraftingType.Alchemy) && (!bDontConsumeItemsFailedAlchemy.Value))
                            && ((___m_craftingStationType == Recipe.CraftingType.Cooking) && (!bDontConsumeItemsFailedCooking.Value)))
                            {
                                ___m_ingredientSelectors[i].Free(false);
                            }
                            ___m_lastFreeRecipeIngredientIDs[i] = ((assignedIngredient.AvailableQty > 0) ? assignedIngredient.ItemID : -1);
                        }
                    }

                    // If crafting succeeded, or the consumption is enabled for Alchemy or Cooking, then consume ingredients
                    if ((num != -1)
                    || ((___m_craftingStationType == Recipe.CraftingType.Alchemy) && (!bDontConsumeItemsFailedAlchemy.Value))
                    || ((___m_craftingStationType == Recipe.CraftingType.Cooking) && (!bDontConsumeItemsFailedCooking.Value)))
                    {
                        // Consume items when crafting succeeds
                        ItemManager.Instance.ConsumeCraftingItems(null, ___reducedItems.ToArray());
                    }
                }
                if (num != -1)
                {
                    for (int j = 0; j < ___m_allRecipes[num].Results.Length; j++)
                    {
                        //this.GenerateResult(___m_allRecipes[num].Results[j], resultMultiplier);
                        MethodInfo methodGenerateResult = typeof(CraftingMenu).GetMethod("GenerateResult", AccessTools.all);
                        methodGenerateResult.Invoke(__instance, new object[2] { ___m_allRecipes[num].Results[j], resultMultiplier });

                        for (int k = 0; k < AchievementManager.ItemCraftingAchievement.Length; k++)
                        {
                            if (AchievementManager.ItemCraftingAchievement[k].Key == ___m_allRecipes[num].Results[j].RefItem.ItemID)
                            { AchievementManager.Instance.SetAchievementAsCompleted(AchievementManager.ItemCraftingAchievement[k].Value); }
                        }

                        int num3 = AchievementManager.SlayerSetAchievementItemIDs.IndexOf(___m_allRecipes[num].Results[j].ItemID);
                        if (num3 != -1)
				        { __instance.LocalCharacter.UpdateSlayerArmorAchievement(num3); }

                        if (!__instance.LocalCharacter.Inventory.RecipeKnowledge.IsRecipeLearned(___m_allRecipes[num].UID))
                        {
                            ___m_refreshComplexeRecipeRequired = true;
                            __instance.LocalCharacter.Inventory.RecipeKnowledge.LearnRecipe(___m_allRecipes[num]);
                        }
                        if (num2 == 4)
                        {
                            if (___m_craftingStationType == Recipe.CraftingType.Cooking)
                            { AchievementManager.Instance.SetAchievementAsCompleted(AchievementManager.Achievement.CordonBleu_28); }
                            else if (___m_craftingStationType == Recipe.CraftingType.Alchemy)
                            { AchievementManager.Instance.SetAchievementAsCompleted(AchievementManager.Achievement.ScienceTroglodyte_29); }
                        }
                        GlobalAudioManager.Sounds sounds = GlobalAudioManager.Sounds.NONE;
                        if (___m_craftingStationType == Recipe.CraftingType.Survival)
                        { sounds = GlobalAudioManager.Sounds.UI_CRAFTING_Survival; }
                        else if (___m_craftingStationType == Recipe.CraftingType.Cooking)
                        {
                            if (___m_simpleMode)
                            { sounds = GlobalAudioManager.Sounds.UI_CRAFTING_Campfire; }
                            else
                            { sounds = GlobalAudioManager.Sounds.UI_CRAFTING_CookingPot; }
                        }
                        else if (___m_craftingStationType == Recipe.CraftingType.Alchemy)
                        { sounds = GlobalAudioManager.Sounds.UI_CRAFTING_Alchemy; }
                        if (sounds != GlobalAudioManager.Sounds.NONE)
                        { Global.AudioManager.PlaySound(sounds, 0f, 1f, 1f, 1f, 1f); }
                    }
                }
                else
                {
                    // Do not spawn failed recipes if this mod is active
                    Item failedRecipeResult = null;
                    if (((___m_craftingStationType == Recipe.CraftingType.Alchemy) && (!bDontConsumeItemsFailedAlchemy.Value))
                    || ((___m_craftingStationType == Recipe.CraftingType.Cooking) && (!bDontConsumeItemsFailedCooking.Value)))
                    { failedRecipeResult = ItemManager.Instance.GetFailedRecipeResult(___m_craftingStationType); }

                    if ((failedRecipeResult)
                    && ((___m_craftingStationType != Recipe.CraftingType.Alchemy)
                    || (___m_craftingStationType != Recipe.CraftingType.Cooking)))
                    {
                        Item item = ItemManager.Instance.GenerateItemNetwork(failedRecipeResult.ItemID);
                        __instance.LocalCharacter.Inventory.TakeItem(item, false);
                        string loc = LocalizationManager.Instance.GetLoc("Notification_Item_Received", new string[]
                        {
                            "1",
                            item.Name
                        });
                        __instance.LocalCharacter.CharacterUI.ShowInfoNotification(loc, item);
                    }

                    else if (___m_craftingStationType == Recipe.CraftingType.Survival)
                    { __instance.LocalCharacter.CharacterUI.ShowInfoNotificationLoc("Notification_Crafting_InvalidCombination"); }
                    else
                    { __instance.LocalCharacter.CharacterUI.ShowInfoNotificationLoc("Notification_Crafting_InvalidCombination"); }
                }
                if (___m_craftProgressPanel && ___m_craftProgressPanel.gameObject.activeSelf)
                { ___m_craftProgressPanel.gameObject.SetActive(false); }
                ___m_sldCraftProgress.normalizedValue = 0f;
                return false;
            }
        }

    }
}
