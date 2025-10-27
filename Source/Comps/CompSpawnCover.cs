using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using UnityEngine;
using CombatExtended;



namespace Deployables
{
    public class CompProps_CompSpawnCover : CompProperties
    {
        public ThingDef coverThingDef;
        public int spawnOffset = 1;
        public bool destroyParentOnUse = true;
        public bool ownerTag = true;
        public bool doFlip = true;

        public ThingDef ammoDef = null;
        public IntRange ammoCountRange = new IntRange(3, 5);
        public bool randomizeAmmo = false;
        public bool infiniteAmmo = false;

        public string turretTexPath;
        public float drawSize = 1f;

        public CompProps_CompSpawnCover()
        {
            this.compClass = typeof(CompSpawnCover);
        }
    }

    public class CompSpawnCover : ThingComp, IUseWhenCastEffect
    {
        public CompProps_CompSpawnCover Props => (CompProps_CompSpawnCover)props;

        public int remainingAmmo;

        public bool isSpawnedTurret = false;

        public bool isCoverTurret = false;

        public override void PostPostMake()
        {
            base.PostPostMake();
            if (Props.coverThingDef.thingClass != null && typeof(Building_Turret).IsAssignableFrom(Props.coverThingDef.thingClass))
            {
                isCoverTurret = true;
                remainingAmmo = Props.ammoCountRange.RandomInRange;
                if (Props.randomizeAmmo)
                    Props.ammoDef = RandomAmmo();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref remainingAmmo, "remainingAmmo", 0);
            Scribe_Values.Look(ref isCoverTurret, "isCoverTurret", false);
            Scribe_Defs.Look(ref Props.ammoDef, "ammoDef");
        }

        public override string CompInspectStringExtra()
        {
            var sb = new System.Text.StringBuilder();
            if (Props.ammoDef != null)
            {
                sb.AppendLine("Deployables.Ammo".Translate() + Props.ammoDef.label.CapitalizeFirst());
                sb.AppendLine("Deployables.RemainingAmmo".Translate() + remainingAmmo);
            }
            return sb.ToString().TrimEndNewlines();
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            base.SpecialDisplayStats();

            if (Props.ammoDef != null)
            {
                yield return new StatDrawEntry(
                    category: StatCategoryDefOf.Basics,
                    label: "Deployables.AmmoType".Translate(),
                    valueString: Props.ammoDef.LabelCap,
                    reportText: "Deployables.AmmoTypeDescriprion".Translate(),
                    displayPriorityWithinCategory: 21
                );
                yield return new StatDrawEntry(
                category: StatCategoryDefOf.Basics,
                label: "Deployables.RemainingAmmo".Translate(),
                valueString: remainingAmmo.ToString(),
                reportText: "Deployables.RemainingAmmoDescriprion".Translate(),
                displayPriorityWithinCategory: 20
            );
            }  
        }

        public override void CompDrawWornExtras()
        {
            base.CompDrawWornExtras();
            if (Props.turretTexPath.NullOrEmpty()) return;
            if (isSpawnedTurret) return;
            Material cachedMat = MaterialPool.MatFrom(Props.turretTexPath);
            Pawn pawn = (parent as Apparel)?.Wearer;
            Vector3 sightVec = Vector3.ClampMagnitude(pawn.Rotation.FacingCell.ToVector3(), Props.drawSize);
            Vector3 drawPos = pawn.DrawPos - sightVec + new Vector3(0, -1f, 0f) ;
            Quaternion rotation = Quaternion.Euler(0, pawn.Rotation.AsAngle + 90f, 0);
            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, rotation, Vector3.one * Props.drawSize);
            Mesh mesh = (Props.drawSize > 0.5f) ? MeshPool.plane20 : MeshPool.plane10;
            Graphics.DrawMesh(mesh, matrix, cachedMat, 0);
        }


        public int TryConsumeAmmo(int amount)
        {
            if (Props.infiniteAmmo) return amount;
            int toConsume = System.Math.Min(amount, remainingAmmo);
            remainingAmmo -= toConsume;
            return toConsume;
        }

        public ThingDef RandomAmmo()
        {
            ThingDef ammoToUse = null;
            if (Props.coverThingDef?.building?.turretGunDef != null)
            {
                var ammoSet = Props.coverThingDef.building.turretGunDef.GetCompProperties<CompProperties_AmmoUser>()?.ammoSet;
                if (ammoSet != null && ammoSet.ammoTypes.Any())
                    ammoToUse = ammoSet.ammoTypes.RandomElement().ammo;
            }
            return ammoToUse;
        }

        public void DoEffect(Pawn pawn, ThingWithComps parent)
        {
            if (pawn == null || Props.coverThingDef == null
                || isSpawnedTurret) return;
            var map = pawn.Map; if (map == null) return;
            var stanceBusy = pawn.stances?.curStance as Stance_Busy; if (stanceBusy == null) return;
            Verb verb = stanceBusy.verb;
            LocalTargetInfo verbTarget = verb.CurrentTarget;

            IntVec3 dirVec = (verbTarget.Cell - pawn.Position).ClampMagnitude(1);
            IntVec3 cell = pawn.Position + dirVec * Props.spawnOffset;
            var requiredAffordance = Props.coverThingDef.terrainAffordanceNeeded;
            if (!cell.InBounds(map)
                || !cell.Standable(map)
                || cell == pawn.Position
                || (requiredAffordance != null && !cell.GetTerrain(map).affordances.Contains(requiredAffordance)))
            {
                VerbUtils.UpdatePawnVerbRanges(pawn, false);
                return;
            }

            var thing = ThingMaker.MakeThing(Props.coverThingDef, parent.Stuff);
            Rot4 rotation = Rot4.FromAngleFlat(dirVec.ToVector3().AngleFlat());
            if (Props.doFlip)
                rotation = new Rot4((rotation.AsInt + 2) % 4);
            var cover = GenSpawn.Spawn(thing, cell, map, rotation);
            cover.HitPoints = (int)(cover.MaxHitPoints * (float)parent.HitPoints / parent.MaxHitPoints);

            if (isCoverTurret)
            {
                isSpawnedTurret = true;
                var ammoComp = (cover as Building_TurretGunCE)?.Gun?.TryGetComp<CompAmmoUser>();
                if (ammoComp != null && Props.ammoDef != null)
                {
                    ammoComp.LoadAmmo(ThingMaker.MakeThing(Props.ammoDef));
                    ammoComp.ResetAmmoCount(Props.ammoDef as AmmoDef);
                }
            }
            if (Props.ownerTag)
                DeployablesOwnerComp.RegisterOwner(thing, pawn, parent);
            if (Props.destroyParentOnUse)
                DelayedDestroy.Destroy(parent); 
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (isCoverTurret)
                VerbUtils.UpdatePawnVerbRanges(pawn, true);
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if (isCoverTurret)
                VerbUtils.UpdatePawnVerbRanges(pawn, true);
            DelayedDestroy.Destroy(parent);
        }
        public override void Notify_WearerDied()
        {
            base.Notify_WearerDied();
            DelayedDestroy.Destroy(parent);
        }
        public override void Notify_Downed()
        {
            base.Notify_Downed();
            DelayedDestroy.Destroy(parent);
        }
}
}
