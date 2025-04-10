using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld;
using Verse;

namespace VFECore.Misc.HireableSystem
{
    public class QuestPartGiveToCaravan : QuestPartActivable
    {
        public Caravan caravan;
        public List<Pawn> pawns;

        protected override void Enable(SignalArgs receivedArgs)
        {
            base.Enable(receivedArgs);

            foreach (Pawn pawn in pawns)
                caravan.AddPawnOrItem(pawn, addCarriedPawnToWorldPawnsIfAny: true);
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
