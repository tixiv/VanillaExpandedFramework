﻿using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;
using System;

namespace VEF.Maps
{

    public static class OptionalFeatures_TileMutatorMechanics
    {
        public static void ApplyFeature(Harmony harm)
        {

            harm.Patch(AccessTools.Method(typeof(Game), "InitNewGame"),
                transpiler: new HarmonyMethod(typeof(VanillaExpandedFramework_Game_InitNewGame_Patch), "TweakMapSizes"));

            harm.Patch(AccessTools.Method(typeof(GetOrGenerateMapUtility), "GetOrGenerateMap", new Type[] { typeof(PlanetTile), typeof(IntVec3), typeof(WorldObjectDef), typeof(IEnumerable<GenStepWithParams>), typeof(bool) }),
               prefix: new HarmonyMethod(typeof(VanillaExpandedFramework_GetOrGenerateMapUtility_GetOrGenerateMap_Patch), "TweakMapSizes"));

            harm.Patch(AccessTools.Method(typeof(MapGenerator), "GenerateMap"),
               postfix: new HarmonyMethod(typeof(VanillaExpandedFramework_MapGenerator_GenerateMap_Patch), "DoObjectSpawnsDefMapSpawns"));

            harm.Patch(AccessTools.Method(typeof(WildAnimalSpawner), "SpawnRandomWildAnimalAt"),
               postfix: new HarmonyMethod(typeof(VanillaExpandedFramework_WildAnimalSpawner_SpawnRandomWildAnimalAt_Patch), "AddExtraAnimalsByMutator"));
        }
    }
}
