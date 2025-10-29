using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using CombatExtended;


namespace Deployables
{
    public static class ManUtils
    {
        private static readonly Dictionary<CompSpawnCover, int> targetWaitTicks = new Dictionary<CompSpawnCover, int>();
        private static readonly Dictionary<CompSpawnCover, int> dangerWaitTicks = new Dictionary<CompSpawnCover, int>();

        public static void TryMan(
            Pawn pawn = null, 
            Thing cover = null, 
            CompSpawnCover coverComp = null, 
            CompAmmoUser ammoComp = null)
        {
            if (coverComp == null) return;

            if (pawn.DestroyedOrNull() || pawn.InMentalState || pawn.DeadOrDowned)
            {
                if (!cover.DestroyedOrNull())
                {
                    cover.Kill();
                    coverComp.isCoverSpawned = false;
                }
                coverComp.DestroyParent();
                return;
            }

            if (!coverComp.isCoverSpawned) return;

            if (cover.DestroyedOrNull())
            {
                coverComp.DestroyParent();
                coverComp.isCoverSpawned = false;
                return;
            }

            if ((pawn.Position - cover.Position).LengthHorizontal > 10f)
            {
                cover.Kill();
                coverComp.isCoverSpawned = false;
                coverComp.DestroyParent();
                return;
            }

            if (!dangerWaitTicks.ContainsKey(coverComp)) dangerWaitTicks[coverComp] = 0;
            if (dangerWaitTicks[coverComp] > 0)
            {
                dangerWaitTicks[coverComp]--;
                return;
            }

            if (pawn.CurJobDef == CE_JobDefOf.RunForCover
                || pawn.mindState.meleeThreat != null)
            {
                dangerWaitTicks[coverComp] = 30;
                pawn.jobs.ClearQueuedJobs();
                return;
            }

            if (pawn.CurJobDef == JobDefOf.Goto)
            {
                TryPickup(pawn, cover, coverComp, ammoComp);
                return;
            }

            if (!coverComp.isCoverTurret) return;

            var turret = cover as Building_TurretGunCE;

            if (pawn.CurJobDef == JobDefOf.Wait_Combat)
            { 
                TryManTurret(pawn, turret);
                return;
            }

            if (pawn.CurJobDef != JobDefOf.ManTurret) return;

            if (ammoComp.HasMagazine && ammoComp.CurMagCount < 0.5f * ammoComp.MagSize)
            {
                TryReloadFromInventory(pawn, turret, coverComp, ammoComp);
                return;
            }

            if (!targetWaitTicks.ContainsKey(coverComp)) targetWaitTicks[coverComp] = 0;

            if ((turret.CurrentTarget == null || !turret.CurrentTarget.IsValid))
            {
                targetWaitTicks[coverComp]++;
                if (targetWaitTicks[coverComp] > 12f)
                {
                    TryPickup(pawn, turret, coverComp, ammoComp);
                    targetWaitTicks[coverComp] = 0;
                }
                return;
            }
            else if(targetWaitTicks[coverComp] != 0)
                targetWaitTicks[coverComp] = 0;
        }

        public static void TryPickup(Pawn pawn, Thing cover, CompSpawnCover coverComp, CompAmmoUser ammoComp = null)
        {
            if (pawn == null || cover == null || coverComp == null) return;
            if (pawn.CanReserveAndReach(cover, PathEndMode.InteractionCell, Danger.None))
            {
                if (ammoComp != null)
                    coverComp.loadedAmmo = ammoComp.CurMagCount;
                var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("PickupCover"), cover);
                pawn.jobs.StartJob(job, JobCondition.InterruptOptional, null, true, true);
            }
        }

        private static void TryManTurret(Pawn pawn, Building_TurretGunCE turret)
        {
            if (pawn == null || turret == null) return;
            if (pawn.CanReserveAndReach(turret, PathEndMode.InteractionCell, Danger.None))
            {
                Job job = JobMaker.MakeJob(JobDefOf.ManTurret, turret);
                pawn.jobs.StartJob(job, JobCondition.InterruptOptional, null, false, true);
            }
        }

        public static int TryConsumeAmmo(int amount, CompSpawnCover coverComp)
        {
            if (coverComp.Props.infiniteAmmo) return amount;
            int toConsume = System.Math.Min(amount, coverComp.remainingAmmo);
            coverComp.remainingAmmo -= toConsume;
            return toConsume;
        }

        private static void TryReloadFromInventory(Pawn pawn, Building_TurretGunCE turret, CompSpawnCover coverComp, CompAmmoUser ammoComp)
        {
            if (pawn == null || turret == null || coverComp == null || ammoComp == null) return;
            if (pawn.CanReserveAndReach(turret, PathEndMode.InteractionCell, Danger.None))
            {
                if (coverComp.remainingAmmo == 0)
                {
                    turret.Kill();
                    coverComp.isCoverSpawned = false;
                    var apparel = coverComp.parent;
                    if (apparel == null) return;
                    apparel.Destroy(DestroyMode.Vanish);
                    return;
                }

                Thing ammoNearby = GenClosest.ClosestThingReachable(
                        pawn.Position,
                        pawn.Map,
                        ThingRequest.ForDef(coverComp.Props.ammoDef),
                        PathEndMode.Touch,
                        TraverseParms.For(pawn),
                        3f,
                        t => t.Spawned && pawn.CanReserve(t));

                Job reloadJob;
                int toReloadCount = ammoComp.MagSize - ammoComp.CurMagCount;
                if (ammoNearby != null)
                {
                    reloadJob = JobMaker.MakeJob(CE_JobDefOf.ReloadTurret, turret, ammoNearby);
                    reloadJob.count = System.Math.Min(ammoNearby.stackCount, toReloadCount);
                }
                else
                {
                    Thing ammoThing = ThingMaker.MakeThing(coverComp.Props.ammoDef);
                    GenPlace.TryPlaceThing(ammoThing, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                    int ammoCount = TryConsumeAmmo(toReloadCount, coverComp);
                    ammoThing.stackCount = ammoCount;
                    reloadJob = JobMaker.MakeJob(CE_JobDefOf.ReloadTurret, turret, ammoThing);
                    reloadJob.count = ammoCount;
                }
                pawn.jobs.StartJob(reloadJob, JobCondition.InterruptForced, null, true, true);
            }
        }
    }
}