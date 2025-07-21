using System;
using System.Collections.Generic;

namespace NutritionBasedBillRepeatModes;

internal static class Extensions
{
    public static int FindIndexOfSequence<T>(this List<T> list, params Predicate<T>[] predicates) =>
        FindIndexOfSequence(list, 0, predicates);

    public static int FindIndexOfSequence<T>(this List<T> list, int startIndex, params Predicate<T>[] predicates)
    {
        if (predicates.Length == 0)
            return -1;

        while (true)
        {
            var index = list.FindIndex(startIndex, predicates[0]);
            if (index < 0)
                return index;

            var matches = 1;
            for (var i = 1; i < predicates.Length; i++)
            {
                if (predicates[i].Invoke(list[index + i]))
                    matches++;
                else
                    break;
            }

            if (matches == predicates.Length)
                return index;

            startIndex = index + 1;
        }
    }
}