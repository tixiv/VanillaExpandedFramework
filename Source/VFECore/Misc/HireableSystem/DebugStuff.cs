using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using Verse;

namespace VFECore.Misc.HireableSystem
{
    public static class DebugTools
    {
        [DebugAction("Quests", "List quests now", false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void foo()
        {
            foreach (var q in Find.QuestManager.QuestsListForReading)
                Log.Message($"Quest list: name={q.name}, hidden={q.hidden}, hiddenInUi={q.hiddenInUI}");
        }
    }


    [HarmonyPatch(typeof(FactionManager), "Remove")]
    public static class FactionManager_Remove_Patch
    {
        // This method runs after the original Remove method (Postfix)
        [HarmonyPostfix]
        public static void Postfix(FactionManager __instance, Faction faction)
        {
            // Add custom code here that runs after the Remove method is called
            Log.Message($"Faction {faction.Name} has been removed.");
        }
    }
}
