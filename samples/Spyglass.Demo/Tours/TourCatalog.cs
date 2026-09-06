using BlackBeard.Spyglass;

namespace Spyglass.Demo.Tours;

/// <summary>The tours this demo registers with <see cref="ITourService"/> at startup.</summary>
public static class TourCatalog
{
    public const string WelcomeTourKey = "welcome-tour";

    public const string AdvancedFeaturesTourKey = "advanced-features-tour";

    public static void RegisterAll(ITourService tourService)
    {
        tourService.Register(BuildWelcomeTour());
        tourService.Register(BuildAdvancedFeaturesTour());
    }

    private static TourDefinition BuildWelcomeTour()
    {
        var tour = new TourDefinition
        {
            Key = WelcomeTourKey,
            Name = "Welcome to Spyglass Demo",
            Introduction = "A quick look at the basics before you get started.",
            ShowEveryTime = true,
        };

        tour.Steps.Add(new TourStep
        {
            TargetId = "add-item-button",
            Title = "Add items",
            Description = "Use this button to add a new item to the list.",
        });

        tour.Steps.Add(new TourStep
        {
            TargetId = "settings-toggle",
            Title = "Tour preferences",
            Description = "Turn this off and tours won't prompt automatically next time - try restarting the app to see it stick.",
        });

        return tour;
    }

    private static TourDefinition BuildAdvancedFeaturesTour()
    {
        var tour = new TourDefinition
        {
            Key = AdvancedFeaturesTourKey,
            Name = "Advanced Features",
            Introduction = "A few controls that are trickier to spotlight, proving Spyglass handles them anyway.",
            ShowEveryTime = false,
        };

        tour.Steps.Add(new TourStep
        {
            TargetId = "left-edge-button",
            Title = "Flush left",
            Description = "This control sits hard against the window's left edge, so the callout falls back away from Left.",
        });

        tour.Steps.Add(new TourStep
        {
            TargetId = "corner-button",
            Title = "Bottom-right corner",
            Description = "Tucked into the corner - Spyglass still clamps the callout inside the window.",
        });

        tour.Steps.Add(new TourStep
        {
            TargetId = "scrolled-target",
            Title = "Scrolled into view",
            Description = "This target starts out of view inside a ScrollViewer; Spyglass scrolls it into view automatically.",
        });

        tour.Steps.Add(new TourStep
        {
            TargetId = "nested-target",
            Title = "Nested deep",
            Description = "This control lives inside a nested UserControl - resolved with a single TransformToVisual call.",
        });

        return tour;
    }
}
