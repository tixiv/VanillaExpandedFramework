using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.Sound;
using VFECore.UItils;

namespace VFECore.Misc
{
    [DefOf]
    public class QuestDefOf
    {
        public static QuestScriptDef VFECore_Hireables;
    }


    public class Dialog_Hire : Window
    {
        private static Ideo hiredIdeo;
        private readonly float availableSilver;
        private readonly Hireable hireable;
        private readonly Dictionary<PawnKindDef, Pair<int, string>> hireData;
        private readonly float riskMultiplier;
        private readonly Map targetMap;
        private HireableFactionDef curFaction;
        private int daysAmount;
        private string daysAmountBuffer;

        public Dialog_Hire(Thing negotiator, Hireable hireable)
        {
            targetMap = negotiator.Map;
            this.hireable = hireable;
            hireData = hireable.SelectMany(def => def.pawnKinds).ToDictionary(def => def, _ => new Pair<int, string>(0, ""));
            closeOnCancel = true;
            forcePause = true;
            closeOnAccept = true;
            availableSilver = targetMap.listerThings.ThingsOfDef(ThingDefOf.Silver)
                                    .Where(x => !x.Position.Fogged(x.Map) && (targetMap.areaManager.Home[x.Position] || x.IsInAnyStorage())).Sum(t => t.stackCount);
            riskMultiplier = Find.World.GetComponent<HiringContractTracker>().GetFactorForHireable(hireable);
        }

        public override Vector2 InitialSize => new Vector2(750f, 650f);
        protected override float Margin => 15f;
        private float CostBase => CostDays * CostPawns();

        private float CostDays => Mathf.Pow(daysAmount, 0.8f);

        private float CostFinal => CostBase * (riskMultiplier + 1f);

        private float CostPawns(ICollection<PawnKindDef> except = null) =>
            hireData.Select(kv => new Pair<PawnKindDef, int>(kv.Key, kv.Value.First)).Where(pair => pair.Second > 0 && (except == null || !except.Contains(pair.First)))
                 .Sum(pair => Mathf.Pow(pair.Second, 1.2f) * pair.First.combatPower);

        private void removeSilver(int amount)
        {
            List<Thing> silverList = targetMap.listerThings.ThingsOfDef(ThingDefOf.Silver)
                                              .Where(x => !x.Position.Fogged(x.Map) && (targetMap.areaManager.Home[x.Position] || x.IsInAnyStorage())).ToList();
            while (amount > 0)
            {
                Thing silver = silverList.First(t => t.stackCount > 0);
                int amountToTakeFromThisStack = Mathf.Min(amount, silver.stackCount);
                silver.SplitOff(amountToTakeFromThisStack).Destroy();
                amount -= amountToTakeFromThisStack;
            }
        }


        private void generateQuest(QuestScriptDef script, Slate slate)
        {
            if (!script.CanRun(slate))
            {
                Messages.Message("Failed to generate quest. CanRun returned false.", MessageTypeDefOf.RejectInput, false);
            }
            else
            {
                QuestUtility.GenerateQuestAndMakeAvailable(script, slate);
            }
        }

