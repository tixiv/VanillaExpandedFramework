using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;
using VFECore.Misc;
using VFECore.Misc.HireableSystem;

namespace VFECore
{
    using UnityEngine;

    public class HiringContractTracker : WorldComponent
    {
        private static HiringContractTracker cachedTracker = null;

        public static HiringContractTracker Get()
        {
            if (cachedTracker == null || cachedTracker.world != Find.World)
                cachedTracker = Find.World.GetComponent<HiringContractTracker>();

            return cachedTracker;
        }

        // Here is where the instances of our HireableFactions for each HireableFactionDef
        // live. They are deep saved in this class.

        private Dictionary<HireableFactionDef, HireableFaction> factions = [];

        static int count;
        public HiringContractTracker(World world) : base(world)
        {
            Log.Message($"HiringContractTracker Constructor called {count++}");
        }

        public IEnumerable<ICommunicable> GetComTargets()
        {
            foreach (var def in HireableSystemStaticInitialization.Hireables)                
                yield return factions[def];
        }

        private static IEnumerable<Quest> getOngoingQuests() => Find.QuestManager.QuestsListForReading.Where(q => q.State == QuestState.Ongoing && q.root.defName == "VFECore_Hireables");

        public static IEnumerable<ContractInfo> GetOngoingContracts()
        {
            foreach (var q in getOngoingQuests())
                foreach (QuestPart_HireableContract qp in q.PartsListForReading.OfType<QuestPart_HireableContract>())
                    yield return qp.contractInfo;            
        }

        public static bool IsHired(Pawn pawn) => GetOngoingContracts().Any(c => c.pawns.Contains(pawn));

        public void NotifyContractEnded(HireableFactionDef hireableFactionDef, int numDead, int numKidnapped)
        {
            factions[hireableFactionDef].NotifyLosses(numDead + numKidnapped);
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            /* 
            if (Find.TickManager.TicksAbs % 150 == 0 && Find.TickManager.TicksAbs > endTicks && this.pawns.Any())
                this.EndContract();
            */
        }

        public void EndContract(HireableFactionDef hireableFactionDef)
        {
        }

        /*
        public void EndContract()
        {
            var deadPeople = 0;

            for (int index = pawns.Count - 1; index >= 0; index--)
            {
                Pawn pawn = pawns[index];
                
                if (pawn == null || pawn.Dead || Find.FactionManager.AllFactionsListForReading.Any(f => f.kidnapped.KidnappedPawnsListForReading.Contains(pawn)))
                {
                    deadPeople++;
                    this.pawns.Remove(pawn);
                }
                else if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
                {
                    if (pawn.Map != null && pawn.CurJobDef != VFEDefOf.VFEC_LeaveMap)
                    {
                        pawn.jobs.StopAll();
                        if (!CellFinder.TryFindRandomPawnExitCell(pawn, out IntVec3 exit))
                            if (!CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => !pawn.Map.roofGrid.Roofed(c) && c.WalkableBy(pawn.Map, pawn) &&
                                                                                     pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly, canBashDoors: true, canBashFences: true,
                                                                                                   TraverseMode.PassDoors), pawn.Map, 0f, out exit))
                            {
                                this.BreakContract();
                                return;
                            }

                        pawn.jobs.TryTakeOrderedJob(new Job(VFEDefOf.VFEC_LeaveMap, exit));
                    }
                    else if (pawn.GetCaravan() != null)
                    {
                        pawn.GetCaravan().RemovePawn(pawn);
                        this.pawns.Remove(pawn);
                    }
                }

                if (deadPeople > 0)
                {
                    if (!deadCount.ContainsKey(hireable))
                        deadCount.Add(hireable, new List<ExposablePair>());

                    deadCount[hireable].Add(new ExposablePair(deadPeople, Find.TickManager.TicksAbs + GenDate.TicksPerYear));
                }
            }

            if (this.pawns.Count <= 0)
                this.hireable = null;
        }
        */

        public static void breakContract()
        {
        
        }

        /*
        public void BreakContract()
        {
            if (this.pawns.Count > 0)
            {
                if (!deadCount.ContainsKey(hireable))
                    deadCount.Add(hireable, new List<ExposablePair>());

                deadCount[hireable].Add(new ExposablePair(this.pawns.Count, Find.TickManager.TicksAbs + GenDate.TicksPerYear));

                foreach (Pawn pawn in this.pawns)
                {
                    if (!pawn.Dead)
                    {
                        if (pawn.Map != null)
                        {
                            pawn.jobs.StopAll();
                            pawn.SetFaction(Faction.OfAncientsHostile);
                            RaidStrategyDefOf.ImmediateAttack.Worker.MakeLords(new IncidentParms() { target = pawn.Map, faction = Faction.OfAncientsHostile, canTimeoutOrFlee = false },
                                                                               new List<Pawn>() { pawn });
                        }
                        else if (pawn.GetCaravan() != null)
                        {
                            pawn.GetCaravan().RemovePawn(pawn);
                        }
                    }
                }
            }

            this.hireable = null;
            this.pawns.Clear();
        }
        */

