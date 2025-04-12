using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using UnityEngine.Tilemaps;
using Verse;
using Verse.Sound;
using VFECore.UItils;
using static System.Collections.Specialized.BitVector32;

namespace VFECore.Misc.HireableSystem
{
    [DefOf]
    public class QuestDefOf
    {
        public static QuestScriptDef VFECore_Hireables;
    }


    public class Dialog_Hire : Window
    {
        private readonly float availableSilver;
        private readonly Dictionary<PawnKindDef, Pair<int, string>> hireData;
        private readonly float riskMultiplier;
        private readonly Map currentMap;
        private HireableFactionDef hireableFactionDef;
        private HireableFactionDef curFaction;
        private int daysAmount;
        private string daysAmountBuffer;

        private TargetChooser targetChooser;
        private Window pauseWindow = new InvisiblePauseWindow();
        private Orders orders;

        public Dialog_Hire(Thing negotiator, HireableFaction hireableFaction)
        {
            currentMap = negotiator.Map;
            this.hireableFactionDef = hireableFaction.Def;
            hireData = hireableFactionDef.pawnKinds.ToDictionary(def => def, _ => new Pair<int, string>(0, ""));

            closeOnCancel = true;
            forcePause = true;
            closeOnAccept = true;
            soundAmbient = SoundDefOf.RadioComms_Ambience;

            availableSilver = currentMap.listerThings.ThingsOfDef(ThingDefOf.Silver)
                                    .Where(x => !x.Position.Fogged(x.Map) && (currentMap.areaManager.Home[x.Position] || x.IsInAnyStorage())).Sum(t => t.stackCount);
            
            riskMultiplier = hireableFaction.GetFactorForHireableFaction();
            targetChooser = new TargetChooser(currentMap);
            orders = Orders.LandInExistingMap(currentMap.Parent);
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
            List<Thing> silverList = currentMap.listerThings.ThingsOfDef(ThingDefOf.Silver)
                                              .Where(x => !x.Position.Fogged(x.Map) && (currentMap.areaManager.Home[x.Position] || x.IsInAnyStorage())).ToList();
            while (amount > 0)
            {
                Thing silver = silverList.First(t => t.stackCount > 0);
                int amountToTakeFromThisStack = Mathf.Min(amount, silver.stackCount);
                silver.SplitOff(amountToTakeFromThisStack).Destroy();
                amount -= amountToTakeFromThisStack;
            }
        }

        public override void OnAcceptKeyPressed()
        {
            base.OnAcceptKeyPressed();

            if (daysAmount > 0 && hireData.Any(kvp => kvp.Value.First > 0))
            {
                SoundDefOf.ExecuteTrade.PlayOneShotOnCamera();
                removeSilver(Mathf.RoundToInt(CostFinal));

                List<Pair<PawnKindDef, int>> list = [];
                foreach (KeyValuePair<PawnKindDef, Pair<int, string>> kvp in hireData.Where(kvp => kvp.Value.First > 0))
                    list.Add(new Pair<PawnKindDef, int>(kvp.Key, kvp.Value.First));

                HireableUtil.SpawnHiredPawnsQuest(hireableFactionDef, in list, daysAmount, CostFinal, orders);
            }
        }

        public void OnSelectTargetKeyPressed()
        {
            // We hide this dialog and show an invisible Window which also forces the game to pause instead.
            // This is so the game doesn't unpause while choosing a target
            Find.WindowStack.TryRemove(this, false);
            Find.WindowStack.Add(pauseWindow);

            TargetChooser targetChooser = new TargetChooser(currentMap);
            targetChooser.StartChoosingDestination(OnTargetChosen, OnTargettingFinished);
        }

        private void OnTargetChosen(Orders orders)
        {
            Log.Message($"Chosen: {orders.Command} {orders.WorldObject}");
            this.orders = orders;
        }

        private void OnTargettingFinished()
        {
            // Remove the pause window and show out dialog again. (The dialog of course also forces the game paused)
            Find.WindowStack.TryRemove(pauseWindow, false);
            Find.WindowStack.Add(this);
        }

