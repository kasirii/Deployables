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

    public class CompUseWhenCast : ThingComp
    {
        public CompProps_CompUseWhenCast Props => (CompProps_CompUseWhenCast)props;

        public void OnUse(Pawn pawn, Verb verb, LocalTargetInfo target)
        {
            //Log.Message("OnUse start");
            foreach (var effect in parent.AllComps.OfType<IUseWhenCastEffect>())
            {
                effect.DoEffect(pawn, parent);
            }
        }
    }

}
