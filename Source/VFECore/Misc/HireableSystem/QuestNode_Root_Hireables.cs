using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace VFECore.Misc.HireableSystem
{
    using HireData = List<Pair<PawnKindDef, int>>;

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
        private void foo(Quest quest, Faction faction){
            Pawn pawn = quest.GeneratePawn(PawnKindDefOf.Refugee, faction, true, null, 0f, true, null, 0f, 0f, false, true, DevelopmentalStage.Adult, true);
        }

        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;

            // Todo: find appropriate Map. this gets a random one
            Map map = QuestGen_Get.GetMap(false, null);
            slate.Set<Map>("map", map, false);

            var hireableFaction = slate.Get<HireableFactionDef>("hireableFaction");
            var hireable = slate.Get<Hireable>("hireable");
            var hireData = slate.Get<HireData>("hireData");
            float price = slate.Get<float>("price");
            int questDurationTicks = 10000;

            Faction faction = getOrMakeFactionOfDef(in hireableFaction, out Faction temporaryFaction);

            if (faction != null)
                Log.Message($"Setting faction to {faction.Name}");
            else
            {
                Log.Error("faction is null?!?");
                return;
            }

            List<Pawn> pawns = HireableUtil.generatePawns(in hireData, faction, quest);

            slate.Set<int>("mercenaryCount", pawns.Count);
            slate.Set<Pawn>("asker", pawns.First<Pawn>());
            slate.Set<int>("deadCount", 0);

            // Explicit Signals

            string removePawnSignal = QuestGenUtility.HardcodedSignalWithQuestID("hireables.RemovePawn");
            string contractCompletedSignal = QuestGenUtility.HardcodedSignalWithQuestID("hireables.ContractCompleted");
            string assaultColonySignal = QuestGenUtility.HardcodedSignalWithQuestID("hireables.AssaultColony");

            // This quest part will make the pawns display a second faction, their home faction, while being hired.
            // This happens as long as the quest state is ongoing and this part includes the pawns.

            QuestPart_ExtraFaction extraFactionPart = quest.ExtraFaction(faction, pawns, ExtraFactionType.MiniFaction, false, [removePawnSignal]);


            QuestPart_HireableContract hireableContractPart = quest.HireableContract(hireable, hireableFaction, faction, temporaryFaction, pawns, price, questDurationTicks);
            hireableContractPart.outSignal_RemovePawn = removePawnSignal;
            hireableContractPart.outSignal_Completed = contractCompletedSignal;
            hireableContractPart.outSignal_AssaultColony = assaultColonySignal;

            foreach (var pawn in pawns)
            {
                pawn.SetFaction(Faction.OfPlayer);
                if (pawn.playerSettings != null)
                    pawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
            }

            

            quest.DropPods(map.Parent, pawns, sendStandardLetter: new bool?(true), useTradeDropSpot: true, dropSpot: null);

            quest.Signal(contractCompletedSignal, delegate
            {
                quest.Letter(LetterDefOf.PositiveEvent, text: "[mercenariesLeavingLetterText]", label: "[mercenariesLeavingLetterLabel]", lookTargets: pawns.Where(p => !p.Dead));

                quest.Leave(pawns, sendStandardLetter: false, leaveOnCleanup: false, wakeUp: false);

                quest.Delay(500, delegate
                {
                    
                    quest.DebugAction( delegate
                    {
                        Log.Message("Yeay! delayed execution!!");
                        QuestUtil.LogPawnInfo(pawns[0], quest);
                    });
                    quest.Delay(500, delegate
                    {

                        quest.DebugAction((SignalArgs args) =>
                        {
                            Log.Message("Yeay! delayed execution 2!!");
                            QuestUtil.LogPawnInfo(pawns[0], quest);
                        });

                        quest.End(QuestEndOutcome.Success, inSignal: null, sendStandardLetter: true);

                    });

                });

            });

            quest.Signal(assaultColonySignal, delegate
            {
                quest.Letter(LetterDefOf.ThreatBig, text: "They got angry and are assaulting the colony now", label: "Mutiny", lookTargets: pawns.Where(p => !p.Dead));
                quest.End(QuestEndOutcome.Fail, inSignal: null);
            });

            if (faction != null)
                Log.Message($"QuestNodeRoot end: Faction is {faction.Name}");
            else
                Log.Message($"QuestNodeRoot end: Faction is null");

            Log.Message($"Quest part reserves faction: {extraFactionPart.QuestPartReserves(faction)}");

        }

        private Faction getOrMakeFactionOfDef(in HireableFactionDef hireableFaction, out Faction temporaryFaction)
        {
            Faction worldFaction = hireableFaction.referencedFaction != null ? Find.World.factionManager.FirstFactionOfDef(hireableFaction.referencedFaction) : null;

            if (worldFaction != null)
                Log.Message($"World faction is {worldFaction.Name}");

            if (worldFaction != null)
            {
                temporaryFaction = null;
                return worldFaction;
            }
            else
            {
                return temporaryFaction = HireableUtil.MakeTemporaryFactionFromDef(hireableFaction.referencedFaction);
            }
        }

        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
    }
}
