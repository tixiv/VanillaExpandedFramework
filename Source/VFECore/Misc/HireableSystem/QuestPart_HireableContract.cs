using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace VFECore.Misc.HireableSystem
{

    public class ContractInfo
    {
        public int endTicks;
        public HireableFactionDef factionDef;
        public Hireable hireable;
        public List<Pawn> pawns = [];
        public float price;
    }

    public class QuestPart_HireableContract : QuestPart
    {
        public ContractInfo contractInfo = new ContractInfo();

        public string inSignalRemovePawn;

        public List<string> inSignalsRemovePawn;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref contractInfo.pawns, "pawns", LookMode.Reference);
            Scribe_Values.Look(ref contractInfo.endTicks, "endTicks");
            Scribe_References.Look(ref contractInfo.hireable, "hireable");
            Scribe_Values.Look(ref contractInfo.price, "price");
            Scribe_Defs.Look(ref contractInfo.factionDef, "faction");

            Scribe_Values.Look(ref inSignalRemovePawn, "inSignalRemovePawn");
            Scribe_Collections.Look(ref inSignalsRemovePawn, "inSignalsRemovePawn", LookMode.Value);
        }
    }

    public static class QuestGen_Hireable
    {
        public static QuestPart_HireableContract HireableContract(this Quest quest, Hireable hireable, HireableFactionDef factionDef, IEnumerable<Pawn> pawns, float price, List<string> inSignalsRemovePawn = null)
        {
            QuestPart_HireableContract qp = new QuestPart_HireableContract
            {
                inSignalsRemovePawn = inSignalsRemovePawn
            };

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
