using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.AI.Group;
using static System.Collections.Specialized.BitVector32;
using static RimWorld.QuestPart;
using static UnityEngine.Random;

namespace VFECore.Misc.HireableSystem
{

    public class ContractInfo
    {
        public List<Pawn> pawns = [];
        public int endTicks;
        public HireableFactionDef hireableFactionDef;
        public float price;
    };

    public class QuestPart_HireableContract : QuestPart_Delay
    {
        public ContractInfo contractInfo = new();
        public Faction faction;
        public Faction temporaryFaction;
        public HireableFaction hireableFaction;
        int deadCount = 0;

        public string outSignal_RemovePawn;
        public string outSignal_AssaultColony;
        public string outSignal_Flee;
        public string outSignal_Completed;

        public string inSignal_Arrested;
        public string inSignal_Destroyed;
        public string inSignal_Kidnapped;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref contractInfo.pawns, "pawns", LookMode.Reference);
            Scribe_Values.Look(ref contractInfo.endTicks, "endTicks");
            Scribe_Defs.Look(ref contractInfo.hireableFactionDef, "factionDef");
            Scribe_Values.Look(ref contractInfo.price, "price");

            Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref temporaryFaction, "temporaryFaction");
            Scribe_References.Look(ref hireableFaction, "hireableFaction");
            Scribe_Values.Look(ref deadCount, "deadCount");

            Scribe_Values.Look(ref outSignal_RemovePawn, "outSignal_RemovePawn");
            Scribe_Values.Look(ref outSignal_AssaultColony, "outSignal_AssaultColony");
            Scribe_Values.Look(ref outSignal_Flee, "outSignal_Flee");
            Scribe_Values.Look(ref outSignal_Completed, "outSignal_Completed");

            Scribe_Values.Look(ref inSignal_Arrested, "inSignal_Arrested");
            Scribe_Values.Look(ref inSignal_Destroyed, "inSignal_Destroyed");
            Scribe_Values.Look(ref inSignal_Kidnapped, "inSignal_Kidnapped");
        }

        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            string debugString = $"Signal: global={signal.global}, tag={signal.tag}, ";

            for (int i = 0; i < 3; i++)
            {
                if (signal.args.TryGetArg(i, out NamedArgument arg))
                {
                    debugString += $"arg{i}: label={arg.label} value={arg.arg}, ";
                }
            }

            Log.Message(debugString);


            if (signal.tag == this.inSignal_Kidnapped && signal.args.TryGetArg<Pawn>("SUBJECT", out Pawn pawn) && contractInfo.pawns.Contains(pawn))
            {
                hireableFaction.NotifyPawnKidnapped();
            }
            else if (signal.tag == this.inSignal_Destroyed && signal.args.TryGetArg<Pawn>("SUBJECT", out Pawn pawn2) && contractInfo.pawns.Contains(pawn2))
            {
            }
            else if (signal.tag == this.inSignal_Arrested && signal.args.TryGetArg<Pawn>("SUBJECT", out Pawn pawn3) && contractInfo.pawns.Contains(pawn3))
            {
            }


        }

        // This will send a signal for each pawn to QuestPart_ExtraFaction so the pawns get removed there.
        private void SendSignalsToRemoveAllPawnsFromQuestPartExtraFaction()
        {
            foreach (Pawn p in contractInfo.pawns)
            {
                if (p != null && !p.Dead)
                {
                    Find.SignalManager.SendSignal(new Signal(outSignal_RemovePawn, new NamedArgument(p, "SUBJECT")));
                }
            }
        }

        public override void Notify_PawnKilled(Pawn pawn, DamageInfo? dinfo)
        {
            if (this.State == QuestPartState.Enabled)
            {
                Log.Message($"Pawn killed: {pawn.LabelCap}, dinfo={dinfo}, intended_traget={dinfo?.IntendedTarget}");

                if (contractInfo.pawns.Contains(pawn))
                {
                    Log.Message($"It was a quest pawn");

                    hireableFaction.NotifyPawnKilled();
                    deadCount++;

                    if ((dinfo?.Instigator?.Faction == Faction.OfPlayer && dinfo?.IntendedTarget == pawn) || // Murdered on purpose
                        (deadCount > 1 && dinfo?.Instigator?.Faction == Faction.OfPlayer)) // To much friendly fire
                    {
                        Log.Message($"To many dead, assault!!!");

                        SendSignalsToRemoveAllPawnsFromQuestPartExtraFaction();

                        AssaultColony(HistoryEventDefOf.MemberKilled);

                        Find.SignalManager.SendSignal(new Signal(outSignal_AssaultColony));
                        Complete();
                    }
                }
            }
        }

        private List<Map> TryGetMapsFromPawns()
        {
            List <Map> list = new List <Map>();

            foreach (var p in contractInfo.pawns)
                if (p != null && !p.Dead && p.Map != null)
                    list.AddDistinct(p.Map);

            return list;        
        }

        private void AssaultColony(HistoryEventDef reason)
        {
            var caravanPawns = contractInfo.pawns.Where(p => p.GetCaravan() != null);
            foreach (Pawn p in caravanPawns)
            {
                // Remove from caravan
                RemoveCaravanPawn(p.GetCaravan(), p);

                // Return pawn to home faction
                p.SetFaction(this.faction);
            }

            if (this.faction.HostileTo(Faction.OfPlayer))
            {
                // achieved: faction is hostile to player anyway. The faction can just attack. Nothing to do.
            }
            else if (this.faction.HasGoodwill)
            {
                Faction.OfPlayer.TryAffectGoodwillWith(this.faction, Faction.OfPlayer.GoodwillToMakeHostile(this.faction), true, false, reason, null);
            }
            else
            {
                this.faction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false, null, null);
            }

            foreach (Pawn p in contractInfo.pawns)
                p?.GetLord()?.Notify_PawnLost(p, PawnLostCondition.ForcedByQuest, null);


            foreach (Pawn p in contractInfo.pawns)
            {
                if (p != null && !p.Dead)
                {
                    p.SetFaction(this.faction, null);
                    if (!p.Awake())
                        RestUtility.WakeUp(p, true);
                }
            }

            foreach (Map map in TryGetMapsFromPawns())
            {
                Lord lord = LordMaker.MakeNewLord(this.faction, new LordJob_AssaultColony(this.faction, canPickUpOpportunisticWeapons: true), map);
                foreach (Pawn p in contractInfo.pawns)
                    if (p != null && !p.Dead && p.Map == map)
                        lord.AddPawn(p);
            }
        }

        private List<Pawn> getKidnappedPawns()
        {
            List<Pawn> kidnappedPawnlist = new List<Pawn>();

            foreach (var f in Find.FactionManager.AllFactionsListForReading)
                kidnappedPawnlist.AddRange(f.kidnapped.KidnappedPawnsListForReading);

            return contractInfo.pawns.Where(p => kidnappedPawnlist.Contains(p)).ToList();
        }

        private void RemoveCaravanPawn(Caravan caravan, Pawn pawn)
        {
            if (!caravan.PawnsListForReading.Any((Pawn x) => x != pawn && caravan.IsOwner(x)))
            {
                foreach (Thing item in CaravanInventoryUtility.AllInventoryItems(caravan))
                    item.Notify_AbandonedAtTile(caravan.Tile);

                caravan.RemovePawn(pawn);
                
                Find.LetterStack.ReceiveLetter(
                    "LetterLabelCaravanLost".Translate(),
                    "LetterCaravanLost".Translate(caravan.Name).CapitalizeFirst(),
                    LetterDefOf.NegativeEvent, new GlobalTargetInfo(caravan.Tile));

                caravan.pawns.Clear();
                caravan.Destroy();
                return;
            }
            caravan.RemovePawn(pawn);
        }

        private void HandleCaravanPawns()
        {
            List<Pawn> caravanPawns = contractInfo.pawns.Where(p => p.GetCaravan() != null).ToList();

            foreach (Pawn p in caravanPawns)
            {
                // Remove from caravan
                RemoveCaravanPawn(p.GetCaravan(), p);

                // Return pawn to home faction
                p.SetFaction(this.faction);
            }
        }

        protected override void DelayFinished()
        {
            base.DelayFinished();
            SendSignalsToRemoveAllPawnsFromQuestPartExtraFaction();

            HandleCaravanPawns();

            List<Pawn> pawnsToLeaveMap = contractInfo.pawns.Where(p => p != null && !p.Dead && p.Map != null).ToList();

            Faction factionToLeaveMap = this.faction;

            // If the original faction is hostile to the player (like pirates eg.) but the player
            // finished the quest without violating the mercenaries, then we generate a temporary faction for
            // the mercenaries to leave the map nicely (or stay in the medical bay until healed).
            if (factionToLeaveMap.HostileTo(Faction.OfPlayer) && pawnsToLeaveMap.Any())
            {
                factionToLeaveMap = temporaryFaction = HireableUtil.MakeFirendlyTemporaryFactionFromReference(factionToLeaveMap);
            }

            foreach (Pawn p in pawnsToLeaveMap)
            {
                p.SetFaction(factionToLeaveMap);
            }

            // We need to make the faction temporary here, because when doing it earlier it gets removed
            // during the quest. Now that it actually has pawns it will stay allive until the pawns leave the map (or die).
            if (temporaryFaction != null)
            {
                temporaryFaction.temporary = true; // temporary means it will get removed once the pawns have left the map
                temporaryFaction.hidden = false;   // unhide fake faction while pawns are leaving the map
            }

            Find.SignalManager.SendSignal(new Signal(outSignal_Completed));
        }
    }

    public static partial class QuestGen_Hireable
    {
        public static QuestPart_HireableContract HireableContract(this Quest quest, HireableFaction hireableFaction, Faction faction, Faction temporaryFaction, IEnumerable<Pawn> pawns, float price, int delayTicks, string inSignalEnable = null, string inSignalDisable = null)
        {
            QuestPart_HireableContract qp = new QuestPart_HireableContract();
            qp.delayTicks = delayTicks;
            qp.inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnable) ?? QuestGen.slate.Get<string>("inSignal");
            qp.inSignalDisable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalDisable);
            qp.reactivatable = false;
            qp.signalListenMode = QuestPart.SignalListenMode.OngoingOnly;
            qp.waitUntilPlayerHasHomeMap = false;

            qp.expiryInfoPart = "GuestsDepartsIn".Translate();
            qp.expiryInfoPartTip = "GuestsDepartsOn".Translate();
            qp.debugLabel = "QuestPart_HireableContract";

            qp.faction = faction;
            qp.temporaryFaction = temporaryFaction;
            qp.hireableFaction = hireableFaction;
            qp.contractInfo.hireableFactionDef = hireableFaction.Def;
            qp.contractInfo.pawns = [.. pawns];
            qp.contractInfo.price = price;
            qp.contractInfo.endTicks = Find.TickManager.TicksAbs + delayTicks;

            quest.AddPart(qp);
            return qp;
        }
    }
}
