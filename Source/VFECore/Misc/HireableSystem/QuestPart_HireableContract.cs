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
        public List<Pawn> pawns = [];
        public int endTicks;
        public HireableFactionDef factionDef;
        public Hireable hireable;
        public float price;
    };

    public class QuestPart_HireableContract : QuestPart_Delay
    {
        public ContractInfo contractInfo = new();
        public Faction faction;
        public Faction temporaryFaction;
        int deadCount = 0;

        public string outSignal_RemovePawn;
        public string outSignal_AssaultColony;
        public string outSignal_Flee;
        public string outSignal_Completed;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref contractInfo.pawns, "pawns", LookMode.Reference);
            Scribe_Values.Look(ref contractInfo.endTicks, "endTicks");
            Scribe_Defs.Look(ref contractInfo.factionDef, "factionDef");
            Scribe_References.Look(ref contractInfo.hireable, "hireable");
            Scribe_Values.Look(ref contractInfo.price, "price");

            Scribe_References.Look(ref faction, "faction");
            Scribe_References.Look(ref temporaryFaction, "temporaryFaction");
            Scribe_Values.Look(ref deadCount, "deadCount");

            Scribe_Values.Look(ref outSignal_RemovePawn, "outSignal_RemovePawn");
            Scribe_Values.Look(ref outSignal_AssaultColony, "outSignal_AssaultColony");
            Scribe_Values.Look(ref outSignal_Flee, "outSignal_Flee");
            Scribe_Values.Look(ref outSignal_Completed, "outSignal_Completed");
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

        // This will send a signal for each pawn to QuestPart_ExtraFaction so the pawns get removed there.
        private void SendSignalRemoveAllPawns()
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
                Log.Message($"Pawn killed: {pawn.LabelCap}");

                if (contractInfo.pawns.Contains(pawn))
                {
                    deadCount++;
                    Log.Message($"It was a quest pawn");
                    if (deadCount > 1)
                    {
                        Log.Message($"To many dead, assault!!!");

                        SendSignalRemoveAllPawns();

                        AssaultColony(HistoryEventDefOf.MemberKilled);

                        Find.SignalManager.SendSignal(new Signal(outSignal_AssaultColony));
                        Complete();
                    }
                }
            }
        }

        private Map TryGetMapFromPawns()
        {
            foreach (var p in contractInfo.pawns)
                if (p != null && !p.Dead && p.Map != null)
                    return p.Map;

            return null;        
        }

        private void AssaultColony(HistoryEventDef reason)
        {
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

            var map = TryGetMapFromPawns();
            if (map != null)
            {

                Lord lord = LordMaker.MakeNewLord(this.faction, new LordJob_AssaultColony(this.faction, true, true, false, false, true, false, true), map, null);
                foreach (Pawn p in contractInfo.pawns)
                    if (p != null && !p.Dead)
                        lord.AddPawn(p);
            }
        }


       

        // Overriding QuestPart_Delay::DelayFinished() because we use this to end the contrat with
        // the hired faction. We now make sure that the faction doesn't attack the player, even if
        // it is normally, hostile while leaving the map.
        protected override void DelayFinished()
        {
            base.DelayFinished();

            foreach (Pawn p in contractInfo.pawns.Where(p => !p.Dead))
            {
                QuestUtil.LogPawnInfo(p);
            }

            SendSignalRemoveAllPawns();

            foreach (Pawn p in contractInfo.pawns.Where(p => !p.Dead))
            {
                QuestUtil.LogPawnInfo(p);
            }


            Faction factionToLeaveMap = this.faction;

            // If the original faction is hostile to the player (like pirates eg.) but the player
            // finished the quest without violating the mercenaries, then we generate a temporary faction for
            // the mercenaries to leave the map nicely (or stay in the medical bay until healed).
            if (factionToLeaveMap.HostileTo(Faction.OfPlayer))
            {
                factionToLeaveMap = temporaryFaction = HireableUtil.MakeFirendlyTemporaryFactionFromReference(factionToLeaveMap);
            }

            foreach (Pawn p in contractInfo.pawns.Where(p => !p.Dead))
            {
                QuestUtil.LogPawnInfo(p);
            }

            foreach (Pawn p in contractInfo.pawns.Where(p => !p.Dead))
            {
                p.SetFaction(factionToLeaveMap);
                Log.Message($"Set faction to {factionToLeaveMap.Name}, pawn has faction {p.Faction.Name} now.");

                QuestUtil.LogPawnInfo(p);
            }

            // We need to make the faction temporary here, because when doing it earlier it gets removed
            // during the quest. Now that it actually has pawns it will stay allive until the pawns leave the map (or die).
            if (temporaryFaction != null)
            {
                temporaryFaction.temporary = true; // temporary means it will get removed once the pawns have left the map

                foreach (Pawn p in contractInfo.pawns.Where(p => !p.Dead))
                {
                    QuestUtil.LogPawnInfo(p);
                }


                temporaryFaction.hidden = false;   // unhide fake faction while pawns are leaving the map

                foreach (Pawn p in contractInfo.pawns.Where(p => !p.Dead))
                {
                    QuestUtil.LogPawnInfo(p);
                }
            }

            Find.SignalManager.SendSignal(new Signal(outSignal_Completed));
            Log.Message("Contract finished");
        }
    }

    public static partial class QuestGen_Hireable
    {
        public static QuestPart_HireableContract HireableContract(this Quest quest, Hireable hireable, HireableFactionDef factionDef, Faction faction, Faction temporaryFaction, IEnumerable<Pawn> pawns, float price, int delayTicks, string inSignalEnable = null, string inSignalDisable = null)
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
