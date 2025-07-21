using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace NutritionBasedBillRepeatModes;

// ReSharper disable once InconsistentNaming
[UsedImplicitly]
[HarmonyPatch(typeof(BillRepeatModeUtility), nameof(BillRepeatModeUtility.MakeConfigFloatMenu))]
internal static class Patch_BillRepeatModeUtility_MakeConfigFloatMenu
{
    // ReSharper disable InconsistentNaming
    private static readonly MethodInfo? Find_WindowStack_get = typeof(Find).GetProperty(nameof(Find.WindowStack))?.GetGetMethod();
    // ReSharper restore InconsistentNaming

    private static readonly object?[] AllAccessedMembers = [Find_WindowStack_get];

    [UsedImplicitly]
    public static bool Prepare() => AllAccessedMembers.All(o => o != null);

    [UsedImplicitly]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeInstruction? prevInstruction = null;

        foreach (var instruction in instructions)
        {
            yield return instruction;

            if (prevInstruction != null && prevInstruction.Calls(Find_WindowStack_get) && instruction.IsLdloc())
            {
                yield return new CodeInstruction(OpCodes.Dup);
                yield return CodeInstruction.LoadArgument(0);
                yield return CodeInstruction.Call(typeof(Patch_BillRepeatModeUtility_MakeConfigFloatMenu), nameof(AddMenuItems));
            }

            prevInstruction = instruction;
        }
    }

    private static void AddMenuItems(List<FloatMenuOption> options, Bill_Production bill)
    {
        options[^2].action = () => SetRepeatMode(BillRepeatModeDefOf.TargetCount);
        options.InsertRange(options.Count - 1, [
            new FloatMenuOption(ModDefs.TargetNutritionAmount.LabelCap, () => SetRepeatMode(ModDefs.TargetNutritionAmount)),
            new FloatMenuOption(ModDefs.TargetDaysOfFood.LabelCap, () => SetRepeatMode(ModDefs.TargetDaysOfFood, 2)),
        ]);

        void SetRepeatMode(BillRepeatModeDef repeatMode, int targetCount = 10)
        {
            if (bill.recipe.WorkerCounter.CanCountProducts(bill))
            {
                bill.repeatMode = repeatMode;
                bill.targetCount = targetCount;
            }
            else
                Messages.Message("RecipeCannotHaveTargetCount".Translate(), MessageTypeDefOf.RejectInput, false);
        }
    }
}