# Build prompt: `BlackBeard.Spyglass`
> Paste the whole of this file into Claude Code as the opening message for a fresh, empty repo.
> Keep it in the repo afterwards as `docs/SPEC.md` — it doubles as the design record.
---
You are building a new, self-contained .NET class library that ships as a public NuGet package. Work
through it in stages, build and run the tests after each stage, and don't move on while the solution
is red.
## 1. What it is
`BlackBeard.Spyglass` is a guided-tour ("coach marks") overlay for WPF applications. A host app wraps
its main window content in a single control, tags any controls it wants to point at with an attached
property, registers a tour object, and asks a service to run it. The overlay darkens the whole window
except for a transparent cut-out ("the spotlight") over the tagged control, and floats a callout with
the step description plus **Next** and **End tour** buttons beside it.
- Target framework: `net7.0-windows`, `UseWPF=true`
- Language: C# 11, `Nullable=enable`, `TreatWarningsAsErrors=true`
- The consuming app is Prism 8.1.97 (`Prism.Core` / `Prism.Wpf` / `Prism.DryIoc`) and fully MVVM, so
  nothing in this library may require code-behind in the host app, and every knob must be settable
  from XAML or from a view model.
### Dependency policy
- The library references **`Prism.Core` 8.1.97 only** — used for the `IContainerRegistry` extension
  method and for optional `IEventAggregator` publishing. Nothing else.
- Do **not** reference `Prism.Wpf`, `Prism.DryIoc`, DryIoc, or any third-party UI/JSON library.
  Serialization uses in-box `System.Text.Json`.
- If Prism's `IEventAggregator` is not supplied, the library must work identically using plain CLR
  events. Prism is a convenience, never a requirement.
## 2. Repository layout
```
/BlackBeard.Spyglass.sln
/Directory.Build.props            # shared TFM, nullable, version, authors, deterministic build
/src/BlackBeard.Spyglass/         # the package
/samples/Spyglass.Demo/           # WPF + Prism.DryIoc demo app
/tests/BlackBeard.Spyglass.Tests/ # xunit
/README.md  /LICENSE  /.gitignore /.editorconfig
/.github/workflows/ci.yml         # build + test + pack on windows-latest
```
Package metadata on the library project: `PackageId=BlackBeard.Spyglass`, `Authors=BlackBeard`,
`GenerateDocumentationFile=true`, `IncludeSymbols=true`, `SymbolPackageFormat=snupkg`,
`PackageReadmeFile=README.md`, `PackageTags=wpf;mvvm;onboarding;tour;walkthrough;coachmarks;prism`.
XML doc comments on **every** public member.
Add an `XmlnsDefinition` attribute mapping every public namespace to
`http://schemas.blackbeard.dev/spyglass`, so consumers need only one XAML namespace declaration.
## 3. Public API — use these exact names
Root namespace `BlackBeard.Spyglass`.
### 3.1 Tour model (plain POCOs, buildable in a view model or a static factory)
```csharp
public sealed class TourDefinition
{
    public string Key { get; set; } = "";          // unique; also the persistence key
    public string Name { get; set; } = "";         // shown on the start prompt
    public string? Introduction { get; set; }      // optional blurb on the start prompt
    public bool ShowEveryTime { get; set; } = true;// seed value if nothing is persisted yet
    public IList<TourStep> Steps { get; } = new List<TourStep>();
}
public sealed class TourStep
{
    public string TargetId { get; set; } = "";     // matches Spyglass.TargetId on a control
    public string Description { get; set; } = "";  // the callout text
    public string? Title { get; set; }
    public Thickness? SpotlightPadding { get; set; }      // overrides the overlay default
    public CalloutPlacement? PreferredPlacement { get; set; } // tried first, before the priority list
    public bool AllowInteraction { get; set; }     // can the user click through the hole on this step
}
public enum CalloutPlacement { Left, Bottom, Right, Top, Center }
public enum TourOutcome { Suppressed, Skipped, Completed, Ended, Failed }
public sealed record TourResult(string TourKey, TourOutcome Outcome, int LastStepIndex);
```
### 3.2 Service
```csharp
public interface ITourService
{
    void Register(TourDefinition tour);                  // idempotent by Key; last wins + trace warning
    bool TryGet(string key, out TourDefinition tour);
    IReadOnlyCollection<TourDefinition> RegisteredTours { get; }
    bool IsRunning { get; }
    string? RunningTourKey { get; }
    Task<TourResult> RequestAsync(string key, CancellationToken ct = default); // honours ShowEveryTime
    Task<TourResult> StartAsync(string key, bool showPrompt = false, CancellationToken ct = default); // ignores it
    void Next();
    void End();
    bool GetShowEveryTime(string key);
    void SetShowEveryTime(string key, bool value);
    event EventHandler<TourStateChangedEventArgs>? StateChanged;
}
```
- `TourService` is the implementation. It is UI-thread affine: every public method marshals to the
  dispatcher if called from elsewhere.
