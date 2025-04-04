using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace VEF.Planet
{
    public class HireableFaction : ICommunicable, IExposable, ILoadReferenceable
    {

        public HireableFactionDef Def;

        public HireableFaction() { }

        public HireableFaction(HireableFactionDef def)
        {
            this.Def = def;
        }

        // Making history events generic like this will help in the future to add new ones
        // without breaking upwards compatability to a new version of the mod
        private class HistoryEvent : IExposable
        {
            public int timestamp;

            public virtual void ExposeData()
            {
                Scribe_Values.Look(ref timestamp, nameof(timestamp));
            }
        }

        private class HistoryEvent_Killed : HistoryEvent { }
        private class HistoryEvent_Kidnapped : HistoryEvent { }
        private class HistoryEvent_Downed : HistoryEvent { }



        private List<HistoryEvent> HiringHistory = [];

        public void NotifyPawnKilled()
        {
            Log.Message("HireableFaction.NotifyPawnKilled");

            HiringHistory.Add(new HistoryEvent_Killed
            {
                timestamp = Find.TickManager.TicksGame,
            });
        }

        public void NotifyPawnKidnapped()
        {
            HiringHistory.Add(new HistoryEvent_Kidnapped
            {
                timestamp = Find.TickManager.TicksGame,
            });
        }

        public float GetFactorForHireableFaction()
        {
            HiringHistory.RemoveWhere(h => h is HistoryEvent_Killed && (Find.TickManager.TicksGame > h.timestamp + GenDate.TicksPerYear));
            HiringHistory.RemoveWhere(h => h is HistoryEvent_Kidnapped && (Find.TickManager.TicksGame > h.timestamp + GenDate.TicksPerYear));
            HiringHistory.RemoveWhere(h => h is HistoryEvent_Downed && (Find.TickManager.TicksGame > h.timestamp + GenDate.TicksPerYear / 2));

            int recentLosses = HiringHistory.OfType<HistoryEvent_Killed>().Count()
                             + HiringHistory.OfType<HistoryEvent_Kidnapped>().Count();
            int recentDowns  = HiringHistory.OfType<HistoryEvent_Downed>().Count();

            Log.Message($"GetFactorForHireableFaction {Def.LabelCap}: recentLosses={recentLosses}, recentDowns={recentDowns}");

            return 1.0f + 0.05f * recentLosses + 0.025f * recentDowns;
        }

        private ContractInfo TryGetContractInfo()
        {
            return HiringContractTracker.GetOngoingContracts().Where(c => c.hireableFactionDef == Def).FirstOrDefault();
        }

        public string GetCallLabel()
        { 
            if (TryGetContractInfo() == null)
                return "VEF.Hire".Translate(Def.LabelCap);
            else
                return "VEF.ContractInfo".Translate(Def.LabelCap);
        }

        public string GetInfoText() => "VEF.HireDesc".Translate(Def.LabelCap);

        public void TryOpenComms(Pawn negotiator)
        {
            var contract = TryGetContractInfo();
            if (contract != null)
            {
                Find.WindowStack.Add(new Dialog_ContractInfo(contract));
            }
            else
            {
                Find.WindowStack.Add(new Dialog_Hire(negotiator, this));
            }
        }

        public Faction GetFaction() => null;

        public FloatMenuOption CommFloatMenuOption(Building_CommsConsole console, Pawn negotiator) => FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(GetCallLabel(),
                () => console.GiveUseCommsJob(negotiator, this),
                iconTex: Def.referencedFaction.FactionIcon,
                iconColor: Def.referencedFaction.DefaultColor,
                priority: MenuOptionPriority.InitiateSocial)
            , negotiator, console);

        public string GetUniqueLoadID()
        {
            string foo = $"{nameof(HireableFaction)}_{Def.defName}";

            Log.Message(foo);

            return foo;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref Def, nameof(Def));

            // Okay, I finally understood how scribe works. When using 'Scribe_Collections.Look' with a 'List'
            // We get one 'LookMode' argument, that one is for the values in the 'List'. Lists don't have keys.
            // Using 'LookMode.Deep' here means we are calling the 'ExposeData()' method on the values in the list
            // to add them to the savegame. That is exactly what we want, because any future history event can just
            // implement ExposeData() to save it's state.

            Scribe_Collections.Look(ref HiringHistory, nameof(HiringHistory), LookMode.Deep);
        }

    }
}
