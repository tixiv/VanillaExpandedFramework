using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using RimWorld.QuestGen;
using RimWorld;
using Verse;
using UnityEngine.Tilemaps;

namespace VEF.Planet
{
    public class QuestPart_VisitSite : QuestPartActivable
    {
        public List<Pawn> pawns;
        public Site site;
        public PawnsArrivalModeDef arrivalMode;

        protected override void Enable(SignalArgs receivedArgs)
        {
            base.Enable(receivedArgs);

            TransportersArrivalAction_VisitSite arrivalAction = new TransportersArrivalAction_VisitSite(site, arrivalMode);
            arrivalAction.Arrived(QuestUtil.MakePods(pawns).ToList(), site.Tile);

            Complete();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
            Scribe_References.Look(ref site, "site");
            Scribe_Defs.Look(ref arrivalMode, "arrivalMode");
        }
    }

    public static partial class QuestGen_Hireable
    {
        public static QuestPart_VisitSite VisitSite(this Quest quest, Site site, IEnumerable<Pawn> pawns, PawnsArrivalModeDef arrivalMode, string inSignalEnable = null)
        {
            QuestPart_VisitSite qp = new QuestPart_VisitSite();
            qp.inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnable) ?? QuestGen.slate.Get<string>("inSignal");
            qp.reactivatable = false;
            qp.arrivalMode = arrivalMode;
            qp.signalListenMode = QuestPart.SignalListenMode.OngoingOnly;

            qp.pawns = pawns.ToList();
            qp.site = site;

            qp.debugLabel = "QuestPart_VisitSite";

            quest.AddPart(qp);
            return qp;
        }
    }
}
