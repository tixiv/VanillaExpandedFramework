using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace VFECore.Misc.HireableSystem
{
    class ComTarget_Hire : ICommunicable
    {
        HireableFactionDef hireableFactionDef;

        public ComTarget_Hire(HireableFactionDef hireableFactionDef)
        {
            this.hireableFactionDef = hireableFactionDef;
        }

        public string GetCallLabel() => "VEF.Hire".Translate(hireableFactionDef.LabelCap);

        public string GetInfoText() => "VEF.HireDesc".Translate(hireableFactionDef.LabelCap);

        public void TryOpenComms(Pawn negotiator)
        {
            Find.WindowStack.Add(new Dialog_Hire(negotiator, hireableFactionDef));
        }

        public Faction GetFaction()
        {
            Log.Message("GetFaction called");
            return Faction.OfPirates;
        }

        public FloatMenuOption CommFloatMenuOption(Building_CommsConsole console, Pawn negotiator) => FloatMenuUtility.DecoratePrioritizedTask(
            new FloatMenuOption(GetCallLabel(),
                () => console.GiveUseCommsJob(negotiator, this),
                itemIcon: hireableFactionDef.referencedFaction.FactionIcon,
                iconColor: hireableFactionDef.referencedFaction.DefaultColor,
                priority: MenuOptionPriority.InitiateSocial)
            , negotiator, console);
    }
}