        public override void OnAcceptKeyPressed()
        {
            base.OnAcceptKeyPressed();
            SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();

            if (daysAmount > 0 && hireData.Any(kvp => kvp.Value.First > 0))
            {
                Log.Message("OnAcceptKeyPressed 1");
                
                removeSilver(Mathf.RoundToInt(CostFinal));

                Log.Message("OnAcceptKeyPressed 2");

                FactionDef factionDef = FactionDefOf.OutlanderCivil;

                Log.Message("OnAcceptKeyPressed 3");

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

                Log.Message("OnAcceptKeyPressed 4");

                FactionGeneratorParms factionGeneratorParms = new FactionGeneratorParms(factionDef, default(IdeoGenerationParms), new bool?(true));
                Faction faction = FactionGenerator.NewGeneratedFactionWithRelations(factionGeneratorParms, listFactionRelations);
                faction.temporary = true;
                Find.FactionManager.Add(faction);

                Log.Message("OnAcceptKeyPressed 5");

                List<Pawn> pawns = [];

                foreach (KeyValuePair<PawnKindDef, Pair<int, string>> kvp in hireData)
                    for (int i = 0; i < kvp.Value.First; i++)
                    {

                        bool flag = kvp.Key.ignoreFactionApparelStuffRequirements;
                        kvp.Key.ignoreFactionApparelStuffRequirements = true;


                        Log.Message("OnAcceptKeyPressed 6");

                        Ideo fixedIdeo = curFaction.referencedFaction is null || Find.World.factionManager.FirstFactionOfDef(curFaction.referencedFaction) is not {} refFaction ?
                                                                                                                                hiredIdeo ??= IdeoGenerator.GenerateIdeo(
                                                                                                                                new IdeoGenerationParms(Faction.OfPlayer.def, classicExtra: true)) :
                                                                                                                                refFaction.ideos.GetRandomIdeoForNewPawn();

                        Log.Message("OnAcceptKeyPressed 7");

                        // Faction faction = Faction.OfPlayer;

                        Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kvp.Key, mustBeCapableOfViolence: true, faction: faction,
                                                                                            forbidAnyTitle: true, fixedIdeo: fixedIdeo));


                        if (pawn != null)
                        {
                            Log.Message("OnAcceptKeyPressed 8");

                            kvp.Key.ignoreFactionApparelStuffRequirements = flag;

                            if (pawn.playerSettings != null)
                            {
                                pawn.playerSettings.hostilityResponse = HostilityResponseMode.Attack;
                            }
                            else
                                Log.Message("pawn.playerSettings is null");

                            Log.Message("OnAcceptKeyPressed 9");

                            pawns.Add(pawn);
                        }
                        else
                            Log.Message("pawn is null");


                        /*
                        IntVec3 loc = DropCellFinder.TryFindSafeLandingSpotCloseToColony(targetMap, IntVec2.Two);

                        ActiveDropPodInfo activeDropPodInfo = new ActiveDropPodInfo();
                        activeDropPodInfo.innerContainer.TryAdd(pawn, 1);
                        activeDropPodInfo.openDelay                     = 60;
                        activeDropPodInfo.leaveSlag                     = false;
                        activeDropPodInfo.despawnPodBeforeSpawningThing = true;
                        activeDropPodInfo.spawnWipeMode                 = WipeMode.Vanish;
                        DropPodUtility.MakeDropPodAt(loc, this.targetMap, activeDropPodInfo);
                        */
                    }

                Log.Message("OnAcceptKeyPressed 10");

                Slate slate = new Slate();
                slate.Set<Faction>("faction", faction);
                slate.Set<List<Pawn>>("pawns", pawns, false);

                generateQuest(QuestDefOf.VFECore_Hireables, slate);

