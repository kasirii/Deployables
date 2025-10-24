using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Deployables
{
	public static class PickupCoverUtils
	{
		public static bool PickupCover(Pawn pawn)
		{
			if (pawn == null || pawn.Map == null) return false;

			var kvp = DeployablesOwnerComp.OwnerMap
				.FirstOrDefault(kv => kv.Value.Item1 == pawn);

			if (kvp.Equals(default(KeyValuePair<Thing, (Pawn, Thing)>))) return false;

			var cover = kvp.Key;
			var parent = kvp.Value.Item2 as Apparel;
			var newApparel = ThingMaker.MakeThing(parent.def, cover.Stuff) as Apparel;
			newApparel.HitPoints = (int)(newApparel.MaxHitPoints * (float)cover.HitPoints / cover.MaxHitPoints);
			pawn.apparel.Wear(newApparel, dropReplacedApparel: true);

			if (!cover.DestroyedOrNull())
			{
                DelayedDestroy.Schedule(cover);
                //cover.DeSpawn();
            }
			return true;
		}
	}
}