- `RequestAsync` is what an app calls on view activation. If the persisted `ShowEveryTime` is false it
  returns `Suppressed` immediately without rendering anything.
- `StartAsync` is what a "Help → Take the tour" menu item calls; it runs regardless of the setting.
- `TourStateChangedEventArgs` carries tour key, step index, step count and a `TourState` enum
  (`Prompting, Running, Ended`). When an `IEventAggregator` is available, also publish a
  `TourStateChangedEvent : PubSubEvent<TourStateChangedEventArgs>`.
Registration, both DI and static:
```csharp
public static class SpyglassRegistration
{
    // Prism: registers ITourService + ITourStateStore as singletons AND sets SpyglassLocator.Current
    public static IContainerRegistry RegisterSpyglass(this IContainerRegistry registry, Action<SpyglassOptions>? configure = null);
}
public static class SpyglassLocator
{
    public static ITourService Current { get; set; } // lazily creates a default TourService if never set
}
```
`SpyglassOptions`: `StateStore`, `StorageFilePath`, `EventAggregator`.
### 3.3 Persistence (the "non-volatile" bit)
```csharp
public interface ITourStateStore
{
    bool? GetShowEveryTime(string tourKey);   // null = never persisted, fall back to TourDefinition.ShowEveryTime
    void SetShowEveryTime(string tourKey, bool value);
    void Flush();
}
```
- `JsonFileTourStateStore` (default): writes
  `%LOCALAPPDATA%\{CompanyName}\{ProductName}\spyglass.tours.json`, resolved from the entry assembly,
  with an override path. Writes atomically (temp file + move), never throws out of the store — log and
  degrade to in-memory on IO failure.
- `InMemoryTourStateStore` for tests and for hosts that want to bind this to their own
  `Properties.Settings`.
Persistence happens the moment the user's choice is known: if the "Don't show this again" checkbox is
ticked, then on **Start**, **Skip**, **End tour** or completion, persist `ShowEveryTime = false` and
`Flush()`.
### 3.4 Attached properties — tagging targets
```csharp
public static class Spyglass
{
    public static readonly DependencyProperty TargetIdProperty;        // attached string, settable
    public static readonly DependencyProperty IsSpotlightedProperty;   // attached readonly bool, for styling
}
```
Usage in any view, at any depth:
```xml
<Button Content="Save" sg:Spyglass.TargetId="save-button" />
```
Implementation notes:
- The property-changed handler registers the element in a static registry keyed by id, holding a
  **`WeakReference<FrameworkElement>`** so tagged controls never leak.
- Hook `Loaded`/`Unloaded` to keep the registry honest; purge dead entries on lookup.
- Duplicate ids: prefer the entry that is currently loaded, visible and connected to a
  `PresentationSource`. If several qualify, take the most recently loaded and emit a trace warning.
- `IsSpotlighted` is set to true on the active step's target so the host can style it (e.g. a glow) —
  and reset on step change and tour end.