        public override void DoWindowContents(Rect inRect)
        {
            var rect = new Rect(inRect);
            var anchor = Text.Anchor;
            var font = Text.Font;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 40f), "VEF.Hire".Translate(hireableFactionDef.LabelCap));
            Text.Font = GameFont.Small;
            rect.yMin += 40f;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 20f), "VEF.AvailableSilver".Translate(availableSilver.ToStringMoney()));
            rect.yMin += 30f;
            DoHireableFaction(ref rect, hireableFactionDef);
            var breakDownRect = rect.TakeTopPart(100f);
            breakDownRect.xMin += 115f;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            var infoRect = breakDownRect.TopPartPixels(20f);
            Widgets.Label(infoRect.LeftHalf(), "VEF.Breakdown".Translate());
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;
            Widgets.Label(infoRect.RightHalf(), "VEF.LongTerm".Translate().Colorize(ColoredText.SubtleGrayColor));
            Text.Font = GameFont.Small;
            infoRect.y += 20f;
            Widgets.DrawLightHighlight(infoRect);
            Widgets.Label(infoRect.LeftHalf(), "VEF.DayAmount".Translate());
            UIUtility.DrawCountAdjuster(ref daysAmount, infoRect.RightHalf(), ref daysAmountBuffer, 0, 60, false, null,
                                        Mathf.Max(Mathf.FloorToInt(Mathf.Pow(availableSilver / (riskMultiplier + 1f) / CostPawns(), 1f / 0.8f)), 1));
            infoRect.y += 20f;
            Widgets.DrawHighlight(infoRect);
            Widgets.Label(infoRect.LeftHalf(), "VEF.Cost".Translate());
            Widgets.Label(infoRect.RightHalf(), CostBase.ToStringMoney());
            infoRect.y += 20f;
            Widgets.DrawLightHighlight(infoRect);
            Widgets.Label(infoRect.LeftHalf(), "VEF.RiskMult".Translate());
            Widgets.Label(infoRect.RightHalf(), riskMultiplier.ToStringPercent());
            infoRect.y += 20f;
            Widgets.DrawHighlight(infoRect);
            Widgets.Label(infoRect.LeftHalf(), "VEF.TotalPrice".Translate());
            Widgets.Label(infoRect.RightHalf(), CostFinal.ToStringMoney());

            var buttonRect = rect.BottomPartPixels(40f);

            var selectTargetButtonRect = buttonRect.LeftHalf();

            if (Widgets.ButtonText(buttonRect.TakeLeftPart(120f), "Cancel".Translate())) OnCancelKeyPressed();
            if (Widgets.ButtonText(selectTargetButtonRect.TakeRightPart(120f), "Select target".Translate())) OnSelectTargetKeyPressed();
            if (Widgets.ButtonText(buttonRect.TakeRightPart(120f), "Confirm".Translate()))
            {
                if (CostFinal > availableSilver)
                    Messages.Message("NotEnoughSilver".Translate(), MessageTypeDefOf.RejectInput);
                else
                    OnAcceptKeyPressed();
            }

            Text.Font = GameFont.Tiny;
            Widgets.Label(rect.ContractedBy(30f, 0f), "VEF.HiringDesc".Translate(hireableFactionDef.LabelCap).Colorize(ColoredText.SubtleGrayColor));
            Text.Anchor = anchor;
            Text.Font = font;
        }

        private void DoHireableFaction(ref Rect inRect, HireableFactionDef def)
        {
            var rect = inRect.TopPartPixels(Mathf.Max(20f + def.pawnKinds.Count * 30f, 120f));
            inRect.yMin += rect.height;
            var titleRect = rect.TakeTopPart(20f);
            var iconRect = rect.LeftPartPixels(105f).ContractedBy(5f);
            titleRect.x += 115f;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny;
            var nameRect = new Rect(titleRect);
            Widgets.Label(titleRect, "VEF.Hire".Translate(def.LabelCap));
            titleRect.x += 200f;
            titleRect.width = 60f;
            Text.Anchor = TextAnchor.MiddleCenter;
            var valueRect = new Rect(titleRect);
            Widgets.Label(titleRect, "VEF.Value".Translate());
            titleRect.x += 100f;
            titleRect.width = 300f;
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
                nameRect.y += 20f;
                valueRect.y += 20f;
                numRect.y += 20f;
                Rect fullRect = new Rect(nameRect.x - 4f, nameRect.y, nameRect.width + valueRect.width + numRect.width, 20f);
                if (highlight) Widgets.DrawHighlight(fullRect);
                highlight = !highlight;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(nameRect, kind.LabelCap);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(valueRect, kind.combatPower.ToStringByStyle(ToStringStyle.Integer));
                var data = hireData[kind];
                var amount = data.First;
                var buffer = data.Second;
                UIUtility.DrawCountAdjuster(ref amount, numRect, ref buffer, 0, 99, curFaction != null && curFaction != def, null, Mathf.Max(Mathf.FloorToInt(Mathf.Pow(
                                             (availableSilver / (riskMultiplier + 1f) / CostDays - CostPawns(new HashSet<PawnKindDef> { kind })) /
                                             kind.combatPower, 1f / 1.2f)), 0));
                if (amount != data.First || buffer != data.Second)
                {
                    hireData[kind] = new Pair<int, string>(amount, buffer);
                    if (amount > 0 && curFaction == null) curFaction = def;
                    if (amount == 0 && curFaction == def && def.pawnKinds.All(pk => hireData[pk].First == 0)) curFaction = null;
                }
            }
        }
    }

    public class InvisiblePauseWindow : Window
    {

        // Override the window's size to make it effectively "invisible"
        public override Vector2 InitialSize => new Vector2(0f, 0f);  // Make it have no size (invisible)

        public InvisiblePauseWindow()
        {
            // Make the window invisible and prevent it from interacting with the user
            this.doCloseButton = false;
            this.preventSave = true;
            this.forcePause = true;
            this.preventCameraMotion = false;        
        }

        // Override the DoWindowContents to not render anything
        public override void DoWindowContents(Rect inRect)
        {
            // This window draws nothing, but forces the game to pause
            // No content is drawn here
        }
    }
}