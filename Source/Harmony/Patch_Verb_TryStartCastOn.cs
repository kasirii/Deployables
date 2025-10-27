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
                if (__instance is Verb_Shoot || __instance is CombatExtended.Verb_ShootCE)
                {
                    var pawn = __instance.CasterPawn;
                    if (pawn == null || !__instance.CasterIsPawn) return;

                    var equipment = pawn.equipment;
                    if (equipment == null) return;

                    var weaponDef = equipment.Primary?.def;
                    if (weaponDef == null || !weaponDef.IsRangedWeapon) return;

                    var wornApparel = pawn.apparel?.WornApparel;
                    if (wornApparel == null) return;

                    foreach (var comp in wornApparel
                                .SelectMany(a => a.AllComps)
                                .OfType<CompUseWhenCast>())
                    {
                        comp.OnUse(pawn);
                    }
                }
            }
            catch (System.Exception) {}
        }
    }
}
