using Verse;

namespace NutritionBasedBillRepeatModes;

public class ModSettings : Verse.ModSettings
{
    public bool CountFoodsOutsideStorage;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref CountFoodsOutsideStorage, nameof(CountFoodsOutsideStorage));
    }
}