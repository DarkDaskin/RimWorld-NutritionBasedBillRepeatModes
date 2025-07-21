using HarmonyLib;
using Verse;

namespace NutritionBasedBillRepeatModes;

[StaticConstructorOnStartup]
public static class Startup
{
    static Startup()
    {
        var harmony = new Harmony("NutritionBasedBillRepeatModes");
        harmony.PatchAll(typeof(Startup).Assembly);
    }
}