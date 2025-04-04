using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LudeonTK;
using RimWorld;
using RimWorld.QuestGen;
using Verse;
using static System.Collections.Specialized.BitVector32;
using static UnityEngine.ParticleSystem;

namespace VFECore.Misc
{

    public static class DebugTools
    {
        [DebugAction("Quests", "List quests now", false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void foo()
        {
            foreach (var q in Find.QuestManager.QuestsListForReading)
                Log.Message($"Quest list: name={q.name}, hidden={q.hidden}, hiddenInUi={q.hiddenInUI}");
        }
    }

    public class QuestNode_Root_Hireables : QuestNode
    {
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            Slate slate = QuestGen.slate;
            Map map = QuestGen_Get.GetMap(false, null);

            // quest.SetInitiallyAccepted();

            /*
            List<FactionRelation> list2 = new List<FactionRelation>();
            foreach (Faction faction3 in Find.FactionManager.AllFactionsListForReading)
            {
                if (!faction3.def.PermanentlyHostileTo(FactionDefOf.OutlanderRefugee))
                {
                    list2.Add(new FactionRelation
                    {
                        other = faction3,
                        kind = FactionRelationKind.Neutral
                    });
                }
            }
            */

            /*
            FactionGeneratorParms factionGeneratorParms = new FactionGeneratorParms(FactionDefOf.OutlanderRefugee, default(IdeoGenerationParms), new bool?(true));
            Faction faction = FactionGenerator.NewGeneratedFactionWithRelations(factionGeneratorParms, list2);
            faction.temporary = true;
            Find.FactionManager.Add(faction);

            List<Pawn> pawns = new List<Pawn>();

            int mercenaryCount = 6;

            for (int i = 0; i < mercenaryCount; i++)
            {
                Pawn pawn = quest.GeneratePawn(PawnKindDefOf.Refugee, faction, true, null, 0f, true, null, 0f, 0f, false, true, DevelopmentalStage.Adult, true);
                if (pawn != null)
                {
                    Log.Message("Generated pawn " + pawn.LabelCap);
                    pawns.Add(pawn);
                }
                else
                    Log.Message("null pawn");
            }

            slate.Set<List<Pawn>>("mercenaries", pawns, false);
            */

            int questDurationTicks = 1000;

            var pawns = slate.Get<List<Pawn>>("pawns");

            if (pawns != null) {
                foreach (Pawn paw in pawns)
                    Log.Message("Quest pawn: " + paw.LabelCap);
            }
            else
                Log.Message("pawns is null");

            var faction = slate.Get<Faction>("faction");



            // faction.leader = pawns.First<Pawn>();
            Pawn asker = pawns.First<Pawn>();

            slate.Set<int>("mercenaryCount", pawns.Count, false);
            slate.Set<Pawn>("asker", asker, false);
            slate.Set<Map>("map", map, false);

            // slate.Set<Faction>("faction", faction, false);


            QuestPart_ExtraFaction extraFactionPart = quest.ExtraFaction(faction, pawns, ExtraFactionType.MiniFaction, false); //, new List<string> { lodgerRecruitedSignal, becameZombySignal   });


            Log.Message("quest state = " + quest.State.ToString());

            quest.DropPods(map.Parent, pawns, null, null, null, null, new bool?(true), true, true, false, null, null, QuestPart.SignalListenMode.OngoingOnly, null, true, false, false, null);

            Log.Message("quest state = " + quest.State.ToString());

            Action fooAction = null;
            quest.Delay(questDurationTicks, delegate
            {
                Quest quest2 = quest;
                Faction faction2 = faction;
                Action action = null;
                Action outAction;
                if ((outAction = fooAction) == null)
                {
                    outAction = (fooAction = delegate ()
                    {
                        quest.Letter(LetterDefOf.PositiveEvent, null, null, null, null, false, QuestPart.SignalListenMode.OngoingOnly, null, false, "[mercenariesLeavingLetterText]", null, "[mercenariesLeavingLetterLabel]", null, null);
                    });
                }
                quest2.SignalPassWithFaction(faction2, action, outAction, null, null);
                quest.Leave(pawns, null, false, false, null, true);
            }, null, null, null, false, null, null, false, "GuestsDepartsIn".Translate(), "GuestsDepartsOn".Translate(), "QuestDelay", false, QuestPart.SignalListenMode.OngoingOnly, false);
        }

        protected override bool TestRunInt(Slate slate)
        {
            return true;
        }
    }
}
