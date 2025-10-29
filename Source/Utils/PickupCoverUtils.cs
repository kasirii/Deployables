using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Deployables
{
	public static class PickupCoverUtils
	{
		public static void PickupCover(Pawn pawn, Thing cover)
		{
			if (pawn == null || cover.DestroyedOrNull()) return;

            var apparel = pawn?.apparel?.WornApparel?.FirstOrDefault(a => a.AllComps.OfType<CompSpawnCover>().Any());
            if (apparel == null) return;

            var coverComp = apparel.AllComps.OfType<CompSpawnCover>().FirstOrDefault();
            if (coverComp == null) return;

            apparel.HitPoints = (int)(apparel.MaxHitPoints * (float)cover.HitPoints / cover.MaxHitPoints);
            cover.Destroy(DestroyMode.Vanish);
			coverComp.isCoverSpawned = false;
			return;
		}
	}
}
