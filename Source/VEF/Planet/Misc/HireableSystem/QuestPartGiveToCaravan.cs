using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld;
using Verse;
using static RimWorld.Reward_Pawn;
using System.Security.Policy;

namespace VEF.Planet
{
    public class QuestPartGiveToCaravan : QuestPartActivable
    {
        public List<Pawn> pawns;
        public Caravan caravan;

        protected override void Enable(SignalArgs receivedArgs)
        {
            base.Enable(receivedArgs);

            foreach (Pawn pawn in pawns)
                caravan.AddPawnOrItem(pawn, addCarriedPawnToWorldPawnsIfAny: true);

            Complete();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            Scribe_References.Look(ref caravan, "caravan");
        }

    }

    public static partial class QuestGen_Hireable
    {
        public static QuestPartGiveToCaravan GiveToCaravan(this Quest quest, IEnumerable<Pawn> pawns, Caravan caravan, string inSignalEnable = null)
        {
            QuestPartGiveToCaravan qp = new QuestPartGiveToCaravan();
            qp.inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnable) ?? QuestGen.slate.Get<string>("inSignal");
            qp.reactivatable = false;
            qp.caravan = caravan;
            qp.signalListenMode = QuestPart.SignalListenMode.OngoingOnly;

            qp.pawns = [.. pawns];

            qp.debugLabel = "QuestPartGiveToCaravan";

            quest.AddPart(qp);
            return qp;
        }
    }

}
