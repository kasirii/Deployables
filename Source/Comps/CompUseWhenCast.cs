using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;

namespace Deployables
{
    public class CompProps_CompUseWhenCast : CompProperties
    {
        public CompProps_CompUseWhenCast()
        {
            this.compClass = typeof(CompUseWhenCast);
        }
    }

    public interface IUseWhenCastEffect
    {
        void DoEffect(Pawn pawn, ThingWithComps parent);
    }

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
            var pawn = __instance.CasterPawn;
            var weapon = pawn.equipment?.Primary;
            if (pawn?.apparel == null) return;
            if (weapon.def == null) return;
            if (!(__instance is Verb_LaunchProjectile)) return;

            foreach (var comp in pawn.apparel.WornApparel.SelectMany(a => a.AllComps).OfType<CompUseWhenCast>())
            {
                comp.OnUse(pawn, __instance, castTarg);
            }
        }
    }

    public class CompUseWhenCast : ThingComp
    {
        public CompProps_CompUseWhenCast Props => (CompProps_CompUseWhenCast)props;

        public void OnUse(Pawn pawn, Verb verb, LocalTargetInfo target)
        {
            foreach (var effect in parent.AllComps.OfType<IUseWhenCastEffect>())
            {
                effect.DoEffect(pawn, parent);
            }
        }
    }

}