### 3.5 The control
**One** public control: `TourOverlay : ContentControl`. The host wraps its main window content:
```xml
<Window ...>
    <sg:TourOverlay ScrimBrush="#B2101010" SpotlightPadding="10" SpotlightCornerRadius="8">
        <ContentControl prism:RegionManager.RegionName="MainRegion" />
    </sg:TourOverlay>
</Window>
```
Dependency properties (all with sensible defaults, all settable in XAML/styles):
| Group | Properties |
| --- | --- |
| Scrim | `ScrimBrush` (default `#B2000000`), `IsAnimationEnabled` (true), `AnimationDuration` (180 ms) |
| Spotlight | `SpotlightPadding` (`Thickness`, default 8), `SpotlightCornerRadius` (default 6), `IsSpotlightInteractive` (false) |
| Callout | `CalloutGap` (12), `HostMargin` (16 — keep-out from the window edges), `PlacementPriority` (`IList<CalloutPlacement>`, default **Left, Bottom, Right, Top**), `CalloutStyle`, `CalloutTemplate` |
| Buttons | `NextButtonStyle`, `EndButtonStyle`, `StartButtonStyle`, `SkipButtonStyle`, `DontShowAgainCheckBoxStyle` |
| Text | `NextButtonContent` ("Next"), `FinishButtonContent` ("Done"), `EndButtonContent` ("End tour"), `StartButtonContent` ("Start tour"), `SkipButtonContent` ("Skip"), `DontShowAgainContent` ("Don't show this again"), `StepCounterFormat` ("{0} of {1}") |
| Read-only | `IsTourActive`, `CurrentStep`, `CurrentStepIndex`, `StepCount`, `CurrentPlacement` |
| Wiring | `TourService` (optional; falls back to `SpyglassLocator.Current`) |
The buttons must be plain `System.Windows.Controls.Button` and the checkbox a plain `CheckBox`, so the
`*Style` properties compose normally with the host's own styles. Do not restyle them beyond the
theme defaults.
Template parts (declare with `[TemplatePart]`): `PART_Content` (`ContentPresenter`), `PART_Scrim`
(`Path`), `PART_Callout` (`ContentControl`), `PART_Prompt` (`ContentControl`). Ship default styles in
`Themes/Generic.xaml` with the `ThemeInfo` assembly attribute.
The overlay registers itself with the service on `Loaded` and detaches on `Unloaded`. If a tour is
requested and no overlay is attached, return `TourOutcome.Failed` and trace a warning — never throw
into the host's UI thread.
## 4. Behaviour
### 4.1 Start prompt
On `RequestAsync` (or `StartAsync(showPrompt: true)`) show a centred card over a full scrim with no
hole: tour `Name`, `Introduction`, a **Start tour** button, a **Skip** button, and a **Don't show this
again** checkbox. Skip ends with `TourOutcome.Skipped`; Start advances to step 0.
### 4.2 Spotlight geometry
1. Resolve the step's `TargetId` from the registry.
2. `target.BringIntoView()`, then wait one layout pass (`Dispatcher.InvokeAsync(..., DispatcherPriority.Loaded)`).
3. `var bounds = target.TransformToVisual(this).TransformBounds(new Rect(target.RenderSize));`
   — `this` being the `TourOverlay`, which is why nesting depth, `ScrollViewer`s and render transforms
   are handled for free. **Do not** walk parents manually or use `PointToScreen`.
4. Inflate by the step's `SpotlightPadding` (else the overlay default), clamp to the overlay bounds.
5. Draw with one `Path`:
   ```csharp
   var group = new GeometryGroup { FillRule = FillRule.EvenOdd };
   group.Children.Add(new RectangleGeometry(new Rect(RenderSize)));          // full scrim
   group.Children.Add(_holeGeometry);                                        // rounded hole
   ```
   Keep `_holeGeometry` alive across steps and animate its `Rect` with a `RectAnimation` +
   `QuadraticEase` so the hole glides between targets.
6. EvenOdd means the hole isn't filled, so hit-testing already falls through to the app underneath.
   When `IsSpotlightInteractive` (or `TourStep.AllowInteraction`) is false, place a transparent
   `Rectangle` over the hole rect to swallow clicks.
While a step is showing, subscribe to `LayoutUpdated` and `SizeChanged`; if the recomputed rect moves
by more than 0.5 px, update it (short animation) so window resizes, DPI changes and scrolling keep the
spotlight glued to the control.
### 4.3 Callout placement
Put this in a **pure static class `CalloutPlacementCalculator`** with no control dependencies so it can
be unit tested directly:
```csharp
public static CalloutLayout Resolve(Size hostSize, Rect hole, Size callout, double gap,
                                    double hostMargin, IReadOnlyList<CalloutPlacement> priority);
```
Algorithm — try `TourStep.PreferredPlacement` first if set, then the priority list, default
**Left → Bottom → Right → Top**:
- **Left**: `x = hole.Left - gap - callout.Width`, `y` centred on the hole vertically.
- **Bottom**: `y = hole.Bottom + gap`, `x` centred horizontally.
- **Right**: `x = hole.Right + gap`, `y` centred vertically.
- **Top**: `y = hole.Top - gap - callout.Height`, `x` centred horizontally.
Clamp the cross-axis coordinate into `[hostMargin, hostSize - callout - hostMargin]`. Accept the first
candidate whose primary axis fits entirely inside the host with the margin respected. If none fit,
choose the side with the most free space, clamp fully into the host, and allow overlap with the hole as
a last resort; if the hole covers more than ~60% of the host, use `Center`.
### 4.4 Callout content
Description text (`TourStep.Description`), optional title, optional step counter, then **Next** and
**End tour**. On the final step, `Next` shows `FinishButtonContent` and completes the tour with
`TourOutcome.Completed`. `End tour` ends immediately with `TourOutcome.Ended`.
### 4.5 Ending and reset
`End()` — and skip, completion, and `Unloaded` — must, in one place:
- stop animations, clear `_holeGeometry`, collapse the scrim/callout/prompt,
- unsubscribe `LayoutUpdated` / `SizeChanged` / keyboard hooks,
- clear `IsSpotlighted` on the last target and clear `CurrentStep`, `CurrentStepIndex`, `StepCount`,
  `IsTourActive`, `RunningTourKey`,
