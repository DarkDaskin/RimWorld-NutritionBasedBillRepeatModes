using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;

namespace NutritionBasedBillRepeatModes;

// TODO: Other mods compatibility

// ReSharper disable once InconsistentNaming
[UsedImplicitly]
[HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoWindowContents))]
internal static class Patch_Dialog_BillConfig_DoWindowContents
{
    // ReSharper disable InconsistentNaming
    private static readonly FieldInfo? Dialog_BillConfig_bill = typeof(Dialog_BillConfig).GetField("bill", AccessTools.all);
    private static readonly FieldInfo? Bill_Production_repeatMode = typeof(Bill_Production).GetField(nameof(Bill_Production.repeatMode));
    private static readonly FieldInfo? BillRepeatModeDefOf_TargetCount = typeof(BillRepeatModeDefOf).GetField(nameof(BillRepeatModeDefOf.TargetCount));
    // ReSharper restore InconsistentNaming

    private static readonly object?[] AllAccessedMembers = [Dialog_BillConfig_bill, Bill_Production_repeatMode, BillRepeatModeDefOf_TargetCount];

    [UsedImplicitly]
    public static bool Prepare() => AllAccessedMembers.All(o => o != null);

    [UsedImplicitly]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var instructionList = instructions.ToList();

        Predicate<CodeInstruction>[] predicates = [
            i => i.IsLdarg(0),
            i => i.LoadsField(Dialog_BillConfig_bill),
            i => i.LoadsField(Bill_Production_repeatMode),
            i => i.LoadsField(BillRepeatModeDefOf_TargetCount),
            i => i.Branches(out _),
        ];
        var startIndex = 0;
        while (true)
        {
            var index = instructionList.FindIndexOfSequence(startIndex, predicates);
            if (index < 0)
                break;

            var target = instructionList[index + 4].operand;
            instructionList.RemoveRange(index + 3, 2);
            CodeInstruction[] replacements = [
                CodeInstruction.Call(typeof(Patch_Dialog_BillConfig_DoWindowContents), nameof(IsTargetRepeatMode)),
                new CodeInstruction(OpCodes.Brfalse, target),
            ];
            instructionList.InsertRange(index + 3, replacements);

            startIndex = index + 1;
        }

        return instructionList;
    }

    private static bool IsTargetRepeatMode(BillRepeatModeDef repeatMode) =>
        repeatMode == BillRepeatModeDefOf.TargetCount || repeatMode == ModDefs.TargetNutritionAmount || repeatMode == ModDefs.TargetDaysOfFood;
}