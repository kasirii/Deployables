using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace Deployables
{
    public class PickupCoverJob : JobDriver
    {
        private const int WorkTicks = 30;

        private Thing Cover => job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Cover, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => pawn == null || pawn.Dead || pawn.Downed);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            
            var toil = Toils_General.Wait(WorkTicks);
            toil.WithProgressBarToilDelay(TargetIndex.A);
            toil.FailOnDestroyedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            if (Cover == null)
                EndJobWith(JobCondition.Incompletable);
            toil.AddFinishAction(() =>
            {
            if (pawn != null && !pawn.Dead && !pawn.Downed && !Cover.DestroyedOrNull())
                PickupCoverUtils.PickupCover(pawn, Cover);
            });
            yield return toil;
        }
    }
}
