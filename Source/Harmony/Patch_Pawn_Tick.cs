using RimWorld;
using Verse;
using HarmonyLib;

namespace Deployables
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Tick))]
    public static class Patch_Pawn_Tick
    {
        private static void Postfix(Pawn __instance)
        {
            if (__instance.Spawned && !__instance.Dead && 
                __instance.RaceProps.intelligence >= Intelligence.ToolUser 
                && __instance.IsHashIntervalTick(600))
            {
                VerbUtils.UpdatePawnVerbRanges(__instance, true);
            }
        }
    }
}