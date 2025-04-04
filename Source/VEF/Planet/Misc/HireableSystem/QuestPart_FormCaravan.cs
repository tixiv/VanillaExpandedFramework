using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using static RimWorld.Reward_Pawn;

namespace VEF.Planet
{
    public class QuestPart_FormCaravan : QuestPartActivable
    {
        public int tile;
        public List<Pawn> pawns;

        protected override void Enable(SignalArgs receivedArgs)
        {
            base.Enable(receivedArgs);

            TransportersArrivalAction_FormCaravan arivalAction = new TransportersArrivalAction_FormCaravan();
            arivalAction.Arrived(QuestUtil.MakePods(pawns).ToList(), tile);

            Complete();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref tile, "tile");
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
        }
    }

    public static partial class QuestGen_Hireable
    {
        public static QuestPart_FormCaravan FormCaravan(this Quest quest, IEnumerable<Pawn> pawns, int tile, string inSignalEnable = null)
        {
            QuestPart_FormCaravan qp = new QuestPart_FormCaravan();
            qp.inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnable) ?? QuestGen.slate.Get<string>("inSignal");
            qp.reactivatable = false;
            qp.tile = tile;
            qp.signalListenMode = QuestPart.SignalListenMode.OngoingOnly;

            qp.pawns = [.. pawns];

            qp.debugLabel = "QuestPart_FormCaravan";

            quest.AddPart(qp);
            return qp;
        }
    }
}
