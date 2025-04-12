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

        private Dictionary<HireableFactionDef, CommTarget_ViewContract> commTargetsViewContract = [];
        private Dictionary<HireableFactionDef, CommTarget_Hire> commTargetsHire = [];

        public HiringContractTracker(World world) : base(world)
        {
        }

        // Making history events generic like this will help in the future to add new ones
        // without breaking upwards compatability to a new version of the mod
        private class HistoryEvent : IExposable
        {
            public HireableFactionDef faction;
            public int timestamp;

            public virtual void ExposeData()
            {
                Scribe_Defs.Look(ref faction, nameof(faction));
                Scribe_Values.Look(ref timestamp, nameof(timestamp));
            }
        }

        private class HistoryEvent_PeopleKilled : HistoryEvent
        {
            public int numKilled;
            public override void ExposeData()
            {
                base.ExposeData();
                Scribe_Values.Look(ref numKilled, nameof(numKilled));
            }
        }

        private List<HistoryEvent> HiringHistory = [];

        private ICommunicable GetCommTarget_ViewContract(HireableFactionDef hireableFactionDef)
        {
            return commTargetsViewContract[hireableFactionDef];
        }

        private ICommunicable GetCommTarget_Hire(HireableFactionDef hireableFactionDef)
        {
            return commTargetsHire[hireableFactionDef];
        }

        public IEnumerable<ICommunicable> GetComTargets()
        {
            var ongoingContracts = GetOngoingContracts();

            foreach (var hireableFactionDef in HireableSystemStaticInitialization.Hireables)
            {
                if (GetOngoingContracts().Any(c => c.hireableFactionDef == hireableFactionDef))
                {
                    yield return GetCommTarget_ViewContract(hireableFactionDef);
                }
                else
                {
                    yield return GetCommTarget_Hire(hireableFactionDef);
                }
            }
        }

        private static IEnumerable<Quest> getOngoingQuests() => Find.QuestManager.QuestsListForReading.Where(q => q.State == QuestState.Ongoing && q.root.defName == "VFECore_Hireables");

        public static IEnumerable<ContractInfo> GetOngoingContracts()
        {
            foreach (var q in getOngoingQuests())
                foreach (QuestPart_HireableContract qp in q.PartsListForReading.OfType<QuestPart_HireableContract>())
                    yield return qp.contractInfo;            
        }

        public static bool IsHired(Pawn pawn) => GetOngoingContracts().Any(c => c.pawns.Contains(pawn));

        private void AddLossesForFaction(HireableFactionDef hireableFactionDef, int numLost)
        {
            HiringHistory.Add(new HistoryEvent_PeopleKilled
            {
                timestamp = Find.TickManager.TicksGame,
                faction = hireableFactionDef,
                numKilled = numLost
            });
        }

        public void NotifyContractEnded(HireableFactionDef hireableFactionDef, int numDead, int numKidnapped)
        {
            AddLossesForFaction(hireableFactionDef, numDead + numKidnapped);
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

        public float GetFactorForHireableFaction(HireableFactionDef hireableFactionDef)
        {
            int recentlyKilled = 0;

            foreach (var historyEvent in HiringHistory.OfType<HistoryEvent_PeopleKilled>().Where(h => h.faction == hireableFactionDef))
            {
                if (Find.TickManager.TicksGame > historyEvent.timestamp + GenDate.TicksPerYear)
                    HiringHistory.Remove(historyEvent);
                else
                    recentlyKilled += historyEvent.numKilled;
            }

            Log.Message($"GetFactorForHireableFaction {hireableFactionDef.LabelCap}: recentlyKilled={recentlyKilled}");

            return 0.05f * recentlyKilled;
        }

        // These variables will not be used anymore. They are only used to convert existing savegames to the new quest based hireables
        public class legacyData
        {
            public int endTicks;
            public HireableFactionDef factionDef;
            public List<Pawn> pawns = [];
            public float price;
        }

        private void loadLegacyStuffsBarfoo()
        {
            //public Dictionary<Hireable, List<ExposablePair>>
            //    deadCount = new Dictionary<Hireable, List<ExposablePair>>(); //the pair being amount of dead people and at what tick it expires

            // Scribe_Values.Look(ref endTicks, nameof(endTicks));

            // Scribe_Collections.Look(ref this.pawns, nameof(this.pawns), LookMode.Reference);

            // Scribe_References.Look(ref hireable, nameof(hireable));

            /*
            var deadCountKey = new List<Hireable>(deadCount.Keys);
            Scribe_Collections.Look(ref deadCountKey, nameof(deadCountKey), LookMode.Reference);
            var deadCountValue = new List<List<ExposablePair>>(deadCount.Values);
            for (var i = 0; i < deadCountKey.Count; i++)
            {
                var exposablePairs = deadCountValue.Count > i ? deadCountValue[i] : new List<ExposablePair>();
                Scribe_Collections.Look(ref exposablePairs, nameof(exposablePairs) + i, LookMode.Deep);

                if (deadCountValue.Count > i)
                    deadCountValue[i] = exposablePairs;
                else
                    deadCountValue.Add(exposablePairs);
            }


            deadCount.Clear();
            for (var index = 0; index < deadCountKey.Count; index++)
                deadCount.Add(deadCountKey[index], deadCountValue[index]);

            */

            // Scribe_Values.Look(ref price, "price");
            // Scribe_Defs.Look(ref factionDef, "faction");
        }



        public override void ExposeData()
        {
            base.ExposeData();

            Log.Message($"Scribe mode = {Scribe.mode}");

            // Okay, I finally understood how scribe works. When using 'Scribe_Collections.Look' with a 'List'
            // We get one 'LookMode' argument, that one is for the values in the 'List'. Lists don't have keys.
            // Using 'LookMode.Deep' here means we are calling the 'ExposeData()' method on the values in the list
            // to add them to the savegame. That is exactly what we want, because any future history event can just
            // implement ExposeData() to save it's state.

            Scribe_Collections.Look(ref HiringHistory, nameof(HiringHistory), LookMode.Deep);

            // Here we call ExposeData on a 'Dictionary'. A 'Dictionary' is a kind of a map, so this one has a 'Key'
            // that maps to a 'Value'. For each 'Key' there is one 'Value'. In C++ this would be 'std::map'.
            // Here 'Scribe_Collections' gives us two arguments for the 'LookMode'. The first one is for the 'Key',
            // the second one is for the 'Value'. We give 'LookMode.Def' for the key, because it is a 'Def' from
            // the XML files. We give 'LookMode.Deep' for the value because it implements 'IExposable'. Here the
            // 'Value' also implements 'ILoadReferenceable'. That one will also only be tracked if we call it here
            // with 'LookMode.Deep'.
            // By the way: This stuff mainly needs to get saved to the savegame in case you order a colonist to use the
            // comms console, and then you save the game before they walk to it. The job needs to have the ICommunicable
            // that the colonist is going to use to be load referenceable so he can complete the job after reload.

            Scribe_Collections.Look(ref commTargetsHire,         nameof(commTargetsHire),         LookMode.Def, LookMode.Deep);
            Scribe_Collections.Look(ref commTargetsViewContract, nameof(commTargetsViewContract), LookMode.Def, LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                // Here we make sure that if we load a savegame that doesn't have this stuff yet (mod just got installed)
                // That we add them if they were not loaded from the savegame.

                if (commTargetsHire == null)
                    commTargetsHire = [];
                if (commTargetsViewContract == null)
                    commTargetsViewContract = [];

                foreach (var hireableFactionDef in HireableSystemStaticInitialization.Hireables)
                {
                    // If any new hireable faction was added we add their commTargets here.

                    if (!commTargetsHire.ContainsKey(hireableFactionDef))
                        commTargetsHire.Add(hireableFactionDef, new CommTarget_Hire(hireableFactionDef));

                    if (!commTargetsViewContract.ContainsKey(hireableFactionDef))
                        commTargetsViewContract.Add(hireableFactionDef, new CommTarget_ViewContract(hireableFactionDef));
                }
            }
        }
    }

    /*
    public class ExposablePair : IExposable
    {
        public object key;
        public object value;


        public ExposablePair(object key, object value)
        {
            this.key   = key;
            this.value = value;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref key,   nameof(key));
            Scribe_Values.Look(ref value, nameof(value));
        }
    }
    */
}
