using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;

namespace NutritionBasedBillRepeatModes;

// ReSharper disable once InconsistentNaming
[UsedImplicitly]
[HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.RepeatInfoText), MethodType.Getter)]
internal static class Patch_Bill_Production_RepeatInfoText
{
    [UsedImplicitly]
    // ReSharper disable InconsistentNaming
    public static bool Prefix(Bill_Production __instance, ref string __result)
    // ReSharper restore InconsistentNaming
    {
        if (__instance.repeatMode != ModDefs.TargetNutritionAmount && __instance.repeatMode != ModDefs.TargetDaysOfFood)
            return true;

        var amount = Patch_Bill_Production_ShouldDoNow.GetFoodAmount(__instance);
        __result = $"{amount}/{__instance.targetCount}";

        return false;
    }
}