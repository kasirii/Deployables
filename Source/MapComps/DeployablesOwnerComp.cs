using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;


namespace Deployables
{
    public class DeployablesOwnerComp : MapComponent
    {
        public static readonly Dictionary<Thing, (Pawn, Thing)> OwnerMap = new Dictionary<Thing, (Pawn, Thing)>();

		public DeployablesOwnerComp(Map map) : base(map) { }

        public static void RegisterOwner(Thing thing, Pawn pawn, Thing parent)
        {
            if (thing == null || pawn == null || parent == null) return;
            OwnerMap[thing] = (pawn, parent);
		}

		private static readonly List<Thing> _toRemoveBuffer = new List<Thing>();
        private static readonly List<Thing> _toPickupBuffer = new List<Thing>();
        private int tickCounter;
        public override void MapComponentTick()
		{
			if (OwnerMap.Count > 0)
			{
                if (++tickCounter % 10 != 0) return;
                foreach (var kvp in OwnerMap)
				{
                    var thing = kvp.Key;
					var pawn = kvp.Value.Item1;
                    var parent = kvp.Value.Item2;
                    if (thing.DestroyedOrNull())
                    {
                        _toRemoveBuffer.Add(thing);
                        if (parent.TryGetComp<CompSpawnCover>().isCoverTurret)
                            parent.TryGetComp<CompSpawnCover>().isSpawnedTurret = false;
                        continue;
                    }
                    if (pawn.DestroyedOrNull()
                        || pawn.InMentalState || pawn.DeadOrDowned
                        || (pawn.Position - thing.Position).LengthHorizontal > 10f)
                    {
                        _toRemoveBuffer.Add(thing);
                        DelayedDestroy.Destroy(parent);
                        DelayedDestroy.Kill(thing);
                        if (parent.TryGetComp<CompSpawnCover>().isCoverTurret)
                            parent.TryGetComp<CompSpawnCover>().isSpawnedTurret = false;
                        continue;
                    }

                    if (pawn.CurJobDef == JobDefOf.Goto
                        && (pawn.CurJob.targetA.Cell - thing.Position).LengthHorizontal > 1.5f)
                            _toPickupBuffer.Add(thing);


                    if (thing.HasComp<CompMannable>()
                        && pawn.CurJob.def != CombatExtended.CE_JobDefOf.ReloadTurret
                        && pawn.CurJob.def != JobDefOf.Goto
                        && pawn.CurJob.def != DefDatabase<JobDef>.GetNamed("PickupCover"))
                            ManUtils.ManTow(pawn, thing);
                }

                if (_toPickupBuffer.Count > 0)
                {
                    foreach (var thing in _toPickupBuffer.ToList())
                    {
                        if (DeployablesOwnerComp.OwnerMap.TryGetValue(thing, out var value))
                        {
                            var pawn = value.Item1;
                            if (pawn != null && !pawn.Dead && !pawn.Downed && !pawn.InMentalState
                                && pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.None))
                            {
                                var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("PickupCover"), thing);
                                pawn.jobs.StartJob(job, JobCondition.InterruptOptional, null, true, true);
                            }
                        }
                    }    
                }

                if (_toRemoveBuffer.Count > 0)
				{
					foreach (var thing in _toRemoveBuffer.ToList())
                    {
                        OwnerMap.Remove(thing);
                    }
                }
                _toRemoveBuffer.Clear();
                _toPickupBuffer.Clear();
            }
        }
	}
}
