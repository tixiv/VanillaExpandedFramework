﻿using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VEF.Apparels
{
	[HarmonyPatch(typeof(ThingSelectionUtility), "SelectableByMapClick")]
	public static class VanillaExpandedFramework_ThingSelectionUtility_Patch
    {
		[HarmonyPostfix]
		public static void GhillieException(ref bool __result, Thing t)
		{
			Pawn pawn;
			bool flag;
			if ((pawn = (t as Pawn)) != null && (pawn.Faction == null || (pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer))) && pawn.apparel != null && pawn.apparel.WornApparel != null)
			{
				flag = StaticCollectionsClass.camouflaged_pawns.Contains(pawn);
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			if (flag2)
			{
				__result = false;
			}
		}
	}
}