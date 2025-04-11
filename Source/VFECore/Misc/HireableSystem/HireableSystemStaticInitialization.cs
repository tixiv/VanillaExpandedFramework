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

namespace VFECore.Misc.HireableSystem
{
    [StaticConstructorOnStartup]
    public static class HireableSystemStaticInitialization
    {
        public static List<Hireable> Hireables;

        static HireableSystemStaticInitialization()
        {
            Hireables = DefDatabase<HireableFactionDef>.AllDefs.GroupBy(def => def.commTag).Select(group => new Hireable(group.Key, group.ToList())).ToList();
            if (Hireables.Any())
            {
                VFECore.harmonyInstance.Patch(AccessTools.Method(typeof(Building_CommsConsole), nameof(Building_CommsConsole.GetCommTargets)),
                                              postfix: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(GetCommTargets_Postfix)));
                VFECore.harmonyInstance.Patch(AccessTools.Method(typeof(LoadedObjectDirectory), "Clear"),
                                              postfix: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(AddHireablesToLoadedObjectDirectory)));
                VFECore.harmonyInstance.Patch(AccessTools.Method(typeof(EquipmentUtility), nameof(EquipmentUtility.QuestLodgerCanUnequip)),
                                              postfix: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(QuestLodgerCanUnequip_Postfix)));
                VFECore.harmonyInstance.Patch(AccessTools.Method(typeof(CaravanFormingUtility), nameof(CaravanFormingUtility.AllSendablePawns)),
                                              transpiler: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(Patch_Quest_IsLodger_Transpiler)));
                VFECore.harmonyInstance.Patch(AccessTools.Method(typeof(CompAbilityEffect_Farskip), nameof(CompAbilityEffect_Farskip.ConfirmationDialog), [typeof(GlobalTargetInfo), typeof(Action)]),
                                              prefix: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(Farskip_ConfirmationDialog_Prefix)));


                //VFECore.harmonyInstance.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.CheckAcceptArrest)),
                //                              postfix: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(CheckAcceptArrestPostfix)));
                //VFECore.harmonyInstance.Patch(AccessTools.Method(typeof(BillUtility), nameof(BillUtility.IsSurgeryViolationOnExtraFactionMember)),
                //                              postfix: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(IsSurgeryViolation_Postfix)));


                var subtype = typeof(CompAbilityEffect_Farskip).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).FirstOrDefault(t => t.Name.Contains("<PawnsToSkip>"));
                if (subtype != null)
                    VFECore.harmonyInstance.Patch(AccessTools.Method(subtype, "MoveNext"),
                                              transpiler: new HarmonyMethod(typeof(HireableSystemStaticInitialization), nameof(Patch_Quest_IsLodger_Transpiler)));
                else
                    Log.Warning("Couldn't apply the patch that allows hireables to be farskipped");

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

        public static void AddHireablesToLoadedObjectDirectory(LoadedObjectDirectory __instance)
        {
            foreach (var hireable in Hireables)
                __instance.RegisterLoaded(hireable);
        }

        // Make our hireables lodgers.
        // Not needed anymore since hireables are "real" quest lodgers now through QuestPart_ExtraFaction
        public static void IsQuestLodger_Postfix(Pawn p, ref bool __result)
        {
            __result = __result || HiringContractTracker.IsHired(p);
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

        public static void CheckAcceptArrestPostfix(Pawn __instance, ref bool __result)
        {
            if (HiringContractTracker.IsHired(__instance))
            {
                HiringContractTracker.breakContract();
                __result = false;
            }
        }

        public static void IsSurgeryViolation_Postfix(Bill_Medical bill, ref bool __result)
        {
            __result = __result || (HiringContractTracker.IsHired(bill.GiverPawn) && bill.recipe.Worker.IsViolationOnPawn(bill.GiverPawn, bill.Part, Faction.OfPlayer));
        }
    }

    public class Hireable : IGrouping<string, HireableFactionDef>, ILoadReferenceable
    {
        private static readonly AccessTools.FieldRef<CrossRefHandler, LoadedObjectDirectory> loadedObjectInfo =
            AccessTools.FieldRefAccess<CrossRefHandler, LoadedObjectDirectory>("loadedObjectDirectory");

        private readonly List<HireableFactionDef> factions;

        public Hireable(string label, List<HireableFactionDef> list)
        {
            Key      = label;
            factions = list;

            loadedObjectInfo(Scribe.loader.crossRefs).RegisterLoaded(this);
        }

        public IEnumerator<HireableFactionDef> GetEnumerator() => factions.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public string Key               { get; }
        public string GetUniqueLoadID() => $"{nameof(Hireable)}_{Key}";
    }
}