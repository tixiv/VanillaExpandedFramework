using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.QuestGen;
using RimWorld;
using UnityEngine.Tilemaps;
using UnityEngine;
using Verse;

namespace VFECore.Misc.HireableSystem
{
    using static System.Collections.Specialized.BitVector32;
    using HireData = List<Pair<PawnKindDef, int>>;
    public static class HireableUtil
    {
        

        private static Faction MakeTemporaryFactionFromDefInternal(FactionDef refFaction)
        {
            FactionDef factionDef = refFaction ?? FactionDefOf.OutlanderCivil;

            Log.Message("Genearating temporary faction. Ref = " + factionDef.label);

            List<FactionRelation> listFactionRelations = [];
            foreach (Faction f in Find.FactionManager.AllFactionsListForReading)
            {
                if (!f.def.PermanentlyHostileTo(factionDef))
                {
                    listFactionRelations.Add(new FactionRelation
                    {
                        other = f,
                        kind = FactionRelationKind.Neutral
                    });
                }
            }

            FactionGeneratorParms factionGeneratorParms = new FactionGeneratorParms(factionDef, default(IdeoGenerationParms), new bool?(true));
            Faction faction = FactionGenerator.NewGeneratedFactionWithRelations(factionGeneratorParms, listFactionRelations);
            
            Log.Message($"Created temporary faction: {faction.Name}. isHostile: {faction.HostileTo(Faction.OfPlayer)}");

            return faction;
        }

        public static Faction MakeTemporaryFactionFromDef(FactionDef refFaction)
        {
            Faction faction = MakeTemporaryFactionFromDefInternal(refFaction);

            faction.temporary = false;
            Find.FactionManager.Add(faction);

            return faction;
        }

        // this is used to make a fake faction that is like the hired one,
        // but it won't attack the player (in contrast to the original one)
        public static Faction MakeFirendlyTemporaryFactionFromReference(Faction referenceWorldFaction)
        {
            Faction faction = MakeTemporaryFactionFromDefInternal(referenceWorldFaction.def);

            faction.Name = referenceWorldFaction.Name + " [non hostile]";
            faction.color = referenceWorldFaction.color;

            faction.temporary = false;
            Find.FactionManager.Add(faction);

            return faction;
        }


        public static List<Pawn> generatePawns(ref readonly HireData hireData, Faction faction, Quest quest = null)
        {
            List<Pawn> pawns = [];

            foreach (var hd in hireData)
            {
                PawnKindDef kind = hd.First;
                int count = hd.Second;

                Log.Message($"Generating {count} pawns of kind {kind.LabelCap}");
                for (int i = 0; i < count; i++)
                {

                    bool flag = kind.ignoreFactionApparelStuffRequirements;
                    kind.ignoreFactionApparelStuffRequirements = true;

                    Ideo fixedIdeo = faction.ideos.GetRandomIdeoForNewPawn();

                    Pawn pawn = quest  != null ?
                        quest.GeneratePawn(new PawnGenerationRequest(kind, faction, mustBeCapableOfViolence: true, forceGenerateNewPawn: false, developmentalStages: DevelopmentalStage.Adult, forbidAnyTitle: true, allowPregnant: false, fixedIdeo: fixedIdeo), ensureNonNumericName:true) :
                        PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction, mustBeCapableOfViolence: true, forceGenerateNewPawn: false, developmentalStages: DevelopmentalStage.Adult, forbidAnyTitle: true, allowPregnant: false, fixedIdeo: fixedIdeo));

                    Log.Message($"Generated pawn :{pawn.LabelCap}");

                    kind.ignoreFactionApparelStuffRequirements = flag;

                    pawns.Add(pawn);
                }
            }

            // Strongest pawn classes first
            pawns.SortBy(p => p.kindDef.combatPower);

            return pawns;
        }

        private static void generateQuest(QuestScriptDef script, Slate slate)
        {
            if (!script.CanRun(slate))
            {
                Log.Error("Failed to generate quest. CanRun() returned false.");
            }
            else
            {
                QuestUtility.GenerateQuestAndMakeAvailable(script, slate);
            }
        }

        public static void SpawnHiredPawnsQuest(HireableFaction hireableFaction, HireData hireData, int questDurationTicks, float price, Orders orders, List<Pawn>pawns = null)
        {
            Slate slate = new Slate();
            slate.Set<HireableFaction>("hireableFaction", hireableFaction);
            slate.Set<HireData>("hireData", hireData);
            slate.Set<int>("questDurationTicks", questDurationTicks);
            slate.Set<float>("price", price);
            slate.Set<Orders>("orders", orders);
            slate.Set<List<Pawn>>("pawns", pawns);

            generateQuest(QuestDefOf.VFECore_Hireables, slate);
        }
    }
}
