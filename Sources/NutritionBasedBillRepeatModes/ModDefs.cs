using RimWorld;
using Verse;

namespace NutritionBasedBillRepeatModes;

public static class ModDefs
{
    public static readonly BillRepeatModeDef TargetNutritionAmount = DefDatabase<BillRepeatModeDef>.GetNamed(nameof(TargetNutritionAmount));
    public static readonly BillRepeatModeDef TargetDaysOfFood = DefDatabase<BillRepeatModeDef>.GetNamed(nameof(TargetDaysOfFood));
}