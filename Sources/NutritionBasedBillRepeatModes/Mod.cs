using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace NutritionBasedBillRepeatModes;

[UsedImplicitly]
public class Mod : Verse.Mod
{
    public Mod(ModContentPack content) : base(content)
    {
        GetSettings<ModSettings>();
    }

    public override string SettingsCategory() => nameof(SettingsCategory).TranslateNS();

    public override void WriteSettings()
    {
        base.WriteSettings();

        var settings = GetSettings<ModSettings>();

        FoodTracker.CountFoodsOutsideStorage = settings.CountFoodsOutsideStorage;
    }

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var settings = GetSettings<ModSettings>();

        var listing = new Listing_Standard();
        listing.Begin(inRect);

        listing.CheckboxLabeled(nameof(settings.CountFoodsOutsideStorage).TranslateNS(), ref settings.CountFoodsOutsideStorage,
            $"{nameof(settings.CountFoodsOutsideStorage)}_Desc".TranslateNS());

        listing.End();
    }
}