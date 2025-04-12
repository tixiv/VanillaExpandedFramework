using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace VFECore.Misc.HireableSystem
{
    public class CommTarget_ViewContract : ICommunicable, IExposable, ILoadReferenceable
    {
        private HireableFactionDef hireableFactionDef;

        public CommTarget_ViewContract(HireableFactionDef hireableFactionDef)
        {
            this.hireableFactionDef = hireableFactionDef;
        }

        public CommTarget_ViewContract()
        {
        }

        private ContractInfo TryGetContractInfo()
        {
            return HiringContractTracker.GetOngoingContracts().Where(c => c.hireableFactionDef == hireableFactionDef).FirstOrDefault();
        }

        public FloatMenuOption CommFloatMenuOption(Building_CommsConsole console, Pawn negotiator) => FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(GetCallLabel(),
                () => console.GiveUseCommsJob(negotiator, this),
                itemIcon: hireableFactionDef.referencedFaction.FactionIcon,
                iconColor: hireableFactionDef.referencedFaction.DefaultColor,
                priority: MenuOptionPriority.InitiateSocial)
            , negotiator, console);

        public string GetCallLabel() => "VEF.ContractInfo".Translate(hireableFactionDef.LabelCap);

        public Faction GetFaction() => null;

        public string GetInfoText() => "";

        public void TryOpenComms(Pawn negotiator)
        {
            var contract = TryGetContractInfo();
            if (contract != null)
                Find.WindowStack.Add(new Dialog_ContractInfo(contract));
        }

        public string GetUniqueLoadID() => $"VEF_{nameof(CommTarget_ViewContract)}_{hireableFactionDef.defName}";

        public void ExposeData()
        {
            Scribe_Defs.Look(ref hireableFactionDef, nameof(hireableFactionDef));
        }
    }
}
