using System.Linq;
using System.Collections.Generic;
using RimWorld;
using Verse;
using HarmonyLib;
using CombatExtended;

namespace Deployables
{
    public static class VerbUtils
    {
        private static Dictionary<Verb, VerbProperties> oldProperties = new Dictionary<Verb, VerbProperties>();

        public static void UpdatePawnVerbRanges(Pawn pawn, bool force)
        {
            if (pawn == null) return;

            bool hasPack = pawn.apparel?.WornApparel?
                .SelectMany(a => a.AllComps)
                .OfType<CompSpawnCover>()
                .Any(c => c?.Props?.coverThingDef != null) ?? false;

            if (hasPack && force)
            {
                var comp = pawn.apparel.WornApparel
                    .SelectMany(a => a.AllComps)
                    .OfType<CompSpawnCover>()
                    .FirstOrDefault();
                var coverThingDef = comp?.Props?.coverThingDef;
                if (coverThingDef == null) return;

                var turretProps = coverThingDef.building;
                if (turretProps == null || turretProps.turretGunDef == null) return;

                var gunDef = turretProps.turretGunDef;
                var gunVerb = gunDef.Verbs?.FirstOrDefault(v =>
                    typeof(Verb_ShootCE).IsAssignableFrom(v.verbClass) ||
                    typeof(Verb_Shoot).IsAssignableFrom(v.verbClass));

                if (gunVerb == null) return;

                float range = gunVerb.range;
                float minRange = gunVerb.minRange;

                foreach (var verb in GetAllVerbs(pawn))
                    ModifyVerb(verb, range, minRange);

            }
            else
            {
                foreach (var verb in GetAllVerbs(pawn))
                    TryResetVerbProps(verb);
            }
        }

        private static void ModifyVerb(Verb verb, float newRange, float minRange)
        {
            if (verb.verbProps.range == newRange && verb.verbProps.minRange == minRange) return;
            oldProperties[verb] = verb.verbProps;
            SetVerbRange(verb, newRange, minRange);
            
        }

        public static void SetVerbRange(Verb verb, float newRange, float minRange)
        {
            var propsCopy = verb.verbProps.MemberwiseClone() as VerbProperties;
            if (propsCopy == null) return;

            var rangeField = Traverse.Create(propsCopy).Field("range");
            if (rangeField != null)
                rangeField.SetValue(newRange);

            var minRangeField = Traverse.Create(propsCopy).Field("minRange");
            if (minRangeField != null)
                minRangeField.SetValue(minRange);

            verb.verbProps = propsCopy;
        }

        private static void TryResetVerbProps(Verb verb)
        {
            if (oldProperties.TryGetValue(verb, out var oldVerbProps)) verb.verbProps = oldVerbProps;
        }

        private static List<Verb> GetAllVerbs(Pawn pawn)
        {
            var allVerbs = new List<Verb>();

            if (pawn.VerbTracker != null)
            {
                //allVerbs.AddRange(pawn.VerbTracker.AllVerbs);
            }

            if (pawn.equipment != null)
            {
                allVerbs.AddRange(pawn.equipment.AllEquipmentVerbs.
                    Where(v => v != null && (v is Verb_Shoot || v is Verb_ShootCE)).ToList());
                //allVerbs.AddRange(pawn.equipment.AllEquipmentVerbs);
            }

            if (pawn.apparel != null)
            {
                //allVerbs.AddRange(pawn.apparel.AllApparelVerbs);
            }

            return allVerbs;
        }
    }
}
