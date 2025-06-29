﻿using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace VEF.Pawns
{
    [HarmonyPatch(typeof(MemoryThoughtHandler), "TryGainMemory", new Type[]
    {
                typeof(Thought_Memory),
                typeof(Pawn)
    })]
    public static class VanillaExpandedFramework_MemoryThoughtHandler_TryGainMemory_Patch
    {
        private static void Postfix(MemoryThoughtHandler __instance, ref Thought_Memory newThought, Pawn otherPawn)
        {
            if (newThought.pawn != null)
            {
                var options = newThought.def.GetModExtension<ThoughtExtensions>();
                if (options != null)
                {
                    if (options.removeThoughtsWhenAdded != null)
                    {
                        foreach (var thoughtDef in options.removeThoughtsWhenAdded)
                        {
                            __instance.pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(thoughtDef);
                        }
                    }
                }

                var factor = newThought.CurStage.baseMoodEffect switch
                {
                    > 0 => __instance.pawn.GetStatValue(VEFDefOf.VEF_PositiveThoughtDurationFactor),
                    < 0 => __instance.pawn.GetStatValue(VEFDefOf.VEF_NegativeThoughtDurationFactor),
                    _   => __instance.pawn.GetStatValue(VEFDefOf.VEF_NeutralThoughtDurationFactor),
                };

                newThought.durationTicksOverride = Mathf.RoundToInt(newThought.DurationTicks * factor);
            }
        }
    }
}
