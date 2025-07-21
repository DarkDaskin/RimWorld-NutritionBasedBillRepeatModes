using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace NutritionBasedBillRepeatModes;

// ReSharper disable once InconsistentNaming
[UsedImplicitly]
[HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.ShouldDoNow))]
internal static class Patch_Bill_Production_ShouldDoNow
{
    

    [UsedImplicitly]
    // ReSharper disable InconsistentNaming
    public static bool Prefix(Bill_Production __instance, ref bool __result)
    // ReSharper restore InconsistentNaming
    {
        if (__instance.repeatMode != ModDefs.TargetNutritionAmount && __instance.repeatMode != ModDefs.TargetDaysOfFood)
            return true;

        var amount = FoodTracker.GetFoodAmount(__instance);

        if (__instance.pauseWhenSatisfied && amount >= __instance.targetCount)
            __instance.paused = true;

        if (!__instance.pauseWhenSatisfied && amount <= __instance.unpauseWhenYouHave)
            __instance.paused = false;

        __result = !__instance.paused && amount < __instance.targetCount;

        return false;
    }
}