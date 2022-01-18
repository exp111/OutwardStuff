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

                var fieldInfo = AccessTools.Field(typeof(DeployableTrap), nameof(DeployableTrap.m_hasHiddenEffects));
                var callInfo = AccessTools.PropertyGetter(typeof(DeployableTrap), nameof(DeployableTrap.CurrentTrapType));

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
                IL_0018: ldarg.0
		        IL_0019: ldfld     bool DeployableTrap::m_hasHiddenEffects
		        IL_001E: brfalse.s IL_0029
		        IL_0020: ldarg.0
		        IL_0021: call      instance valuetype DeployableTrap/TrapType DeployableTrap::get_CurrentTrapType()
		        IL_0026: ldc.i4.1
		        IL_0027: beq.s     IL_0041

                => //TODO: maybe rather change to a "|| this.CurrentTrapType == DeployableTrap.TrapType.Runic" to make conflicts more unlikely (with other mods)?
                if (!this.m_hasHiddenEffects)
			    {
                ...
                }

                IL_0018: ldarg.0
		        IL_0019: ldfld     bool DeployableTrap::m_hasHiddenEffects
		        IL_001E: brfalse.s IL_0029
                IL_0020: br IL_0041
                */

                var cur = new CodeMatcher(instructions);
                // First find the start // if (!this.m_hasHiddenEffects ...
                cur.MatchForward(false,
                    new CodeMatch(OpCodes.Ldfld, fieldInfo))
                .Advance(2); // jump over cur and next (which is a jump)

                var start = cur.Pos;

                // Then look for the end // this.CurrentTrapType != DeployableTrap.TrapType.PressurePlateTrap)
                cur.MatchForward(false,
                    new CodeMatch(OpCodes.Call, callInfo))
                .Advance(2);

                var end = cur.Pos;
                // copy the target for later (as we need to jump out)
                var jumpTarget = (Label)cur.Operand;

                // we need to jump away from this shit
                cur.SetInstruction(new CodeInstruction(OpCodes.Br, jumpTarget));
                // Then remove that shit (except the jump instruction at the end)
                cur.RemoveInstructionsInRange(start, end - 1);

                var e = cur.InstructionEnumeration();
                /*foreach (var code in e)
                {
                    Log.LogMessage(code);
                }*/
                return e;
            }
        }
    }
}
