using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VEF.Planet
{
    using RimWorld;
    using Verse;
    using Verse.AI;

    public class JobDriver_LeaveMap : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Legacy job from the old Hireable framework

            // Also yield two toils (like the old job did) so when an old game is loaded you
            // don't get an error about this job not having the requested toil.
            // The pawns will leave the map now through the quest after it gets converted.
            // pawns might still have this job on load thoguh, that is why this job
            // driver with two toils in it is needed to load cleanly.

            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.ClosestTouch);
        }
    }
}