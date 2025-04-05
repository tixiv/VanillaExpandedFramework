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
using static RimWorld.QuestPart;
using static UnityEngine.Random;

namespace VFECore.Misc.HireableSystem
{

    public class ContractInfo
    {
        public int endTicks;
        public HireableFactionDef factionDef;
        public Hireable hireable;
        public List<Pawn> pawns = [];
        public float price;
    };

    public class QuestPart_HireableContract : QuestPart_Delay
    {
        public ContractInfo contractInfo = new ContractInfo();
        public MapParent mapParent;
        public Faction faction;
        int deadCount = 0;

        public string outSignal_AssaultColony;
        public string outSignal_Flee;
        public string outSignal_Completed;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref contractInfo.pawns, "pawns", LookMode.Reference);
            Scribe_Values.Look(ref contractInfo.endTicks, "endTicks");
            Scribe_Values.Look(ref deadCount, "deadCount");
            Scribe_References.Look(ref contractInfo.hireable, "hireable");
            Scribe_Values.Look(ref contractInfo.price, "price");
            Scribe_Defs.Look(ref contractInfo.factionDef, "faction");
        }


        public override void Notify_QuestSignalReceived(Signal signal)
        {
            base.Notify_QuestSignalReceived(signal);

            string debugString = $"Signal: global={signal.global}, tag={signal.tag}, ";

            for (int i = 0; i < 3; i++)
            {
                if (signal.args.TryGetArg(i, out NamedArgument arg))
                {
                    debugString += $"arg{i}: label={arg.label} value={arg.arg.ToString()}, ";
                }
            }

            Log.Message(debugString);
        }

        public override void Notify_PawnKilled(Pawn pawn, DamageInfo? dinfo)
        {
            if (this.State == QuestPartState.Enabled)
            {
                Log.Message($"Pawn killed: {pawn.LabelCap}");

                if (contractInfo.pawns.Contains(pawn))
                {
                    deadCount++;
                    Log.Message($"It was a quest pawn");
                    if (deadCount > 1)
                    {
                        Log.Message($"To many dead, assault!!!");

                        AssaultColony(HistoryEventDefOf.MemberKilled);

                        Find.SignalManager.SendSignal(new Signal(outSignal_AssaultColony));
                        Complete();
                    }
                }
            }
        }

        private void AssaultColony(HistoryEventDef reason)
        {
            if (this.faction.HasGoodwill)
            {
                Faction.OfPlayer.TryAffectGoodwillWith(this.faction, Faction.OfPlayer.GoodwillToMakeHostile(this.faction), true, false, reason, null);
            }
            else
            {
                this.faction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, false, null, null);
            }

            foreach (Pawn p in contractInfo.pawns)
                p.GetLord()?.Notify_PawnLost(p, PawnLostCondition.ForcedByQuest, null);



            foreach (Pawn p in contractInfo.pawns)
            {
                if (!p.Dead)
                {
                    p.SetFaction(this.faction, null);
                    if (!p.Awake())
                        RestUtility.WakeUp(p, true);
                }
            }

            Lord lord = LordMaker.MakeNewLord(this.faction, new LordJob_AssaultColony(this.faction, true, true, false, false, true, false, true), mapParent.Map, null);
            foreach (Pawn p in contractInfo.pawns)
                if (!p.Dead)
                    lord.AddPawn(p);
        }

        protected override void DelayFinished()
        {
            base.DelayFinished();

            foreach(Pawn p in contractInfo.pawns)
                p.SetFaction(faction, null);

            Find.SignalManager.SendSignal(new Signal(outSignal_Completed));
            Log.Message("Contract finished");
        }
    }

    public static class QuestGen_Hireable
    {
        public static QuestPart_HireableContract HireableContract(this Quest quest, MapParent mapParent, Hireable hireable, HireableFactionDef factionDef, Faction faction, IEnumerable<Pawn> pawns, float price, int delayTicks, string inSignalEnable = null, string inSignalDisable = null)
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

            qp.mapParent = mapParent;
            qp.faction = faction;
            qp.contractInfo.hireable = hireable;
            qp.contractInfo.hireable = hireable;
            qp.contractInfo.factionDef = factionDef;
            qp.contractInfo.pawns = [.. pawns];
            qp.contractInfo.price = price;

            quest.AddPart(qp);
            return qp;
        }
    }
}
