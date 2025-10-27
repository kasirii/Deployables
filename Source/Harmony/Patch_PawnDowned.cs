using RimWorld;
using Verse;
using System.Linq;
using HarmonyLib;

namespace Deployables
{
    [HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
    public static class Patch_PawnDowned
    {
        private static readonly AccessTools.FieldRef<Pawn_HealthTracker, Pawn> pawnRef =
           AccessTools.FieldRefAccess<Pawn_HealthTracker, Pawn>("pawn");
        public static void Postfix(Pawn_HealthTracker __instance)
        {
            foreach (var comp in pawnRef(__instance)?.apparel?.WornApparel?.SelectMany(a => a.AllComps).OfType<CompSpawnCover>().ToList())
                comp.Notify_Downed();
        }
    }
}

