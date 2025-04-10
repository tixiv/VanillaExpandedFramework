using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Noise;

namespace VFECore.Misc.HireableSystem
{
    using HireData = List<Pair<PawnKindDef, int>>;

    public class QuestNode_Root_Hireables : QuestNode
    {
        private void foo(Quest quest, Faction faction){
            Pawn pawn = quest.GeneratePawn(PawnKindDefOf.Refugee, faction, true, null, 0f, true, null, 0f, 0f, false, true, DevelopmentalStage.Adult, true);
        }

        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;

            var hireableFaction = slate.Get<HireableFactionDef>("hireableFaction");
            var hireable = slate.Get<Hireable>("hireable");
            var hireData = slate.Get<HireData>("hireData");
            float price = slate.Get<float>("price");
            var orders = slate.Get<Orders>("orders");

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

            if (orders.WorldObject is MapParent mapParent && orders.Command == Orders.Commands.LandInExistingMap)
            {
                if (orders.Cell.HasValue)
                {
                    quest.DropPods(mapParent, pawns, sendStandardLetter: new bool?(true), dropSpot: orders.Cell);
                }
                else
                {
                    quest.DropPods(mapParent, pawns, sendStandardLetter: new bool?(true), useTradeDropSpot: true);
                }
            }
            else if (orders.WorldObject is Settlement settlement && orders.Command == Orders.Commands.AttackAndDropAtEdge)
            {
                quest.AttackEnemyBase(settlement, pawns, PawnsArrivalModeDefOf.EdgeDrop);
            }
            else if (orders.WorldObject is Settlement settlement2 && orders.Command == Orders.Commands.AttackAndDropInCenter)
            {
                quest.AttackEnemyBase(settlement2, pawns, PawnsArrivalModeDefOf.CenterDrop);
            }
            else if (orders.WorldObject is Site site && orders.Command == Orders.Commands.SiteDropAtEdge)
            {
                quest.VisitSite(site, pawns, PawnsArrivalModeDefOf.EdgeDrop);
            }
            else if (orders.WorldObject is Site site2 && orders.Command == Orders.Commands.SiteDropInCenter)
            {
                quest.VisitSite(site2, pawns, PawnsArrivalModeDefOf.CenterDrop);
            }
            else if (orders.Command == Orders.Commands.FormCaravan)
            {
                quest.FormCaravan(pawns, orders.WorldTile);
                quest.Letter(LetterDefOf.PositiveEvent, text: "Your hired mercenaries formed a caravan at the assigned location and are awaiting further orders.", label: "new caravan", lookTargets: pawns.Where(p => !p.Dead));
            }
            else if (orders.WorldObject is Caravan caravan && orders.Command == Orders.Commands.GiveToCaravan)
            {
                quest.GiveToCaravan(pawns, caravan);
                quest.Letter(LetterDefOf.PositiveEvent, text: "Your hired mercenaries have joined " + caravan.Label + ".", label: "mercenaries arrived", lookTargets: pawns.Where(p => !p.Dead));
            }
            else
            {
                Log.Error($"Bad orders {orders}");
            }

            quest.Signal(contractCompletedSignal, delegate
            {
                quest.Letter(LetterDefOf.NeutralEvent, text: "[mercenariesLeavingLetterText]", label: "[mercenariesLeavingLetterLabel]", lookTargets: pawns.Where(p => !p.Dead));

                quest.Leave(pawns, sendStandardLetter: false, leaveOnCleanup: false, wakeUp: false);
                quest.End(QuestEndOutcome.Success, inSignal: null, sendStandardLetter: false);
            });

            quest.Signal(assaultColonySignal, delegate
            {
                quest.Letter(LetterDefOf.ThreatBig, text: "The mercenaries got angry and are assaulting your colonists now", label: "Mutiny", lookTargets: pawns.Where(p => !p.Dead));
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
