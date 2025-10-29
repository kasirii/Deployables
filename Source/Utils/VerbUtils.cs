using System.Collections.Generic;
using RimWorld;
using Verse;
using CombatExtended;

namespace Deployables
{
    public static class VerbUtils
    {
        private static readonly Dictionary<Verb, VerbProperties> oldProperties = new Dictionary<Verb, VerbProperties>();

        public static void UpdatePawnVerbRanges(Pawn pawn, CompSpawnCover coverComp, bool force)
        {
            if (pawn == null) return;

            var verbs = pawn.equipment?.AllEquipmentVerbs;
            if (verbs == null) return;

            if (force)
            {
                var coverThingDef = coverComp?.Props?.coverThingDef;
                var turretProps = coverThingDef?.building;
                var gunDef = turretProps?.turretGunDef;
                if (gunDef?.Verbs == null || gunDef.Verbs.Count == 0) return;

                VerbProperties gunVerb = null;
                foreach (var v in gunDef.Verbs)
                {
                    var cls = v.verbClass;
                    if (cls == typeof(Verb_Shoot) || cls == typeof(Verb_ShootCE))
                    {
                        gunVerb = v;
                        break;
                    }
                }
                if (gunVerb == null) return;

                float range = gunVerb.range;
                float minRange = gunVerb.minRange;

                foreach (var verb in verbs)
                {
                    if (verb is Verb_Shoot || verb is Verb_ShootCE)
                        ModifyVerb(verb, range, minRange);
                }

            }
            else
            {
                foreach (var verb in verbs)
                {
                    if (verb is Verb_Shoot || verb is Verb_ShootCE)
                        TryResetVerbProps(verb);
                }
            }
        }

        private static void ModifyVerb(Verb verb, float newRange, float minRange)
        {
            var props = verb.verbProps;
            if (props == null || (props.range == newRange && props.minRange == minRange)) return;

            if (!oldProperties.ContainsKey(verb))
                oldProperties[verb] = props;

            VerbProperties propsCopy = verb.verbProps.MemberwiseClone();

            propsCopy.range = newRange;
            propsCopy.minRange = minRange;
            verb.verbProps = propsCopy;
        }

        private static void TryResetVerbProps(Verb verb)
        {
            if (oldProperties.TryGetValue(verb, out var oldVerbProps))
            {
                verb.verbProps = oldVerbProps;
                oldProperties.Remove(verb);
            }
        }
    }
}
