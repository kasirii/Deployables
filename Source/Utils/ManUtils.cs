using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Reflection;
using CombatExtended;


namespace Deployables
{
    public static class ManUtils
    {
        private static readonly Dictionary<Building_TurretGunCE, int> targetWaitTicks = new Dictionary<Building_TurretGunCE, int>();
        public static void ManTow(Pawn pawn, Thing thing)
        {
            if (pawn == null || thing == null) return;
            Building_TurretGunCE turret = thing as Building_TurretGunCE;
            var compAmmo = turret.CompAmmo;
            
           //Log.Message($"compAmmo is {compAmmo.ToString()}");

            if (turret.mannableComp.ManningPawn == null)
            {
                TryManTurret(pawn, thing);
            }

            if (compAmmo != null)
            {
               //Log.Message($"compAmmo CurMagCount is {compAmmo.CurMagCount.ToString()}");
                if (HasAnyAmmo(pawn, compAmmo) 
                    && compAmmo.HasMagazine 
                    && compAmmo.CurMagCount < 0.5f* compAmmo.MagSize)
                {
                    TryReloadFromInventory(pawn, turret);
                    return;
                }

                if (!HasAnyAmmo(pawn, compAmmo) && compAmmo.CurMagCount == 0)
                {
                    thing.Kill();
                    return;
                }
            }
            if (!targetWaitTicks.ContainsKey(turret))
                targetWaitTicks[turret] = 0;
            //var turretGun = thing as Building_TurretGunCE;
            if (turret != null && (turret.CurrentTarget == null || !turret.CurrentTarget.IsValid))
            {
               //Log.Message("turret target is invalid");
                targetWaitTicks[turret]++;
                if (targetWaitTicks[turret] > 12f)
                {
                    var job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("PickupCover"), thing);
                    pawn.jobs.StartJob(job, JobCondition.InterruptOptional, null, true, true);
                    targetWaitTicks[turret] = 0;
                }
                    
                return;
            }
            else
                targetWaitTicks[turret] = 0;

        }

        private static void TryManTurret(Pawn pawn, Thing thing)
        {
            if (pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.None))
            {
               //Log.Message("TryManTurret start");
                Job job = JobMaker.MakeJob(JobDefOf.ManTurret, thing);
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
        }

        private static void TryReloadFromInventory(Pawn pawn, Building_TurretGunCE turret)
        {
            Thing ammo = null;
            var compAmmo = turret.CompAmmo;
            foreach (var item in pawn.inventory.innerContainer)
            {
                if (compAmmo.Props.ammoSet.ammoTypes.Any(t => t.ammo == item.def))
                {
                    ammo = item;
                    break;
                }
            }
           //Log.Message($"ammo is {ammo.ToString()}");

            if (ammo == null || ammo.stackCount == 0) return;
            Thing ammoToDrop = ammo.SplitOff(System.Math.Min(compAmmo.MagSize - compAmmo.CurMagCount, ammo.stackCount));
            GenPlace.TryPlaceThing(ammoToDrop, pawn.Position, pawn.Map, ThingPlaceMode.Near);

            Job reloadJob = JobMaker.MakeJob(CE_JobDefOf.ReloadTurret, turret, ammoToDrop);
            reloadJob.count = ammoToDrop.stackCount;
            pawn.jobs.StartJob(reloadJob, JobCondition.InterruptOptional, null, true, true);
           //Log.Message($"job is {pawn.CurJob.def.ToString()}");
            

        }

        private static bool HasAnyAmmo(Pawn pawn, CompAmmoUser compAmmo)
        {
            foreach (var item in pawn.inventory.innerContainer)
            {
                if (compAmmo.Props.ammoSet.ammoTypes.Any(t => t.ammo == item.def))
                    return true;
            }
            return false;
        }

    }
}