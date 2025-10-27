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
                if (!(__instance is Verb_Shoot || __instance is CombatExtended.Verb_ShootCE)) return;

                if (!__instance.CasterIsPawn) return;

                var pawn = __instance.CasterPawn;
                if (pawn == null || pawn.apparel == null) return;

                var weapon = pawn.equipment?.Primary;
                if (weapon?.def == null) return;

                foreach (var comp in pawn.apparel.WornApparel
                             .SelectMany(a => a.AllComps)
                             .OfType<CompUseWhenCast>())
                {
                    comp.OnUse(pawn);
                }
            }
            catch (System.Exception) {}
        }
    }
}
