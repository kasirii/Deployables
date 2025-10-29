using RimWorld;
using Verse;
using System.Linq;
using HarmonyLib;

namespace Deployables
{
    public static class Patch_Verb_TryStartCastOn
    {
        public static void Postfix(
            Verb __instance,
            LocalTargetInfo castTarg,
            LocalTargetInfo destTarg,
            bool surpriseAttack,
            bool canHitNonTargetPawns,
            bool preventFriendlyFire,
            bool nonInterruptingSelfCast)
        {
            try
            {
                if (!__instance.verbProps.IsMeleeAttack && (__instance is Verb_Shoot || __instance is CombatExtended.Verb_ShootCE))
                {
                    var pawn = __instance.CasterPawn;
                    if (pawn == null || !__instance.CasterIsPawn) return;
                    var apparel = pawn?.apparel?.WornApparel?.FirstOrDefault(a => a.AllComps.OfType<CompSpawnCover>().Any());
                    if (apparel == null) return;
                    var coverComp = apparel.AllComps.OfType<CompSpawnCover>().FirstOrDefault();
                    if (coverComp == null || !coverComp.cover.DestroyedOrNull()) return;

                    coverComp.DoEffect(pawn, castTarg);
                }
            }
            catch (System.Exception) { }
        }
    }
}