- persist the "don't show again" choice if ticked, and `Flush()`,
- restore the previously focused element,
- complete the pending `TaskCompletionSource<TourResult>` exactly once.
Ending must be safe to call twice, and safe to call while no tour is running.
### 4.6 Keyboard and focus
`Esc` ends the tour, `Enter`/`Right` advances. Move focus to the Next button when a step renders, keep
focus inside the overlay while active, restore it on end. Set `AutomationProperties.Name`/`HelpText`
so screen readers announce the step description.
### 4.7 Failure modes (all traced through a `SpyglassTrace` `TraceSource`, none fatal)
| Situation | Behaviour |
| --- | --- |
| Target id not found / collapsed / zero-size | Skip that step; if every step fails, end with `Failed` |
| Tour key not registered | `Failed` |
| No overlay attached | `Failed` |
| Tour with zero steps | `Completed` immediately |
| Second tour requested while one runs | End the first, then run the second |
## 5. Sample app — `samples/Spyglass.Demo`
WPF + `Prism.DryIoc` 8.1.97, `net7.0-windows`. Must demonstrate:
- `containerRegistry.RegisterSpyglass()` in `RegisterTypes`, tours registered from a
  `TourCatalog` class, and `ITourService` injected into a view model.
- `TourOverlay` wrapping a Prism region in `MainWindow`.
- Targets at genuinely awkward positions: hard against the left edge (forces the Bottom fallback),
  bottom-right corner, one inside a `ScrollViewer` that must scroll into view, one inside a nested
  `UserControl` several levels deep.
- Two tours: one auto-requested on first view activation via `RequestAsync`, one launched from a
  "Help → Replay tour" menu item via `StartAsync`.
- A settings toggle bound to `GetShowEveryTime`/`SetShowEveryTime`, proving persistence survives a
  restart.
- Custom `NextButtonStyle` / `EndButtonStyle` / `ScrimBrush` set in XAML, proving the styling hooks.
## 6. Tests — `tests/BlackBeard.Spyglass.Tests` (xunit)
Cover at minimum:
- `CalloutPlacementCalculator`: honours priority order; falls back Left→Bottom when the hole hugs the
  left edge; Bottom→Top when the hole is at the bottom; clamps into the host; centres when the hole
  fills the window; respects `PreferredPlacement`.
- `JsonFileTourStateStore`: round-trip, unknown key returns null, corrupt file degrades gracefully.
- `TourService`: `RequestAsync` returns `Suppressed` when persisted false; `StartAsync` runs anyway;
  ticking "don't show again" persists false on every exit path; `End()` is idempotent; registering a
  duplicate key replaces.
- Target registry: weak references release; dead entries purged; duplicate ids resolve to the loaded one.
Use `InMemoryTourStateStore` and a fake `ITourPresenter` so service tests need no UI. Where a WPF
object is unavoidable, use `xunit.stafact`.
## 7. README
Short and practical: install line, the `RegisterSpyglass()` call, the `TourOverlay` wrapper XAML, one
tagged control, a ten-line tour definition, `await _tours.RequestAsync("main-tour")`, then a table of
the overlay's dependency properties and a note on where the state file lives.
## 8. Definition of done
- `dotnet build -c Release` clean with zero warnings.
- `dotnet test` green.
- `dotnet pack -c Release` produces `BlackBeard.Spyglass.<version>.nupkg` + `.snupkg` with README,
  license and XML docs inside.
- The sample app runs, and every scenario in §5 behaves as specified.
- Public API is exactly as named in §3 — if you need to deviate, say so and explain why before
  changing it.
Start by scaffolding the solution and `Directory.Build.props`, then the model + service + store (fully
testable, no UI), then the attached-property registry, then `CalloutPlacementCalculator` with its
tests, and only then the `TourOverlay` control and `Generic.xaml`. Sample app last.
