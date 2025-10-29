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
        public List<ThingWeight> coverThingList;
        public ThingDef coverThingDef;
        public int spawnOffset = 1;
        public float drawOffset = 1;
        public bool doFlip = true;

        public List<ThingWeight> ammoList;
        public AmmoDef ammoDef;
        public IntRange ammoCountRange = new IntRange(3, 5);
        public bool randomizeAmmo = false;
        public bool infiniteAmmo = false;

        public CompProps_CompSpawnCover()
        {
            this.compClass = typeof(CompSpawnCover);
        }
    }

    public class CompSpawnCover : ThingComp
    {
        public CompProps_CompSpawnCover Props => (CompProps_CompSpawnCover)props;

        public int remainingAmmo;
        public int loadedAmmo;
        public bool isCoverTurret = false;
        public bool isCoverSpawned = false;

        public Thing cover;
        public CompAmmoUser ammoComp;

        public virtual Pawn PawnOwner => (this.parent as Apparel).Wearer;

        private Thing MakeThing => ThingMaker.MakeThing(Props.coverThingDef, parent.Stuff);


        public ThingDef GetRandomThingDef(List<ThingWeight> ThingList)
        {
            if (ThingList.NullOrEmpty()) return null;

            ThingWeight result;
            if (ThingList.TryRandomElementByWeight(tw => tw.weight, out result)) return result.thingDef;

            return null;
        }

        public override void PostPostMake()
        {
            base.PostPostMake();
            if (!Props.coverThingList.NullOrEmpty())
                Props.coverThingDef = GetRandomThingDef(Props.coverThingList);
            if (Props.coverThingDef.thingClass != null && typeof(Building_Turret).IsAssignableFrom(Props.coverThingDef.thingClass))
            {
                isCoverTurret = true;
                remainingAmmo = Props.ammoCountRange.RandomInRange;
                if (!Props.ammoList.NullOrEmpty())
                    Props.ammoDef = GetRandomThingDef(Props.ammoList) as AmmoDef;
                if (Props.randomizeAmmo)
                    Props.ammoDef = RandomAmmo() as AmmoDef;
                VerbUtils.UpdatePawnVerbRanges(PawnOwner, this, true);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref remainingAmmo, "remainingAmmo", 0);
            Scribe_Values.Look(ref loadedAmmo, "loadedAmmo", 0);
            Scribe_Values.Look(ref isCoverTurret, "isCoverTurret", false);
            Scribe_Values.Look(ref isCoverSpawned, "isCoverSpawned", false);
            Scribe_Defs.Look(ref Props.ammoDef, "ammoDef");
            Scribe_Defs.Look(ref Props.coverThingDef, "coverThingDef");
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
            if (isCoverSpawned || !isCoverTurret) return;
            ThingDef gunDef = Props.coverThingDef.building.turretGunDef;
            Graphic graphic = gunDef.graphicData.Graphic;
            Rot4 rot = PawnOwner.Rotation;
            Vector3 sightVec = rot.FacingCell.ToVector3() * Props.coverThingDef.building.turretTopDrawSize / 2f * Props.drawOffset;
            Vector3 drawPos = PawnOwner.DrawPos - sightVec + new Vector3(0, -1f, 0f) ;
            Quaternion quaternion = Quaternion.Euler(0, rot.AsAngle + 90f, 0);
            Matrix4x4 matrix = Matrix4x4.TRS(drawPos, quaternion, Vector3.one * Props.coverThingDef.building.turretTopDrawSize);
            Material mat = graphic.MatAt(rot);
            Graphics.DrawMesh(MeshPool.plane10, matrix, mat, 0);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (Find.TickManager.TicksGame % 10 != 0) return;
            ManUtils.TryMan(PawnOwner, cover, this, ammoComp);
            if (Find.TickManager.TicksGame % 600 == 0 && isCoverTurret)
                VerbUtils.UpdatePawnVerbRanges(PawnOwner, this, true);
        }

        private ThingDef RandomAmmo()
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

        public void DoEffect(Pawn pawn, LocalTargetInfo castTarg)
        {
            if (pawn == null || Props.coverThingDef == null || isCoverSpawned) return;

            var map = pawn.Map; if (map == null) return;

            IntVec3 dirVec = (castTarg.Cell - pawn.Position).ClampMagnitude(1);
            IntVec3 cell = pawn.Position + dirVec * Props.spawnOffset;
            var requiredAffordance = Props.coverThingDef.terrainAffordanceNeeded;

            if (!cell.InBounds(map)
                || !cell.Standable(map)
                || cell == pawn.Position
                || (requiredAffordance != null && !cell.GetTerrain(map).affordances.Contains(requiredAffordance)))
            {
                VerbUtils.UpdatePawnVerbRanges(pawn, this, false);
                return;
            }
            
            Rot4 rotation = Rot4.FromAngleFlat(dirVec.ToVector3().AngleFlat());
            if (Props.doFlip) rotation = new Rot4((rotation.AsInt + 2) % 4);
            cover = GenSpawn.Spawn(MakeThing, cell, map, rotation) as Building_TurretGunCE;
            isCoverSpawned = true;
            cover.HitPoints = (int)(cover.MaxHitPoints * (float)parent.HitPoints / parent.MaxHitPoints);
            ammoComp = (cover as Building_TurretGunCE)?.Gun?.TryGetComp<CompAmmoUser>();
            if (isCoverTurret)
            {
                if (ammoComp == null || Props.ammoDef == null) return;
                ammoComp.LoadAmmo(ThingMaker.MakeThing(Props.ammoDef));
                if (loadedAmmo == 0)
                {
                    int toConsume = System.Math.Min(ammoComp.MagSize, remainingAmmo);
                    Props.ammoDef.ammoCount = toConsume;
                    ammoComp.ResetAmmoCount(Props.ammoDef);
                    remainingAmmo += loadedAmmo - toConsume;
                }
                else
                {
                    Props.ammoDef.ammoCount = loadedAmmo;
                    ammoComp.ResetAmmoCount(Props.ammoDef);
                }
            }
        }

        public void DestroyParent()
        {
            VerbUtils.UpdatePawnVerbRanges(PawnOwner, this, false);
            parent.Destroy(DestroyMode.Vanish);
        }

        public override void Notify_Equipped(Pawn pawn)
        {
            base.Notify_Equipped(pawn);
            if (isCoverTurret)
                VerbUtils.UpdatePawnVerbRanges(pawn, this, true);
        }
        public override void Notify_Unequipped(Pawn pawn)
        {
            base.Notify_Unequipped(pawn);
            if (isCoverTurret)
                VerbUtils.UpdatePawnVerbRanges(pawn, this, false);
        }
    }
    public class ThingWeight
    {
        public ThingDef thingDef;
        public float weight = 1f;
    }
}
