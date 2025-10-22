using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;

namespace Deployables
{
    public class DeployablesOwnerComp : MapComponent
    {
        public static readonly Dictionary<Thing, (Pawn, Thing)> OwnerMap = new Dictionary<Thing, (Pawn, Thing)>();

		public DeployablesOwnerComp(Map map) : base(map) { }

        public static void RegisterOwner(Thing thing, Pawn pawn, Thing parent)
        {
            if (thing == null || pawn == null) return;
            OwnerMap[thing] = (pawn, parent);
		}

        public static (Pawn, Thing) GetOwnerWithParent(Thing cover)
        {
            return cover != null && OwnerMap.TryGetValue(cover, out var v) ? v : (null, null);
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
                    if (thing.DestroyedOrNull() || pawn.DestroyedOrNull() || pawn.Downed || pawn.InMentalState)
                    {
                        _toRemoveBuffer.Add(thing);
                        continue;
                    }    
						
                    if (pawn.CurJob != null && pawn.CurJob.targetA.IsValid)
                    {
                        IntVec3 targetPos = pawn.CurJob.targetA.Cell;
                        if ((targetPos - thing.Position).LengthHorizontal > 1.5f)
                        {
                            _toPickupBuffer.Add(thing);
                        }
                    }
                }
                if (_toPickupBuffer.Count > 0)
                {
                    foreach (var thing in _toPickupBuffer)
                    {
                        if (DeployablesOwnerComp.OwnerMap.TryGetValue(thing, out var value))
                        {
                            var pawn = value.Item1;
                            if (pawn != null && !pawn.Dead && !pawn.Downed && !pawn.InMentalState)
                            {
                                var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("PickupCover"), thing);
                                pawn.jobs.StartJob(job, JobCondition.InterruptOptional, null, true, true);
                            }
                        }
                    }    
                }
                if (_toRemoveBuffer.Count > 0)
				{
					foreach (var thing in _toRemoveBuffer)
						OwnerMap.Remove(thing);
				}
                _toRemoveBuffer.Clear();
                _toPickupBuffer.Clear();
            }
		}
	}
}
