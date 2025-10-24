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
                    if (thing.DestroyedOrNull() || pawn.DestroyedOrNull() 
                        || pawn.Downed || pawn.InMentalState
                        || (pawn.Position - thing.Position).LengthHorizontal > 10f)
                    {
                        _toRemoveBuffer.Add(thing);
                        continue;
                    }    
						
                    if (pawn.CurJobDef == JobDefOf.Goto)
                    {
                        //Log.Message($"pawn CurJob is {pawn.CurJob.ToString()}");
                        if ((pawn.CurJob.targetA.Cell - thing.Position).LengthHorizontal > 1.5f)
                        {
                            _toPickupBuffer.Add(thing);
                        }
                    }

                    if (thing.HasComp<CompMannable>()
                        && pawn.CurJob.def != CombatExtended.CE_JobDefOf.ReloadTurret)
                    {
                       //Log.Message("ManUtils start");
                        ManUtils.ManTow(pawn, thing);
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
