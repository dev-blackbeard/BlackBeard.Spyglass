# BlackBeard.Spyglass

A guided-tour ("coach marks") overlay for WPF applications. Wrap your window's content in one
control, tag the controls you want to point at, register a tour, and ask a service to run it —
fully MVVM, no code-behind required in your app.

## Install

```
dotnet add package BlackBeard.Spyglass
```

## Register (Prism)

```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterSpyglass();
}
```

Not using Prism? Just read/write `SpyglassLocator.Current` instead — it lazily creates a default
`TourService` the first time it's touched.

## Wrap your window

```xml
<Window ...
        xmlns:sg="http://schemas.blackbeard.dev/spyglass">
    <sg:TourOverlay ScrimBrush="#B2101010" SpotlightPadding="10" SpotlightCornerRadius="8">
        <ContentControl prism:RegionManager.RegionName="MainRegion" />
    </sg:TourOverlay>
</Window>
```

## Tag a control

```xml
<Button Content="Save" sg:Spyglass.TargetId="save-button" />
```

## Define a tour

```csharp
var tour = new TourDefinition
{
    Key = "main-tour",
    Name = "Welcome",
    Introduction = "Here's a quick look around.",
    ShowEveryTime = true,
};

tour.Steps.Add(new TourStep
{
    TargetId = "save-button",
    Title = "Save your work",
    Description = "Click here any time to save.",
});

tour.Steps.Add(new TourStep
{
    TargetId = "settings-button",
    Description = "Your preferences live here.",
});

tourService.Register(tour);
```

## Run it

```csharp
// On view activation - honours the persisted "show every time" setting.
await _tours.RequestAsync("main-tour");

// From a "Help > Take the tour" menu item - always runs.
await _tours.StartAsync("main-tour", showPrompt: true);
```

## `TourOverlay` reference

| Group | Properties |
| --- | --- |
| Scrim | `ScrimBrush` (default `#B2000000`), `IsAnimationEnabled` (`true`), `AnimationDuration` (180 ms) |
| Spotlight | `SpotlightPadding` (`Thickness`, default 8), `SpotlightCornerRadius` (default 6), `IsSpotlightInteractive` (`false`) |
| Callout | `CalloutGap` (12), `HostMargin` (16), `PlacementPriority` (default Left, Bottom, Right, Top), `CalloutStyle`, `CalloutTemplate` |
| Prompt | `PromptStyle`, `PromptTemplate` |
| Buttons | `NextButtonStyle`, `EndButtonStyle`, `StartButtonStyle`, `SkipButtonStyle`, `DontShowAgainCheckBoxStyle` |
| Text | `NextButtonContent`, `FinishButtonContent`, `EndButtonContent`, `StartButtonContent`, `SkipButtonContent`, `DontShowAgainContent`, `StepCounterFormat` |
| Read-only | `IsTourActive`, `CurrentStep`, `CurrentStepIndex`, `StepCount`, `CurrentPlacement` |
| Wiring | `TourService` (optional; falls back to `SpyglassLocator.Current`) |

## Where preferences are stored

The default `JsonFileTourStateStore` writes to:

```
%LOCALAPPDATA%\{CompanyName}\{ProductName}\spyglass.tours.json
```

`CompanyName`/`ProductName` come from your entry assembly's `AssemblyCompanyAttribute` /
`AssemblyProductAttribute`. Pass `SpyglassOptions.StorageFilePath` to `RegisterSpyglass(...)` to
override the location, or `SpyglassOptions.StateStore` to supply your own `ITourStateStore`
(e.g. one backed by `Properties.Settings`). Writes are atomic and never throw; if persistence
fails for any reason, Spyglass logs a warning and keeps working in memory for that session.

## License

Apache-2.0 - see [LICENSE](LICENSE).
