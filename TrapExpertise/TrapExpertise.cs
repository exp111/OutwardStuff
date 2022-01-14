using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TrapExpertise
{
    [BepInPlugin(ID, NAME, VERSION)]
    public class TrapExpertise : BaseUnityPlugin
    {
        // Choose a GUID for your project. Change "myname" and "mymod".
        public const string ID = "com.exp111.TrapExpertise";
        // Choose a NAME for your project, generally the same as your Assembly Name.
        public const string NAME = "TrapExpertise";
        // Increment the VERSION when you release a new version of your mod.
        public const string VERSION = "1.0.0";

        public static ManualLogSource Log;

        void Awake()
        {
            try
            {
                Log = Logger;
                Log.LogMessage("Awake");

                // Setup Config

                // Harmony is for patching methods. If you're not patching anything, you can comment-out or delete this line.
                var harmony = new Harmony(ID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Log.LogMessage($"Exception during TrapExpertise.Awake: {ex}");
            }
        }

        [HarmonyPatch(typeof(DeployableTrap), nameof(DeployableTrap.CleanUp))]
        public static class DeployableTrap_CleanUp_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                /*
                 * Remove CurrentTrapType Check from func
                if (!this.m_hasHiddenEffects || this.CurrentTrapType != DeployableTrap.TrapType.PressurePlateTrap)
			    {
				    if (!PhotonNetwork.isNonMasterClientInRoom)
				    {
					    ItemManager.Instance.DestroyItem(this.UID);
					    return;
				    }
			    }
                => //TODO: maybe rather change to a "|| this.CurrentTrapType == DeployableTrap.TrapType.Runic" to make conflicts more unlikely (with other mods)?
                if (!this.m_hasHiddenEffects)
			    {
                ...
                }
                */
                var found = false;
                var startIndex = -1;
                var endIndex = -1;

                var codes = new List<CodeInstruction>(instructions);
                var fieldInfo = AccessTools.Field(typeof(DeployableTrap), nameof(DeployableTrap.m_hasHiddenEffects));
                var callInfo = AccessTools.PropertyGetter(typeof(DeployableTrap), nameof(DeployableTrap.CurrentTrapType));
                Label? jumpTarget = null;
                for (var i = 0; i < codes.Count; i++)
                {
                    // First find the start // if (!this.m_hasHiddenEffects ...
                    if (!found && codes[i].opcode == OpCodes.Ldfld)
                    {
                        if (codes[i].LoadsField(fieldInfo))
                        {
                            found = true;
                            startIndex = i + 2; // jump over cur and next (which is a jump)
                            continue;
                        }
                    }
                    // Then look for the end // this.CurrentTrapType != DeployableTrap.TrapType.PressurePlateTrap)
                    if (found && codes[i].opcode == OpCodes.Call)
                    {
                        if (i < startIndex)
                            continue;

                        if (!codes[i].Calls(callInfo))
                            continue;

                        endIndex = i + 2; // plus the next two (cmp target and the cmp jmp itself)
                        // copy the jmp target from the jmp
                        jumpTarget = (Label) codes[endIndex].operand;
                        // No need to look further, go out
                        break;
                    }
                }

                // Then remove that shit
                if (startIndex > -1 && endIndex > -1)
                {
                    // we need to jump away from this shit
                    codes[endIndex] = new CodeInstruction(OpCodes.Br, jumpTarget);
                    codes.RemoveRange(startIndex, endIndex - startIndex); // this deletes everything but the last line
                }

                /*foreach (var code in codes)
                {
                    Log.LogMessage(code);
                }*/
                return codes.AsEnumerable();
            }
        }
    }
}
