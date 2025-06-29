﻿
using RimWorld;
using Verse;
namespace VEF.Genes
{
    public class ConditionalStatAffecter_Indoors : ConditionalStatAffecter
    {
        public override string Label => "VGE_StatsReport_Inside".Translate();

        public override bool Applies(StatRequest req)
        {
            if (!ModsConfig.BiotechActive)
            {
                return false;
            }
            if (req.HasThing && req.Thing.Spawned)
            {
                return req.Thing.Map.roofGrid.Roofed(req.Thing.Position);
            }
            return false;
        }
    }
}
