using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace VEF.Planet
{
    public class QuestPart_AttackEnemyBase : QuestPartActivable
    {
        public List<Pawn> pawns;
        public Settlement enemyBase;
        public PawnsArrivalModeDef arrivalMode;

        protected override void Enable(SignalArgs receivedArgs)
        {
            base.Enable(receivedArgs);

            TransportersArrivalAction_AttackSettlement arrivalAction = new TransportersArrivalAction_AttackSettlement(enemyBase, arrivalMode);
            arrivalAction.Arrived(QuestUtil.MakePods(pawns).ToList(), enemyBase.Tile);

            Complete();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            Scribe_References.Look(ref enemyBase, "enemyBase");
            Scribe_Defs.Look(ref arrivalMode, "arrivalMode");
        }
    }

    public static partial class QuestGen_Hireable
    {
        public static QuestPart_AttackEnemyBase AttackEnemyBase(this Quest quest, Settlement enemyBase, IEnumerable<Pawn> pawns, PawnsArrivalModeDef arrivalMode, string inSignalEnable = null)
        {
            QuestPart_AttackEnemyBase qp = new QuestPart_AttackEnemyBase();
            qp.inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnable) ?? QuestGen.slate.Get<string>("inSignal");
            qp.reactivatable = false;
            qp.arrivalMode = arrivalMode;
            qp.signalListenMode = QuestPart.SignalListenMode.OngoingOnly;

            qp.pawns = pawns.ToList();
            qp.enemyBase = enemyBase;

            qp.debugLabel = "QuestPart_AttackEnemyBase";

            quest.AddPart(qp);
            return qp;
        }
    }
}
