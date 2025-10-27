using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Deployables
{
	public static class PickupCoverUtils
	{
		public static void PickupCover(Pawn pawn)
		{
			if (pawn == null || pawn.Map == null) return;

			var kvp = DeployablesOwnerComp.OwnerMap
				.FirstOrDefault(kv => kv.Value.Item1 == pawn);

			if (kvp.Equals(default(KeyValuePair<Thing, (Pawn, Thing)>))) return;

			var cover = kvp.Key;
			var parent = kvp.Value.Item2 as Apparel;
            var coverComp = parent.AllComps.OfType<CompSpawnCover>().FirstOrDefault();

            if (coverComp.Props.destroyParentOnUse)
			{
                var newApparel = ThingMaker.MakeThing(parent.def, cover.Stuff) as Apparel;
                newApparel.HitPoints = (int)(newApparel.MaxHitPoints * (float)cover.HitPoints / cover.MaxHitPoints);
                pawn.apparel.Wear(newApparel, dropReplacedApparel: true);
            }

			if (!cover.DestroyedOrNull())
			{
                DelayedDestroy.Destroy(cover);
                //cover.Destroy(DestroyMode.Vanish);
                //cover.DeSpawn();
            }
			return;
		}
	}
}