                Find.World.GetComponent<HiringContractTracker>().SetNewContract(daysAmount, pawns, hireable, curFaction, CostFinal);
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            var rect   = new Rect(inRect);
            var anchor = Text.Anchor;
            var font   = Text.Font;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font   = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 40f), hireable.GetCallLabel());
            Text.Font =  GameFont.Small;
            rect.yMin += 40f;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 20f), "VEF.AvailableSilver".Translate(availableSilver.ToStringMoney()));
            rect.yMin += 30f;
            foreach (var def in hireable) DoHireableFaction(ref rect, def);
            var breakDownRect = rect.TakeTopPart(100f);
            breakDownRect.xMin += 115f;
            Text.Anchor        =  TextAnchor.UpperLeft;
            Text.Font          =  GameFont.Small;
            var infoRect = breakDownRect.TopPartPixels(20f);
            Widgets.Label(infoRect.LeftHalf(), "VEF.Breakdown".Translate());
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font   = GameFont.Tiny;
            Widgets.Label(infoRect.RightHalf(), "VEF.LongTerm".Translate().Colorize(ColoredText.SubtleGrayColor));
            Text.Font  =  GameFont.Small;
            infoRect.y += 20f;
            Widgets.DrawLightHighlight(infoRect);
            Widgets.Label(infoRect.LeftHalf(), "VEF.DayAmount".Translate());
            UIUtility.DrawCountAdjuster(ref daysAmount, infoRect.RightHalf(), ref daysAmountBuffer, 0, 60, false, null,
                                        Mathf.Max(Mathf.FloorToInt(Mathf.Pow(availableSilver / (riskMultiplier + 1f) / CostPawns(), 1f / 0.8f)), 1));
            infoRect.y += 20f;
            Widgets.DrawHighlight(infoRect);
            Widgets.Label(infoRect.LeftHalf(),  "VEF.Cost".Translate());
            Widgets.Label(infoRect.RightHalf(), CostBase.ToStringMoney());
            infoRect.y += 20f;
            Widgets.DrawLightHighlight(infoRect);
            Widgets.Label(infoRect.LeftHalf(),  "VEF.RiskMult".Translate());
            Widgets.Label(infoRect.RightHalf(), riskMultiplier.ToStringPercent());
            infoRect.y += 20f;
            Widgets.DrawHighlight(infoRect);
            Widgets.Label(infoRect.LeftHalf(),  "VEF.TotalPrice".Translate());
            Widgets.Label(infoRect.RightHalf(), CostFinal.ToStringMoney());
            if (Widgets.ButtonText(rect.TakeLeftPart(120f).BottomPartPixels(40f), "Cancel".Translate())) OnCancelKeyPressed();
            if (Widgets.ButtonText(rect.TakeRightPart(120f).BottomPartPixels(40f), "Confirm".Translate()))
            {
                if (CostFinal > availableSilver)
                    Messages.Message("NotEnoughSilver".Translate(), MessageTypeDefOf.RejectInput);
                else
                    OnAcceptKeyPressed();
            }

            Text.Font = GameFont.Tiny;
            Widgets.Label(rect.ContractedBy(30f, 0f), "VEF.HiringDesc".Translate(hireable.Key).Colorize(ColoredText.SubtleGrayColor));
            Text.Anchor = anchor;
            Text.Font   = font;
        }

        private void DoHireableFaction(ref Rect inRect, HireableFactionDef def)
        {
            var rect = inRect.TopPartPixels(Mathf.Max(20f + def.pawnKinds.Count * 30f, 120f));
            inRect.yMin += rect.height;
            var titleRect = rect.TakeTopPart(20f);
            var iconRect  = rect.LeftPartPixels(105f).ContractedBy(5f);
            titleRect.x += 115f;
            Text.Anchor =  TextAnchor.MiddleLeft;
            Text.Font   =  GameFont.Tiny;
            var nameRect = new Rect(titleRect);
            Widgets.Label(titleRect, "VEF.Hire".Translate(def.LabelCap));
            titleRect.x     += 200f;
            titleRect.width =  60f;
            Text.Anchor     =  TextAnchor.MiddleCenter;
            var valueRect = new Rect(titleRect);
            Widgets.Label(titleRect, "VEF.Value".Translate());
            titleRect.x     += 100f;
            titleRect.width =  300f;
            var numRect = new Rect(titleRect);
            Text.Font = GameFont.Tiny;
            Widgets.Label(titleRect, "VEF.ChooseNumberOfUnits".Translate().Colorize(ColoredText.SubtleGrayColor));
            Text.Font = GameFont.Small;
            Widgets.DrawLightHighlight(iconRect);
            GUI.color = def.color;
            Widgets.DrawTextureFitted(iconRect, def.Texture, 1f);
            GUI.color = Color.white;
            var highlight = true;
            foreach (PawnKindDef kind in def.pawnKinds)
            {
                nameRect.y  += 20f;
                valueRect.y += 20f;
                numRect.y   += 20f;
                Rect fullRect = new Rect(nameRect.x - 4f, nameRect.y, nameRect.width + valueRect.width + numRect.width, 20f);
                if (highlight) Widgets.DrawHighlight(fullRect);
                highlight   = !highlight;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(nameRect, kind.LabelCap);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(valueRect, kind.combatPower.ToStringByStyle(ToStringStyle.Integer));
                var data   = hireData[kind];
                var amount = data.First;
                var buffer = data.Second;
                UIUtility.DrawCountAdjuster(ref amount, numRect, ref buffer, 0, 99, curFaction != null && curFaction != def, null, Mathf.Max(Mathf.FloorToInt(Mathf.Pow(
                                             (availableSilver / (riskMultiplier + 1f) / CostDays - CostPawns(new HashSet<PawnKindDef> { kind })) /
                                             kind.combatPower, 1f / 1.2f)), 0));
                if (amount != data.First || buffer != data.Second)
                {
                    hireData[kind] = new Pair<int, string>(amount, buffer);
                    if (amount > 0  && curFaction == null) curFaction                                                    = def;
                    if (amount == 0 && curFaction == def && def.pawnKinds.All(pk => hireData[pk].First == 0)) curFaction = null;
                }
            }
        }
    }
}