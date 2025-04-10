using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace VFECore.Misc.HireableSystem
{
    public class ComTarget_ViewContract : ICommunicable
    {
        private Hireable hireable;


        public ComTarget_ViewContract(Hireable hireable)
        {
            this.hireable = hireable;
        }

        public FloatMenuOption CommFloatMenuOption(Building_CommsConsole console, Pawn negotiator) => FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(GetCallLabel(), () => console.GiveUseCommsJob(negotiator, this), MenuOptionPriority.InitiateSocial), negotiator, console);

        public string GetCallLabel() => "VEF.ContractInfo".Translate(hireable.Key.CapitalizeFirst());
        
        public Faction GetFaction()
        {
            Log.Message("GetFaction called");
            return Faction.OfPirates;
        }

        public string GetInfoText() => "";

        public void TryOpenComms(Pawn negotiator)
        {
            var contracts = HiringContractTracker.GetOngoingContracts().Where(c => c.hireable == hireable);
            if (contracts.Any())
                Find.WindowStack.Add(new Dialog_ContractInfo(contracts.First()));
        }
    }
}
