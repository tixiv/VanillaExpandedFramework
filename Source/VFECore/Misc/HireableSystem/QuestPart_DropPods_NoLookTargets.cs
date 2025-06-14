using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace VFECore.Misc.HireableSystem
{
    public class QuestPart_DropPods_NoLookTargets : QuestPart_DropPods
    {
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                yield break;
            }
        }
    }


    public static partial class QuestGen_Hireable
    {
        public static QuestPart_DropPods_NoLookTargets DropPods_NoLookTargets(this Quest quest, MapParent mapParent, IEnumerable<Thing> contents, string customLetterLabel = null, RulePack customLetterLabelRules = null, string customLetterText = null, RulePack customLetterTextRules = null, bool? sendStandardLetter = true, bool useTradeDropSpot = false, bool joinPlayer = false, bool makePrisoners = false, string inSignal = null, IEnumerable<Thing> thingsToExcludeFromHyperlinks = null, QuestPart.SignalListenMode signalListenMode = QuestPart.SignalListenMode.OngoingOnly, IntVec3? dropSpot = null, bool destroyItemsOnCleanup = true, bool dropAllInSamePod = false, bool allowFogged = false, Faction faction = null)
        {
            QuestPart_DropPods_NoLookTargets dropPods = new QuestPart_DropPods_NoLookTargets();
            dropPods.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal) ?? QuestGen.slate.Get<string>("inSignal");
            dropPods.signalListenMode = signalListenMode;
            if (!customLetterLabel.NullOrEmpty() || customLetterLabelRules != null)
            {
                QuestGen.AddTextRequest("root", delegate (string x)
                {
                    dropPods.customLetterLabel = x;
                }, QuestGenUtility.MergeRules(customLetterLabelRules, customLetterLabel, "root"));
            }

            if (!customLetterText.NullOrEmpty() || customLetterTextRules != null)
            {
                QuestGen.AddTextRequest("root", delegate (string x)
                {
                    dropPods.customLetterText = x;
                }, QuestGenUtility.MergeRules(customLetterTextRules, customLetterText, "root"));
            }

            dropPods.sendStandardLetter = sendStandardLetter ?? dropPods.sendStandardLetter;
            dropPods.useTradeDropSpot = useTradeDropSpot;
            dropPods.joinPlayer = joinPlayer;
            dropPods.makePrisoners = makePrisoners;
            dropPods.mapParent = mapParent;
            dropPods.Things = contents;
            dropPods.destroyItemsOnCleanup = destroyItemsOnCleanup;
            dropPods.dropAllInSamePod = dropAllInSamePod;
            dropPods.allowFogged = allowFogged;
            dropPods.faction = faction;
            if (dropSpot.HasValue)
            {
                dropPods.dropSpot = dropSpot.Value;
            }

            if (thingsToExcludeFromHyperlinks != null)
            {
                dropPods.thingsToExcludeFromHyperlinks.AddRange(thingsToExcludeFromHyperlinks.Select((Thing t) => t.GetInnerIfMinified().def));
            }

            QuestGen.quest.AddPart(dropPods);
            return dropPods;
        }
    }
}
