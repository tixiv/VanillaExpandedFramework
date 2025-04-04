using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VEF.Planet
{
    [StaticConstructorOnStartup]
    public static class HireableSystemStaticInitialization
    {
        public static List<HireableFactionDef> Hireables;

        static HireableSystemStaticInitialization()
        {
            Hireables = DefDatabase<HireableFactionDef>.AllDefs.ToList();

            if (Hireables.Any())
            {
                VEF_Mod.harmonyInstance.Patch(AccessTools.Method(typeof(Building_CommsConsole), nameof(Building_CommsConsole.GetCommTargets)),
                                              postfix: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(GetCommTargets_Postfix)));                
                VEF_Mod.harmonyInstance.Patch(AccessTools.Method(typeof(EquipmentUtility), nameof(EquipmentUtility.QuestLodgerCanUnequip)),
                                              postfix: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(QuestLodgerCanUnequip_Postfix)));
                VEF_Mod.harmonyInstance.Patch(AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.AllSendablePawns)),
                                              transpiler: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(Patch_Quest_IsLodger_Transpiler)));
                VEF_Mod.harmonyInstance.Patch(AccessTools.Method(typeof(CompAbilityEffect_Farskip), nameof(CompAbilityEffect_Farskip.ConfirmationDialog), [typeof(GlobalTargetInfo), typeof(Action)]),
                                              prefix: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(Farskip_ConfirmationDialog_Prefix)));
                VEF_Mod.harmonyInstance.Patch(AccessTools.Method(typeof(WorldTargeter), nameof(WorldTargeter.StopTargeting)),
                                              postfix: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(WorldTargeter_StopTargeting_Postfix)));
                VEF_Mod.harmonyInstance.Patch(AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(CompAbilityEffect_Farskip), "PawnsToSkip")),
                                              transpiler: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(Patch_Quest_IsLodger_Transpiler)));

                // Patch to keep temporary maps from closing.
                // VEF_Mod.harmonyInstance.Patch(AccessTools.Method(typeof(MapPawns), "IsValidColonyPawn"),
                //                               prefix: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(IsValidColonyPawn_Prefix)));
            }
        }

        // Patch CommsConsole to have our CommTargets
        public static IEnumerable<ICommunicable> GetCommTargets_Postfix(IEnumerable<ICommunicable> communicables)
        {
            var contractTracker = HiringContractTracker.Get();

            if (contractTracker != null) {
                return communicables.Concat(contractTracker.GetComTargets());
            }

            return communicables;
        }

        // We lock the weapons on our hireables.
        public static void QuestLodgerCanUnequip_Postfix(Pawn pawn, ref bool __result)
        {
            // Note from Tixiv:
            // I don't understand why 'pawn.RaceProps.Humanlike' is in this check. It seems
            // wrong because we just want to lock the gear on our hireables. Why should everything
            // that is non Humanlike have their gear locked also? Is this even called on anything
            // non human? I would assume it only gets called on player pawns anyway.

            __result = __result && pawn.RaceProps.Humanlike && !HiringContractTracker.IsHired(pawn);
        }

        // We allow our hireables to be taken onto caravans. And farskipped.
        public static IEnumerable<CodeInstruction> Patch_Quest_IsLodger_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var questLodger = AccessTools.Method(typeof(QuestUtility), nameof(QuestUtility.IsQuestLodger));
            bool patchApplied = false;

            foreach (var instruction in instructions)
                if (instruction.Calls(questLodger))
                {
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return instruction;
                    yield return CodeInstruction.Call(typeof(HireableSystemStaticInitialization), nameof(HireablesAreNotLodgersHelper));
                    patchApplied = true;
                }
                else
                    yield return instruction;

            if (!patchApplied)
                Log.Warning("Transpiler was unabe to apply patch");
        }

        public static bool HireablesAreNotLodgersHelper(Pawn pawn, bool questLodger) =>
            questLodger && !HiringContractTracker.IsHired(pawn);

        // Don't show confirmation dialog that says that the related quest will fail if you farskip hireables that are in a caravan
        public static bool Farskip_ConfirmationDialog_Prefix(ref Window __result, CompAbilityEffect_Farskip __instance, GlobalTargetInfo target, Action confirmAction)
        {
            // Need to call this private method the hard way:
            var method = AccessTools.Method(typeof(CompAbilityEffect_Farskip), "PawnsToSkip");
            var pawns = (IEnumerable<Pawn>)method.Invoke(__instance, null);

            // Check for any real quest lodgers (non hired)
            if (pawns.Any(p => p.IsQuestLodger() && !HiringContractTracker.IsHired(p)))
            {
                // Do original confirmation box check in that case, player is actually attempting to skip a lodger
                return true;
            }

            // Do wee need to suppress the original check at all?
            if (pawns.Any(p => HiringContractTracker.IsHired(p)))
            {
                // Yup, only hireables to be skipped, no confirmation window, don't run the original check
                __result = null;
                return false;
            }

            // Doesn't hurt to run the original check anyway. This is probably more compatible in case
            // more conditions get added to it in a future Rimworld version
            return true;
        }

        // Sadly I found no cleaner way to get a signal from WorldTargeter when targeting is canceled
        public static void WorldTargeter_StopTargeting_Postfix(WorldTargeter __instance)
        {
            TargetChooser.TargetingFinishedCallback();
        }

        public static bool IsValidColonyPawn_Prefix(Pawn pawn, ref bool __result)
        {
            __result = true;
            return false;
        }
    }
}