using RimWorld;
using Verse;
using Verse.AI;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using CombatExtended;
using CombatExtended.CombatExtended.Jobs.Utils;


namespace Deployables
{
    public static class ManUtils
    {
        private static readonly Dictionary<Building_TurretGunCE, int> targetWaitTicks = new Dictionary<Building_TurretGunCE, int>();
        //private static readonly HashSet<Pawn> reloadingPawns = new HashSet<Pawn>();
        public static void ManTow(Pawn pawn, Thing thing)
        {
            
            if (pawn == null || thing == null) return;
            //if (reloadingPawns.Contains(pawn)) return;
            Building_TurretGunCE turret = thing as Building_TurretGunCE;
            var compAmmo = turret.CompAmmo;
            if (pawn.CurJob.def != JobDefOf.ManTurret)
            {
                if (turret.mannableComp.ManningPawn == null)
                {
                    TryManTurret(pawn, thing);
                }
            }
            else
            { 
                if (compAmmo != null)
                {
                    if (compAmmo.HasMagazine
                        && compAmmo.CurMagCount < 0.5f * compAmmo.MagSize)
                    {
                        if (pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.None))
                            TryReloadFromInventory(pawn, turret);
                        return;
                    }
                }
                if (!targetWaitTicks.ContainsKey(turret))
                    targetWaitTicks[turret] = 0;
                if (turret != null && (turret.CurrentTarget == null || !turret.CurrentTarget.IsValid))
                {
                    targetWaitTicks[turret]++;
                    if (targetWaitTicks[turret] > 12f)
                    {
                        var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("PickupCover"), turret);
                        pawn.jobs.StartJob(job, JobCondition.InterruptOptional, null, false, true);
                        targetWaitTicks[turret] = 0;
                    }
                    
                    return;
                }
                else
                    targetWaitTicks[turret] = 0;
            }
        }

        private static void TryManTurret(Pawn pawn, Thing thing)
        {
            if (pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.Some))
            {
                Job job = JobMaker.MakeJob(JobDefOf.ManTurret, thing);
                pawn.jobs.StartJob(job, JobCondition.InterruptOptional, null, false, true);
            }
        }

        private static void TryReloadFromInventory(Pawn pawn, Building_TurretGunCE turret)
        {
            var compAmmo = turret.CompAmmo;
            if (compAmmo == null || !compAmmo.HasMagazine) return;

            var coverComp = pawn.apparel?.WornApparel?
                .SelectMany(a => a.AllComps)
                .OfType<CompSpawnCover>()
                .FirstOrDefault();
            if (coverComp == null)
            {
                DelayedDestroy.Destroy(turret);
                return;
            }
            if (coverComp.remainingAmmo == 0)
            {
                DelayedDestroy.Kill(turret);
                var kvp = DeployablesOwnerComp.OwnerMap
                .FirstOrDefault(kv => kv.Value.Item1 == pawn);
                var parent = kvp.Value.Item2 as Apparel;
                DelayedDestroy.Destroy(parent);
                return;
            }
                

            int toReloadCount = compAmmo.MagSize - compAmmo.CurMagCount;
            int ammoCount = coverComp.TryConsumeAmmo(toReloadCount);

            Thing ammoThing = ThingMaker.MakeThing(coverComp.Props.ammoDef);
            if (ammoThing == null) return;

            var ammoNearby = GenClosest.ClosestThingReachable(
                    pawn.Position,
                    pawn.Map,
                    ThingRequest.ForGroup(ThingRequestGroup.HaulableEver),
                    PathEndMode.Touch,
                    TraverseParms.For(pawn),
                    2.9f,
                    t => t is AmmoThing);
            Job reloadJob;
            if (ammoNearby != null)
            {
                reloadJob = JobMaker.MakeJob(CE_JobDefOf.ReloadTurret, turret, ammoNearby);
                reloadJob.count = System.Math.Min(ammoNearby.stackCount, ammoCount);
            }
            else 
            {
                GenPlace.TryPlaceThing(ammoThing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                ammoThing.stackCount = ammoCount;
                reloadJob = JobMaker.MakeJob(CE_JobDefOf.ReloadTurret, turret, ammoThing);
                reloadJob.count = ammoCount;
            }
            pawn.jobs.StartJob(reloadJob, JobCondition.InterruptForced, null, true, true);

        }
    }
}