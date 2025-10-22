using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace Deployables
{
    public class PickupCoverJob : JobDriver
    {
        private const int WorkTicks = 30;

        protected Thing Cover => job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOn(() => pawn == null || pawn.Dead || pawn.Downed);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            var toil = Toils_General.Wait(WorkTicks);
            toil.WithProgressBarToilDelay(TargetIndex.A);
            toil.AddFinishAction(() =>
            {
                if (pawn != null && Cover != null)
                    PickupCoverUtils.PickupCover(pawn);
            });
            yield return toil;
        }
    }
}
