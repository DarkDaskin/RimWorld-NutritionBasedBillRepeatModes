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
        var foods = CountFoods(bill);

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

    // Roughly the same logic as in RecipeWorkerCounter.CountProducts, but for FoodDefs instead of the recipe product.
    private static IEnumerable<(ThingDef thingDef, int count)> CountFoods(Bill_Production bill)
    {
        var includeSlotGroup = bill.GetIncludeSlotGroup();
        var isBillFastPath = includeSlotGroup == null &&
                             bill is
                             {
                                 includeEquipped: false, limitToAllowedStuff: false, hpRange: { min: 0, max: 1 },
                                 qualityRange: { min: QualityCategory.Awful, max: QualityCategory.Legendary }
                             };

        foreach (var foodDef in FoodDefs)
        {
            var count = 0;

            // Fast path
            if (foodDef.CountAsResource && isBillFastPath)
            {
                count = bill.Map.resourceCounter.GetCount(foodDef) + GetCarriedCount(bill, foodDef);
                if (count > 0)
                    yield return (foodDef, count);

                continue;
            }

            if (includeSlotGroup == null)
            {
                
                count = bill.recipe.WorkerCounter.CountValidThings(bill.Map.listerThings.ThingsOfDef(foodDef), bill, foodDef);

                count += GetCarriedCount(bill, foodDef);

                if (foodDef.Minifiable)
                {
                    var minifiedThings = bill.Map.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing);
                    foreach (var thing in minifiedThings) 
                        count += CountThing(thing, bill, foodDef);
                }

                foreach (var haulSource in bill.Map.haulDestinationManager.AllHaulSourcesListForReading)
                foreach (var thing in haulSource.GetDirectlyHeldThings())
                    count += CountThing(thing, bill, foodDef);
            }
            else
            {
                foreach (var thing in includeSlotGroup.HeldThings) 
                    count += CountThing(thing, bill, foodDef);
            }

            if (bill.includeEquipped)
            {
                foreach (var pawn in bill.Map.mapPawns.FreeColonistsSpawned)
                foreach (var thing in pawn.EquippedWornOrInventoryThings)
                    count += CountThing(thing, bill, foodDef);
            }

            if (count > 0)
                yield return (foodDef, count);
        }
    }

    private static int GetCarriedCount(Bill_Production bill, ThingDef foodDef)
    {
        var carriedCount = 0;
        foreach (var pawn in bill.Map.mapPawns.FreeColonistsSpawned)
        {
            var carriedThing = pawn.carryTracker.CarriedThing;
            if (carriedThing != null)
                carriedCount += CountThing(carriedThing, bill, foodDef);
        }
        return carriedCount;
    }

    private static int CountThing(Thing thing, Bill_Production bill, ThingDef foodDef)
    {
        if (thing is MinifiedThing minifiedThing && bill.recipe.WorkerCounter.CountValidThing(minifiedThing.InnerThing, bill, foodDef))
            return minifiedThing.stackCount * minifiedThing.InnerThing.stackCount;

        if (bill.recipe.WorkerCounter.CountValidThing(thing, bill, foodDef))
            return thing.stackCount;

        return 0;
    }
}