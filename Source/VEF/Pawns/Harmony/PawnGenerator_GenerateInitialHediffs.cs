﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace VEF.Pawns
{

    public static class Patch_PawnGenerator
    {

      

        [HarmonyPatch(typeof(PawnGenerator), "GenerateInitialHediffs")]
        public static class VanillaExpandedFramework_PawnGenerator_GenerateInitialHediffs
        {
            public static void Postfix(Pawn pawn)
            {
                pawn.story?.AllBackstories?.OfType<VEBackstoryDef>().SelectMany(selector: bd => bd.forcedHediffs).Select(DefDatabase<HediffDef>.GetNamedSilentFail).
                     Do(action: hd =>
                                {
                                    BodyPartRecord bodyPartRecord = null;
                                    DefDatabase<RecipeDef>.AllDefs.FirstOrDefault(predicate: rd => rd.addsHediff == hd)?.appliedOnFixedBodyParts.SelectMany(selector: bpd =>
                                        pawn.health.hediffSet.GetNotMissingParts().Where(predicate: bpr => bpr.def == bpd && !pawn.health.hediffSet.hediffs.Any(predicate: h => h.def == hd && h.Part == bpr)))
                                            .TryRandomElement(out bodyPartRecord);
                                    pawn.health.AddHediff(hd, bodyPartRecord);
                                });
            }
        }
    }
}