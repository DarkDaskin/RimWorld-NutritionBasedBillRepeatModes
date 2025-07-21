using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace NutritionBasedBillRepeatModes;

// ReSharper disable once InconsistentNaming
[UsedImplicitly]
[HarmonyPatch(typeof(RecipeWorkerCounter), nameof(RecipeWorkerCounter.CountValidThings), [typeof(List<Thing>), typeof(Bill_Production), typeof(ThingDef)])]
internal static class Patch_RecipeWorkerCounter_CountValidThings
{
    [UsedImplicitly]
    // ReSharper disable InconsistentNaming
    public static bool Prefix(List<Thing> things, Bill_Production bill, ThingDef def, RecipeWorkerCounter __instance, out int __result)
    // ReSharper restore InconsistentNaming
    {
        __result = 0;

        foreach (var thing in things)
            if (__instance.CountValidThing(thing, bill, def))
                __result += thing.stackCount;

        return false;
    }
}