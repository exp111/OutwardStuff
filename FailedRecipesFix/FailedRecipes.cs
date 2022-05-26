using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using BepInEx.Configuration;
using BepInEx.Logging;
using System.Reflection.Emit;

namespace FailedRecipes
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class FailedRecipes : BaseUnityPlugin
    {
        const string ID = "com.exp111.failedrecipes";
        const string NAME = "FailedRecipes";
        const string VERSION = "1.0.1";


        // Consume Items when crafting fails: enable for Alchemy / Cooking
        public static ConfigEntry<bool> bDontConsumeItemsFailedAlchemy;
        public static ConfigEntry<bool> bDontConsumeItemsFailedCooking;

        public static ManualLogSource Log;

        void SetupConfig()
        {
            bDontConsumeItemsFailedAlchemy = Config.Bind("General", "Enable for alchemy", true, "Don't consume ingredients on fail for Alchemy");
            bDontConsumeItemsFailedCooking = Config.Bind("General", "Enable for cooking", true, "Don't consume ingredients on fail for Cooking");
        }


        private void Awake()
        {
            try
            {
                Log = Logger;
                // Initialize Settings
                SetupConfig();

                var harmony = new Harmony(ID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Log.LogMessage($"Exception during FailedRecipes.Awake: {e}");
            }
        }


        [HarmonyPatch(typeof(CraftingMenu), nameof(CraftingMenu.CraftingDone))]
        class CraftingMenu_CraftingDone_Patch
        {
            //[HarmonyDebug]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                try
                {
                    var cur = new CodeMatcher(instructions);
                    /*
                    // Add config check to skip instead of destroying ingredients
                    if (num != -1 || this.m_currentlyDisplayedCraftingType != Recipe.CraftingType.Survival)
                    {
                    ...

                    IL_0041: ldarg.0
                    IL_0042: ldfld     valuetype Recipe/CraftingType CraftingMenu::m_currentlyDisplayedCraftingType
                    IL_0047: ldc.i4.2
                    IL_0048: beq       IL_0181

                    =>

                    if (num != -1 || shouldRunOriginalCode(this))
                    {

                    IL_0041: ldarg.0
                    IL_0042: call shouldRunOriginalCode
                    IL_0048: brfalse       IL_0181

                    */

                    var CraftingMenu_m_currentlyDisplayedCraftingType = AccessTools.Field(typeof(CraftingMenu), nameof(CraftingMenu.m_currentlyDisplayedCraftingType));

                    // first find the second part of the if check // || this.m_craftingStationType != Recipe.CraftingType.Survival)
                    cur.MatchForward(false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld, CraftingMenu_m_currentlyDisplayedCraftingType),
                        new CodeMatch(OpCodes.Ldc_I4_2)
                        );
                    var start = cur.Pos;
                    // store the label
                    var jumpTarget = (Label)cur.InstructionAt(3).operand;// on the branch
                    // remove the part
                    cur.RemoveInstructions(4); // including the current == 4

                    // insert our own check which jumps to the label if false
                    cur.Insert(
                        new CodeInstruction(OpCodes.Ldarg_0), // put "this" on the stack
                        Transpilers.EmitDelegate<Func<CraftingMenu, bool>>((menu) =>
                        {
                            if (menu.m_currentlyDisplayedCraftingType == Recipe.CraftingType.Alchemy && !bDontConsumeItemsFailedAlchemy.Value
                                || menu.m_currentlyDisplayedCraftingType == Recipe.CraftingType.Cooking && !bDontConsumeItemsFailedCooking.Value)
                            {
                                return true; // should run code
                            }
                            return false; // nope
                        }),
                        new CodeInstruction(OpCodes.Brfalse_S, jumpTarget) // pops return value from stack
                    );


                    /*
                    // Add config check to send notification and quit out instead of adding food waste
                    Item failedRecipeResult = ItemManager.Instance.GetFailedRecipeResult(__instance.m_currentlyDisplayedCraftingType);
                    if (failedRecipeResult)
                    {
                        Item item = ItemManager.Instance.GenerateItemNetwork(failedRecipeResult.ItemID);
                        this.LocalCharacter.Inventory.TakeItem(item, false);
                        string loc = LocalizationManager.Instance.GetLoc("Notification_Item_Received", new string[]
                        {
                            "1",
                            item.Name
                        });
                        this.LocalCharacter.CharacterUI.ShowInfoNotification(loc, item);
                    }
                    ...

                    IL_0359: br IL_0413
                    IL_035E: call      class ItemManager ItemManager::get_Instance() [LABEL]
                    IL_0363: ldarg.0
                    IL_0364: ldfld valuetype Recipe/CraftingType CraftingMenu::m_currentlyDisplayedCraftingType
                    =>
                        if (this.m_currentlyDisplayedCraftingType == Recipe.CraftingType.Alchemy && bDontConsumeItemsFailedAlchemy.Value
                                || this.m_currentlyDisplayedCraftingType == Recipe.CraftingType.Cooking && bDontConsumeItemsFailedCooking.Value)
                            {
                                this.LocalCharacter.CharacterUI.ShowInfoNotificationLoc("Notification_Crafting_InvalidCombination");
                            }
                        else
                        {
                            Item failedRecipeResult = ItemManager.Instance.GetFailedRecipeResult(__instance.m_currentlyDisplayedCraftingType);
                            if (failedRecipeResult)
                            ...
                        }

                    // we call a delegate here and just interpret the return value if we should skip/run the 
                    // else code (the original code in this case)
                    IL_0359: br IL_0413
                    IL_0XXX: ldarg.0 [LABEL]
                    IL_0XXX: call shouldSkipOriginalCode(this) [LABEL]
                    IL_0XXX  brtrue IL_0413
                    IL_035E: call      class ItemManager ItemManager::get_Instance()
                    IL_0363: ldarg.0
                    IL_0364: ldfld valuetype Recipe/CraftingType CraftingMenu::m_currentlyDisplayedCraftingType
                    */

                    var ItemManagerInstanceGet = AccessTools.PropertyGetter(typeof(ItemManager), nameof(ItemManager.Instance));
                    // First find the start // ItemManager.Instance.GetFailedRecipeResult(..)
                    cur.MatchForward(false,
                        new CodeMatch(OpCodes.Br),
                        new CodeMatch(OpCodes.Call, ItemManagerInstanceGet)
                        );
                    var jumpLabel = (Label)cur.Operand;

                    // Insert puts code before cur il, so we need to go one forward (now on the ItemManager.Instance)
                    cur.Advance(1);

                    // put "this" on the stack for the delegate call
                    var loadThis = new CodeInstruction(OpCodes.Ldarg_0);
                    // steal the labels cause we wanna jump here instead of the original destination
                    loadThis.MoveLabelsFrom(cur.Instruction);
                    cur.InsertAndAdvance(loadThis);
                    var ifCheck = Transpilers.EmitDelegate<Func<CraftingMenu, bool>>((menu) =>
                    {
                        if (menu.m_currentlyDisplayedCraftingType == Recipe.CraftingType.Alchemy && bDontConsumeItemsFailedAlchemy.Value
                                || menu.m_currentlyDisplayedCraftingType == Recipe.CraftingType.Cooking && bDontConsumeItemsFailedCooking.Value)
                        {
                            menu.LocalCharacter.CharacterUI.ShowInfoNotificationLoc("Notification_Crafting_InvalidCombination");
                            return true; // skip original code/"else"
                        }
                        return false;
                    });
                    cur.InsertAndAdvance(ifCheck,
                        new CodeInstruction(OpCodes.Brtrue_S, jumpLabel) // automatically pops return value
                        );

                    var e = cur.InstructionEnumeration();
                    /*foreach (var code in e)
                    {
                        Log.LogMessage(code);
                    }*/
                    return e;
                }
                catch (Exception e)
                {
                    Log.LogMessage($"Exception during FailedRecipes.CraftingMenu_CraftingDone: {e}");
                    return instructions;
                }
            }
        }

    }
}
