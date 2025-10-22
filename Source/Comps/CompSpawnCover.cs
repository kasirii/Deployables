using RimWorld;
using Verse;
using Verse.AI;

namespace Deployables
{
    public class CompProps_CompSpawnCover : CompProperties
    {
        public ThingDef coverThingDef;
        public int spawnOffset = 1;
        public bool destroyParentOnUse = true;
        public bool ownerTag = true;

        public CompProps_CompSpawnCover()
        {
            this.compClass = typeof(CompSpawnCover);
        }
    }

    public class CompSpawnCover : ThingComp, IUseWhenCastEffect
    {
        public CompProps_CompSpawnCover Props => (CompProps_CompSpawnCover)props;

        public void DoEffect(Pawn pawn, ThingWithComps parent)
        {
            if (pawn == null || Props.coverThingDef == null) return;
            var map = pawn.Map; if (map == null) return;

            var stanceBusy = pawn.stances?.curStance as Stance_Busy;
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
                return;
            }

            var thing = ThingMaker.MakeThing(Props.coverThingDef, parent.Stuff);
            Rot4 rotation = Rot4.FromAngleFlat(dirVec.ToVector3().AngleFlat());
            rotation = new Rot4((rotation.AsInt + 2) % 4);

            var cover = GenSpawn.Spawn(thing, cell, map, rotation);
            cover.HitPoints = (int)(cover.MaxHitPoints * (float)parent.HitPoints / parent.MaxHitPoints);

            if (Props.ownerTag)
                DeployablesOwnerComp.RegisterOwner(thing, pawn, parent);

            if (Props.destroyParentOnUse)
                DelayedDestroy.Schedule(parent);
        }
    }
}
