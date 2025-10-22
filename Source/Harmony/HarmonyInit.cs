using HarmonyLib;
using Verse;
using System;

namespace Deployables
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("Deployables.HarmonyPatches");
            var original = AccessTools.Method(
                typeof(Verb),
                "TryStartCastOn",
                new Type[]
                {
                    typeof(LocalTargetInfo),
                    typeof(LocalTargetInfo),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool),
                    typeof(bool)
                }
            );

            if (original != null)
            {
                var postfix = AccessTools.Method(typeof(Patch_Verb_TryStartCastOn), nameof(Patch_Verb_TryStartCastOn.Postfix));
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            }
        }
    }
}