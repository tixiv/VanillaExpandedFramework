using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace VFECore.Misc.HireableSystem
{
    public class QuestPart_DebugAction : QuestPartActivable
    {
        public Action<SignalArgs> Action;

        protected override void Enable(SignalArgs receivedArgs)
        {
            base.Enable(receivedArgs);
            Action(receivedArgs);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Log.Warning("Trying to load or save QuestPart_DebugAction. This can't be done because the lambda function can't be reconsturcted on load, at least not easily. That is probably why this utility doesn't exist in the game. Or does it?");
        }
    }

    public static class QuestUtil
    {
        public static void LogPawnInfo(Pawn p, Quest q = null)
        {
            string factionString = p.Faction != null ? p.Faction.Name + $"-temporary={p.Faction.temporary}-hidden={p.Faction.Hidden}" : "null";

            Log.Message($"PawnMaindesc={p.MainDesc(true, true)}, faction={factionString}");
            List<ExtraFaction> outExtraFactions = [];

            QuestUtility.GetExtraFactionsFromQuestParts(p, outExtraFactions, null);
            foreach (ExtraFaction f in outExtraFactions)
                Log.Message($"ExtraFaction nullQuest: name={f.faction.Name}, type={f.factionType}");

            if (q != null)
            {
                QuestUtility.GetExtraFactionsFromQuestParts(p, outExtraFactions, q);
                foreach (ExtraFaction f in outExtraFactions)
                    Log.Message($"ExtraFaction Quest: name={f.faction.Name}, type={f.factionType}");
            }
        }


        public static QuestPart_DebugAction DebugAction(this Quest quest, Action<SignalArgs> action, string inSignalEnable = null)
        {
            QuestPart_DebugAction qp = new QuestPart_DebugAction();
            qp.inSignalEnable = QuestGenUtility.HardcodedSignalWithQuestID(inSignalEnable) ?? QuestGen.slate.Get<string>("inSignal");
            qp.Action = action;
            quest.AddPart(qp);
            return qp;
        }

    }
}
