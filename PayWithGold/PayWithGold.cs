using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace PayWithGold
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class PayWithGold : BaseUnityPlugin
    {
        // Choose a GUID for your project. Change "myname" and "mymod".
        public const string ID = "com.exp111.PayWithGold";
        // Choose a NAME for your project, generally the same as your Assembly Name.
        public const string NAME = "PayWithGold";
        // Increment the VERSION when you release a new version of your mod.
        public const string VERSION = "1.0.0";

        // For accessing your BepInEx Logger from outside of this class (MyMod.Log)
        static ManualLogSource Log;

        // Awake is called when your plugin is created. Use this to set up your mod.
        void Awake()
        {
            try
            {
                Log = this.Logger;
                Log.LogMessage("Awake");

                // Harmony is for patching methods. If you're not patching anything, you can comment-out or delete this line.
                var harmony = new Harmony(ID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Log.LogMessage($"Exception during PayWithGold.Awake: {ex}");
            }
        }

        // This is an example of a Harmony patch.
        // If you're not using this, you should delete it.
        /*
        [HarmonyPatch(typeof(TrainerPanel), nameof(TrainerPanel.CheckHasEnoughCurrency))]
        public class TrainerPanelCheckCurrencyPatch
        {
            static void Postfix(TrainerPanel __instance, ref bool __result, SkillSlot _slot, out string _outMessage)
            {
                bool result;
                if (!_slot.UseAlternateCurrency)
                {
                    result = (_slot.RequiredMoney <= __instance.LocalCharacter.Inventory.AvailableMoney);
                    _outMessage = LocalizationManager.Instance.GetLoc("Notification_Trainer_NotEnoughMoney");
                }
                else
                {
                    result = (__instance.LocalCharacter.Inventory.ItemCount(_slot.AlternateCurrency) >= _slot.RequiredMoney);
                    _outMessage = LocalizationManager.Instance.GetLoc("Notification_Trainer_NotEnoughItem", new string[]
                    {
                LocalizationManager.Instance.GetItemName(_slot.AlternateCurrency)
                    });
                }
                __result = result;
            }
        }
    }*/

        [HarmonyPatch(typeof(ItemContainer), nameof(ItemContainer.ContainedGold))]
        public class ItemContainer_ContainedGold_Patch
        {
            static void Postfix(ItemContainer __instance, ref int __result)
            {
                __result = __instance.ItemStackCount(Currency.GoldItemID);
            }
        }
    }
}
