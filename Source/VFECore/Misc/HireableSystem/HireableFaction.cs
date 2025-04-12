using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace VFECore.Misc.HireableSystem
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

        public void NotifyLosses(int numLost)
        {
            HiringHistory.Add(new HistoryEvent_PeopleKilled
            {
                timestamp = Find.TickManager.TicksGame,
                numKilled = numLost
            });
        }

        public float GetFactorForHireableFaction()
        {
            int recentlyKilled = 0;

            foreach (var historyEvent in HiringHistory.OfType<HistoryEvent_PeopleKilled>())
            {
                if (Find.TickManager.TicksGame > historyEvent.timestamp + GenDate.TicksPerYear)
                    HiringHistory.Remove(historyEvent);
                else
                    recentlyKilled += historyEvent.numKilled;
            }

            Log.Message($"GetFactorForHireableFaction {Def.LabelCap}: recentlyKilled={recentlyKilled}");

            return 0.05f * recentlyKilled;
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
                itemIcon: Def.referencedFaction.FactionIcon,
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
