using HarmonyLib;
using NodeCanvas.Tasks.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphDumper
{
#if DEBUG
    [HarmonyDebug]
#endif
    [HarmonyPatch(typeof(QuestAction_AddLogEntry), nameof(QuestAction_AddLogEntry.info), MethodType.Getter)]
    public class QuestAction_AddLogEntry_Patch
    {
        // removes the splicetext
        static void Postfix(QuestAction_AddLogEntry __instance, ref string __result)
        {
            GraphDumper.DebugLog($"Removing splice");
            string str;
            if (__instance.AssociatedLogType == QuestAction_AddLogEntry.LogType.SimpleText || __instance.m_logSignatureUID.IsNull || !__instance.IsCompleted)
            {
                str = "Add Log Entry\n";
            }
            else
            {
                str = "Update Log Entry\n";
            }
            string text = __instance.statement.text;
            text = Global.SpliceText(text, 30);
            text = "\"" + text + "\"";
            text += "\n";
            __result = str + text;
        }
    }
}
