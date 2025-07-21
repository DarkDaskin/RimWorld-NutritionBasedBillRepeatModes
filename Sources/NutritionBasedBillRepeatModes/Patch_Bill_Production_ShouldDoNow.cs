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
    private static readonly List<ThingDef> FoodDefs = DefDatabase<ThingDef>.AllDefs.Where(def => def.IsNutritionGivingIngestible && def.ingestible.HumanEdible).ToList();

    [UsedImplicitly]
    // ReSharper disable InconsistentNaming
    public static bool Prefix(Bill_Production __instance, ref bool __result)
    // ReSharper restore InconsistentNaming
    {
        if (__instance.repeatMode != ModDefs.TargetNutritionAmount && __instance.repeatMode != ModDefs.TargetDaysOfFood)
            return true;

        var amount = GetFoodAmount(__instance);

        if (__instance.pauseWhenSatisfied && amount >= __instance.targetCount)
            __instance.paused = true;

        if (!__instance.pauseWhenSatisfied && amount <= __instance.unpauseWhenYouHave)
            __instance.paused = false;

        __result = !__instance.paused && amount < __instance.targetCount;

        return false;
    }

    internal static int GetFoodAmount(Bill_Production bill)
    {
        var foods = GetFoods(bill);

        if (bill.repeatMode == ModDefs.TargetNutritionAmount)
        {
            var totalNutrition = foods.Sum(p => p.thingDef.ingestible.CachedNutrition * p.count);
            return (int)totalNutrition;
        }

        if (bill.repeatMode == ModDefs.TargetDaysOfFood)
        {
            var pawns = bill.Map.mapPawns.AllHumanlikeSpawned.Where(pawn =>
                pawn.Faction == Faction.OfPlayer || pawn.HostFaction == Faction.OfPlayer).ToList();
            var foodList = foods.Select(p =>
            {
                var thing = ThingMaker.MakeThing(p.thingDef);
                thing.stackCount = p.count;
                return thing;
            }).ToList();
            // TODO: Extract only relevant bits of code from this method
            return (int)DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood(pawns, foodList, bill.Map.Tile,
                IgnorePawnsInventoryMode.DontIgnore, Faction.OfPlayer);

            //var foodsDict = foods.ToLookup(p => p.thingDef, p => p.count).ToDictionary(g => g.Key, g => g.Sum());

            //var days = 0;
            //float totalNutritionPerDay = 0;

            //foreach (var pawn in bill.Map.mapPawns.PawnsInFaction(Faction.OfPlayer))
            //{
            //    IEnumerable<ThingDef> foodDefsAllowed = foodsDict.Keys;
            //    var foodPolicy = pawn.foodRestriction?.CurrentFoodPolicy;
            //    if (foodPolicy != null)
            //        foodDefsAllowed = foodDefsAllowed.Where(foodPolicy.Allows);
            //    foodDefsAllowed = pawn.IsAnimal ? 
            //        foodDefsAllowed.OrderByDescending(foodDef => foodDef.ingestible.optimalityOffsetFeedingAnimals) : 
            //        foodDefsAllowed.OrderByDescending(foodDef => foodDef.ingestible.optimalityOffsetHumanlikes);

            //    const int ticksPerDay = 60000;
            //    totalNutritionPerDay += ticksPerDay / (float)pawn.needs.food.TicksUntilHungryWhenFed *
            //                            pawn.needs.food.NutritionBetweenHungryAndFed;

            //    //pawn.needs.food.NutritionWanted
            //    //pawn.needs.food.TicksUntilHungryWhenFed
            //    //pawn.needs.food.NutritionBetweenHungryAndFed
            //}

            //if (totalNutritionPerDay == 0)
            //    return int.MaxValue;

            //var totalNutrition = foods.Sum(p => p.thingDef.ingestible.CachedNutrition * p.count);

            //return (int)(totalNutrition / totalNutritionPerDay);
        }

        throw new NotSupportedException($"{bill.repeatMode} is not supported.");
    }

    // TODO: Should be roughly the same logic as in RecipeWorkerCounter.CountProducts, but get products instead of counting them.
    private static IEnumerable<(ThingDef thingDef, int count)> GetFoods(Bill_Production bill)
    {
        foreach (var foodDef in FoodDefs)
        {
            var includeSlotGroup = bill.GetIncludeSlotGroup();

            // Fast path
            if (foodDef.CountAsResource && includeSlotGroup == null &&
                bill is
                {
                    includeEquipped: false, limitToAllowedStuff: false, hpRange: { min: 0, max: 1 },
                    qualityRange: { min: QualityCategory.Awful, max: QualityCategory.Legendary }
                })
            {
                var count = bill.Map.resourceCounter.GetCount(foodDef);
                if (count > 0)
                    yield return (foodDef, count);

                continue;
            }

            Log.WarningOnce($"Count of {foodDef} can't be determined.", "NutritionBasedBillRepeatModes.CantCountFood".GetHashCode());

            //var thingsOnMap = bill.Map.listerThings.ThingsOfDef(foodDef);
            //foreach (var thing in thingsOnMap)
            //    if (bill.recipe.WorkerCounter.CountValidThing(thing, bill, foodDef))
            //        yield return (foodDef, thing.stackCount);

        }
    }
}