        private class LegacySavegameData
        {
            public int endTicks;
            public HireableFactionDef factionDef;
            public List<Pawn> pawns = [];
            public float price;
            public string hireable;

            public bool Valid;

            public void ConvertToQuest()
            {
                HireableUtil.SpawnHiredPawnsQuest(factionDef, null, endTicks - Find.TickManager.TicksAbs, price, Orders.ConvertSavegame());
            }

            public void ExposeData()
            {
                // Here we load the data from an old savegame to convert it to the new system

                if (Scribe.mode != LoadSaveMode.Saving)
                {
                    Scribe_Values.Look(ref endTicks, nameof(endTicks));
                    Scribe_Values.Look(ref price, "price");
                    Scribe_Values.Look(ref hireable, "hireable");
                    Scribe_Defs.Look(ref factionDef, "faction");
                    Scribe_Collections.Look(ref pawns, nameof(pawns), LookMode.Reference);

                    // Okay, so the way deadCount was saved was really messy. And broken, two!
                    // You never had any buisiness history whith the old version after loading.

                    // So we exposed a temporary 'List<Hireable>' with 'LookMode.Reference'.
                    // Because of the reference look mode, this means that the 'Hireable' instance
                    // is expected to be deepsaved somewhere else. That was not done by the old code,
                    // Instead it used a harmony patch to make the 'LoadedObjectDirectory.Clear()' method
                    // deep inside the scribe system have the side effect of not only clearing the directory,
                    // but also inserting our keys.... Yuck ... terrible hack. So if you were wondering while
                    // with VFE installed you got messages that stuff isn't deepsaved, it is because of this hack!

                    // Sadly no meaningfull buisiness history is ever saved, it saves *something* if you save
                    // after killing hireables, but it doesn't really have anything to do with what happened.
                    // Loading was totally broken by the way and always cleared the history. So not being
                    // able to convert it here is not a big loss.

                    if (Scribe.mode == LoadSaveMode.PostLoadInit)
                    {
                        Log.Message($"endTicks={endTicks}, price={price}, factionDef={factionDef}, hireable={hireable}");

                        foreach (Pawn p in pawns)
                        {
                            Log.Message($"pawn={p.Name}");
                        }

                        pawns.RemoveWhere(p => p == null);

                        // Check whether we have an active contract with pawns that the player is still controlling
                        // The pawns might be leaving, but they would still belong to 'Faction.OfPlayer'
                        if (hireable != null && pawns.Any(p => !p.Dead && p.Faction != null && p.Faction == Faction.OfPlayer))
                            Valid = true;
                    }
                }
            }
        }

        LegacySavegameData legacyData = new LegacySavegameData();

        public override void ExposeData()
        {
            base.ExposeData();

            Log.Message($"Scribe mode = {Scribe.mode}");

            // Handle loading old savegames
            legacyData.ExposeData();

            // Okay, I finally understood how scribe works.
            // Here we call ExposeData on a 'Dictionary'. A 'Dictionary' is a kind of a map, so this one has a 'Key'
            // that maps to a 'Value'. For each 'Key' there is one 'Value'. In C++ this would be 'std::map'.
            // Here 'Scribe_Collections' gives us two arguments for the 'LookMode'. The first one is for the 'Key',
            // the second one is for the 'Value'. We give 'LookMode.Def' for the key, because it is a 'HireableFactionDef'
            //  from the XML files. We give 'LookMode.Deep' for the value because it implements 'IExposable'. Here the
            // 'Value' also implements 'ILoadReferenceable'. That one will also only be tracked by Scribe if we call it here
            // with 'LookMode.Deep'.
            // By the way: 'ILoadRefernceable' on the 'HireableFaction' is mainly needed for the usecase when you order a
            // colonist to use the comms console, and then you save the game while they are walking to it. The job needs to have
            // the 'ICommunicable' that the colonist is going to use to be load referenceable so he can complete the job after
            // reloading the savegame.

            Scribe_Collections.Look(ref factions, nameof(factions), LookMode.Def, LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Here we make sure that if we load a savegame that doesn't have this stuff yet (mod just got installed)
                // That we add them if they were not loaded from the savegame.
                SanitizeHireableFactions();
            }
        }

        private void SanitizeHireableFactions()
        {
            if (factions == null)
                factions = [];

            // If any hireable factions got removed, we also remove them here.

            foreach (var f in factions)
                if (!HireableSystemStaticInitialization.Hireables.Contains(f.Key))
                    factions.Remove(f.Key);

            // If any new 'HierableFactionDef' have been created we add a 'HireableFaction' instance

            foreach (var def in HireableSystemStaticInitialization.Hireables)
                if (!factions.ContainsKey(def))
                    factions.Add(def, new HireableFaction(def));
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            Log.Message("Finalize init called");
            SanitizeHireableFactions();
        }
    }
}
