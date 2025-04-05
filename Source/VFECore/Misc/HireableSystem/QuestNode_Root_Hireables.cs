using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using VFECore.Misc.HireableSystem;
using static System.Collections.Specialized.BitVector32;
using static UnityEngine.ParticleSystem;

namespace VFECore.Misc
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

        // Optionally, you can also define a Prefix if you want to run code before the original method is called
        /*
        [HarmonyPrefix]
        public static bool Prefix(FactionManager __instance, Faction faction)
        {
            Log.Message($"Removing faction: {faction.Name}");
            return true; // Return true to allow the original method to run, or false to skip it.
        }
        */
    }


    public class QuestNode_Root_Hireables : QuestNode
    {
        private void foo(){
            // Pawn pawn = quest.GeneratePawn(PawnKindDefOf.Refugee, faction, true, null, 0f, true, null, 0f, 0f, false, true, DevelopmentalStage.Adult, true);
        }

        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Map map = QuestGen_Get.GetMap(false, null);

            var hireableFaction = slate.Get<HireableFactionDef>("hireableFaction");
            var hireable = slate.Get<Hireable>("hireable");
            var pawns = slate.Get<List<Pawn>>("pawns");
            var faction = slate.Get<Faction>("faction");
            var price = slate.Get<float>("price");

            if (faction != null)
                Log.Message($"QuestNodeRoot: Faction is {faction.Name}");
            else
                Log.Message($"QuestNodeRoot: Faction is null");
            
            slate.Set<int>("mercenaryCount", pawns.Count);
            slate.Set<Pawn>("asker", pawns.First<Pawn>());
            slate.Set<Map>("map", map, false);
            slate.Set<int>("deadCount", 0);


            int questDurationTicks = 10000;


            QuestPart_HireableContract hireableContractPart = quest.HireableContract(hireable, hireableFaction, pawns, price, null);

            QuestPart_ExtraFaction extraFactionPart = quest.ExtraFaction(faction, pawns, ExtraFactionType.MiniFaction, false); //, new List<string> { lodgerRecruitedSignal, becameZombySignal   });

            foreach (var pawn in pawns)
            {
                pawn.SetFaction(Faction.OfPlayer);
                if (pawn.playerSettings != null)
                    pawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
            }

            quest.DropPods(map.Parent, pawns, null, null, null, null, new bool?(true), true, true, false, null, null, QuestPart.SignalListenMode.OngoingOnly, null, true, false, false, null);


            var questPartDelay = quest.Delay(questDurationTicks, delegate
            {
                Log.Message("Stuff in delay happening now.");
                void outAction()
                {
                    Log.Message("Stuff in outAction happening now.");
                    quest.Letter(LetterDefOf.PositiveEvent, null, null, null, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, "[mercenariesLeavingLetterText]", null, "[mercenariesLeavingLetterLabel]", null, null);
                }

                quest.SignalPassWithFaction(faction, null, outAction, null, null);
                quest.Leave(pawns, null, false, false, null, true);
                quest.End(QuestEndOutcome.Success, 0, null, null, QuestPart.SignalListenMode.OngoingOnly, true, false);
                Log.Message("Done with delay stuff.");
            }, null, null, null, false, null, null, false, "GuestsDepartsIn".Translate(), "GuestsDepartsOn".Translate(), "QuestDelay", false, QuestPart.SignalListenMode.OngoingOnly, false);


            if (faction != null)
                Log.Message($"QuestNodeRoot end: Faction is {faction.Name}");
            else
                Log.Message($"QuestNodeRoot end: Faction is null");

            Log.Message($"Quest part reserves faction: {extraFactionPart.QuestPartReserves(faction)}");


        }

        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
    }
}
