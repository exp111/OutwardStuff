using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Tutorial
{
#if DEBUG
    [HarmonyDebug]
#endif
    // Need to specify parameters as function is overloaded
    [HarmonyPatch(typeof(TargetingSystem), nameof(TargetingSystem.IsTargetable), new[] { typeof(Character.Factions) })]
    public class TargetingSystem_IsTargetable
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var compareTo = AccessTools.Method(typeof(Enum), nameof(Enum.CompareTo));

                var cur = new CodeMatcher(instructions);

                cur.MatchForward(false,
                    new CodeMatch(OpCodes.Callvirt, compareTo), // CompareTo(_faction)
                    new CodeMatch(OpCodes.Brtrue) // return true
                )
                .Advance(1); // go to brtrue

                Debug.Assert(cur.Opcode == OpCodes.Brtrue);
                // change to false
                cur.Opcode = OpCodes.Brfalse;

                var e = cur.InstructionEnumeration();
                foreach (var code in e)
                {
                    Tutorial.DebugLog(code.ToString());
                }
                return e;
            }
            catch (Exception e) 
            {
                Tutorial.DebugLog($"Exception during TargetingSystem_IsTargetable.Transpiler: {e}");
                return instructions;
            }
        }
    }
}
