using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using BlackBeard.Spyglass.Diagnostics;
using BlackBeard.Spyglass.Placement;

namespace BlackBeard.Spyglass.Controls;

/// <summary>
/// Wraps a window's content and renders the darkened scrim, spotlight cut-out, and callout for a
/// running <see cref="ITourService"/> tour. Attach once around the host window's content; everything
/// else is driven by the tour service.
/// </summary>
[TemplatePart(Name = PartContent, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PartScrim, Type = typeof(Path))]
[TemplatePart(Name = PartCallout, Type = typeof(ContentControl))]
[TemplatePart(Name = PartPrompt, Type = typeof(ContentControl))]
public sealed class TourOverlay : ContentControl, ITourPresenter
{
    private const string PartRoot = "PART_Root";
    private const string PartContent = "PART_Content";
    private const string PartScrim = "PART_Scrim";
    private const string PartCallout = "PART_Callout";
    private const string PartPrompt = "PART_Prompt";

    #region Dependency properties

    /// <summary>Identifies the <see cref="ScrimBrush"/> dependency property.</summary>
    public static readonly DependencyProperty ScrimBrushProperty = DependencyProperty.Register(
        nameof(ScrimBrush), typeof(Brush), typeof(TourOverlay),
        new FrameworkPropertyMetadata(CreateFrozenBrush(0xB2, 0, 0, 0), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="IsAnimationEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsAnimationEnabledProperty = DependencyProperty.Register(
        nameof(IsAnimationEnabled), typeof(bool), typeof(TourOverlay), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="AnimationDuration"/> dependency property.</summary>
    public static readonly DependencyProperty AnimationDurationProperty = DependencyProperty.Register(
        nameof(AnimationDuration), typeof(Duration), typeof(TourOverlay),
        new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(180))));

    /// <summary>Identifies the <see cref="SpotlightPadding"/> dependency property.</summary>
    public static readonly DependencyProperty SpotlightPaddingProperty = DependencyProperty.Register(
        nameof(SpotlightPadding), typeof(Thickness), typeof(TourOverlay), new PropertyMetadata(new Thickness(8)));

    /// <summary>Identifies the <see cref="SpotlightCornerRadius"/> dependency property.</summary>
    public static readonly DependencyProperty SpotlightCornerRadiusProperty = DependencyProperty.Register(
        nameof(SpotlightCornerRadius), typeof(double), typeof(TourOverlay), new PropertyMetadata(6.0));

    /// <summary>Identifies the <see cref="IsSpotlightInteractive"/> dependency property.</summary>
    public static readonly DependencyProperty IsSpotlightInteractiveProperty = DependencyProperty.Register(
        nameof(IsSpotlightInteractive), typeof(bool), typeof(TourOverlay), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="CalloutGap"/> dependency property.</summary>
    public static readonly DependencyProperty CalloutGapProperty = DependencyProperty.Register(
        nameof(CalloutGap), typeof(double), typeof(TourOverlay), new PropertyMetadata(12.0));

    /// <summary>Identifies the <see cref="HostMargin"/> dependency property.</summary>
    public static readonly DependencyProperty HostMarginProperty = DependencyProperty.Register(
        nameof(HostMargin), typeof(double), typeof(TourOverlay), new PropertyMetadata(16.0));

    /// <summary>Identifies the <see cref="PlacementPriority"/> dependency property.</summary>
    public static readonly DependencyProperty PlacementPriorityProperty = DependencyProperty.Register(
        nameof(PlacementPriority), typeof(IList<CalloutPlacement>), typeof(TourOverlay), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="CalloutStyle"/> dependency property.</summary>
    public static readonly DependencyProperty CalloutStyleProperty = DependencyProperty.Register(
        nameof(CalloutStyle), typeof(Style), typeof(TourOverlay), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="CalloutTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty CalloutTemplateProperty = DependencyProperty.Register(
        nameof(CalloutTemplate), typeof(DataTemplate), typeof(TourOverlay), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="NextButtonStyle"/> dependency property.</summary>
    public static readonly DependencyProperty NextButtonStyleProperty = DependencyProperty.Register(
        nameof(NextButtonStyle), typeof(Style), typeof(TourOverlay), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="EndButtonStyle"/> dependency property.</summary>
    public static readonly DependencyProperty EndButtonStyleProperty = DependencyProperty.Register(
        nameof(EndButtonStyle), typeof(Style), typeof(TourOverlay), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="StartButtonStyle"/> dependency property.</summary>
    public static readonly DependencyProperty StartButtonStyleProperty = DependencyProperty.Register(
        nameof(StartButtonStyle), typeof(Style), typeof(TourOverlay), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="SkipButtonStyle"/> dependency property.</summary>
    public static readonly DependencyProperty SkipButtonStyleProperty = DependencyProperty.Register(
        nameof(SkipButtonStyle), typeof(Style), typeof(TourOverlay), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="DontShowAgainCheckBoxStyle"/> dependency property.</summary>
    public static readonly DependencyProperty DontShowAgainCheckBoxStyleProperty = DependencyProperty.Register(
        nameof(DontShowAgainCheckBoxStyle), typeof(Style), typeof(TourOverlay), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="NextButtonContent"/> dependency property.</summary>
    public static readonly DependencyProperty NextButtonContentProperty = DependencyProperty.Register(
        nameof(NextButtonContent), typeof(object), typeof(TourOverlay), new PropertyMetadata("Next"));

    /// <summary>Identifies the <see cref="FinishButtonContent"/> dependency property.</summary>
    public static readonly DependencyProperty FinishButtonContentProperty = DependencyProperty.Register(
        nameof(FinishButtonContent), typeof(object), typeof(TourOverlay), new PropertyMetadata("Done"));

    /// <summary>Identifies the <see cref="EndButtonContent"/> dependency property.</summary>
    public static readonly DependencyProperty EndButtonContentProperty = DependencyProperty.Register(
        nameof(EndButtonContent), typeof(object), typeof(TourOverlay), new PropertyMetadata("End tour"));

    /// <summary>Identifies the <see cref="StartButtonContent"/> dependency property.</summary>
    public static readonly DependencyProperty StartButtonContentProperty = DependencyProperty.Register(
        nameof(StartButtonContent), typeof(object), typeof(TourOverlay), new PropertyMetadata("Start tour"));

    /// <summary>Identifies the <see cref="SkipButtonContent"/> dependency property.</summary>
    public static readonly DependencyProperty SkipButtonContentProperty = DependencyProperty.Register(
        nameof(SkipButtonContent), typeof(object), typeof(TourOverlay), new PropertyMetadata("Skip"));

    /// <summary>Identifies the <see cref="DontShowAgainContent"/> dependency property.</summary>
    public static readonly DependencyProperty DontShowAgainContentProperty = DependencyProperty.Register(
        nameof(DontShowAgainContent), typeof(object), typeof(TourOverlay), new PropertyMetadata("Don't show this again"));

    /// <summary>Identifies the <see cref="StepCounterFormat"/> dependency property.</summary>
    public static readonly DependencyProperty StepCounterFormatProperty = DependencyProperty.Register(
        nameof(StepCounterFormat), typeof(string), typeof(TourOverlay), new PropertyMetadata("{0} of {1}"));

    private static readonly DependencyPropertyKey IsTourActivePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsTourActive), typeof(bool), typeof(TourOverlay), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsTourActive"/> dependency property.</summary>
    public static readonly DependencyProperty IsTourActiveProperty = IsTourActivePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey CurrentStepPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CurrentStep), typeof(TourStep), typeof(TourOverlay), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="CurrentStep"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentStepProperty = CurrentStepPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey CurrentStepIndexPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CurrentStepIndex), typeof(int), typeof(TourOverlay), new PropertyMetadata(-1));

    /// <summary>Identifies the <see cref="CurrentStepIndex"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentStepIndexProperty = CurrentStepIndexPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey StepCountPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(StepCount), typeof(int), typeof(TourOverlay), new PropertyMetadata(0));

    /// <summary>Identifies the <see cref="StepCount"/> dependency property.</summary>
    public static readonly DependencyProperty StepCountProperty = StepCountPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey CurrentPlacementPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CurrentPlacement), typeof(CalloutPlacement?), typeof(TourOverlay), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="CurrentPlacement"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentPlacementProperty = CurrentPlacementPropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="TourService"/> dependency property.</summary>
    public static readonly DependencyProperty TourServiceProperty = DependencyProperty.Register(
        nameof(TourService), typeof(ITourService), typeof(TourOverlay), new PropertyMetadata(null));

    #endregion

    private Grid? _rootPart;
    private Path? _scrimPart;
    private ContentControl? _calloutPart;
    private ContentControl? _promptPart;
    private Rectangle? _interactionBlocker;

    private RectangleGeometry? _fullRectGeometry;
    private RectangleGeometry? _holeGeometry;
    private Rect? _lastHoleBounds;

    private FrameworkElement? _currentTarget;
    private FrameworkElement? _previousSpotlightTarget;
    private FrameworkElement? _previouslyFocused;
    private Button? _nextButtonElement;
    private Button? _startButtonElement;
    private bool _dontShowAgain;

    private TourService? _boundService;
    private ICommand? _nextCommand;
    private ICommand? _endCommand;

    static TourOverlay()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TourOverlay), new FrameworkPropertyMetadata(typeof(TourOverlay)));
    }

    /// <summary>Creates a <see cref="TourOverlay"/>.</summary>
    public TourOverlay()
    {
        SetCurrentValue(PlacementPriorityProperty, new List<CalloutPlacement>
        {
            CalloutPlacement.Left,
            CalloutPlacement.Bottom,
            CalloutPlacement.Right,
            CalloutPlacement.Top,
        });

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>The brush used to darken the window outside the spotlight. Default <c>#B2000000</c>.</summary>
    public Brush ScrimBrush
    {
        get => (Brush)GetValue(ScrimBrushProperty);
        set => SetValue(ScrimBrushProperty, value);
    }

    /// <summary>Whether the spotlight glides between targets with an animation. Default <see langword="true"/>.</summary>
    public bool IsAnimationEnabled
    {
        get => (bool)GetValue(IsAnimationEnabledProperty);
        set => SetValue(IsAnimationEnabledProperty, value);
    }

    /// <summary>How long the spotlight's move animation takes. Default 180 ms.</summary>
    public Duration AnimationDuration
    {
        get => (Duration)GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    /// <summary>The default padding inflating a step's target into its spotlight hole. Default 8.</summary>
    public Thickness SpotlightPadding
    {
        get => (Thickness)GetValue(SpotlightPaddingProperty);
        set => SetValue(SpotlightPaddingProperty, value);
    }

    /// <summary>The corner radius of the spotlight hole. Default 6.</summary>
    public double SpotlightCornerRadius
    {
        get => (double)GetValue(SpotlightCornerRadiusProperty);
        set => SetValue(SpotlightCornerRadiusProperty, value);
    }

    /// <summary>When <see langword="true"/>, clicks inside the spotlight hole reach the app underneath for every step. Default <see langword="false"/>.</summary>
    public bool IsSpotlightInteractive
    {
        get => (bool)GetValue(IsSpotlightInteractiveProperty);
        set => SetValue(IsSpotlightInteractiveProperty, value);
    }

    /// <summary>The space left between the spotlight hole and the callout. Default 12.</summary>
    public double CalloutGap
    {
        get => (double)GetValue(CalloutGapProperty);
        set => SetValue(CalloutGapProperty, value);
    }

    /// <summary>The minimum distance kept between the callout and the overlay's edges. Default 16.</summary>
    public double HostMargin
    {
        get => (double)GetValue(HostMarginProperty);
        set => SetValue(HostMarginProperty, value);
    }

    /// <summary>The placements tried, in order, when a step has no <see cref="TourStep.PreferredPlacement"/>. Default Left, Bottom, Right, Top.</summary>
    public IList<CalloutPlacement> PlacementPriority
    {
        get => (IList<CalloutPlacement>)GetValue(PlacementPriorityProperty);
        set => SetValue(PlacementPriorityProperty, value);
    }

    /// <summary>An optional style applied to the callout's container.</summary>
    public Style? CalloutStyle
    {
        get => (Style?)GetValue(CalloutStyleProperty);
        set => SetValue(CalloutStyleProperty, value);
    }

    /// <summary>
    /// An optional template overriding the callout's default content. When set, its data context is an
    /// internal object exposing <c>Title</c>, <c>Description</c>, <c>StepCounterText</c>, <c>IsFinal</c>,
    /// <c>NextCommand</c> and <c>EndCommand</c> for binding.
    /// </summary>
    public DataTemplate? CalloutTemplate
    {
        get => (DataTemplate?)GetValue(CalloutTemplateProperty);
        set => SetValue(CalloutTemplateProperty, value);
    }

    /// <summary>An optional style applied to the built-in callout's Next button.</summary>
    public Style? NextButtonStyle
    {
        get => (Style?)GetValue(NextButtonStyleProperty);
        set => SetValue(NextButtonStyleProperty, value);
    }

    /// <summary>An optional style applied to the built-in callout's End tour button.</summary>
    public Style? EndButtonStyle
    {
        get => (Style?)GetValue(EndButtonStyleProperty);
        set => SetValue(EndButtonStyleProperty, value);
    }

    /// <summary>An optional style applied to the built-in prompt's Start tour button.</summary>
    public Style? StartButtonStyle
    {
        get => (Style?)GetValue(StartButtonStyleProperty);
        set => SetValue(StartButtonStyleProperty, value);
    }

    /// <summary>An optional style applied to the built-in prompt's Skip button.</summary>
    public Style? SkipButtonStyle
    {
        get => (Style?)GetValue(SkipButtonStyleProperty);
        set => SetValue(SkipButtonStyleProperty, value);
    }

    /// <summary>An optional style applied to the built-in prompt's "don't show again" checkbox.</summary>
    public Style? DontShowAgainCheckBoxStyle
    {
        get => (Style?)GetValue(DontShowAgainCheckBoxStyleProperty);
        set => SetValue(DontShowAgainCheckBoxStyleProperty, value);
    }

    /// <summary>The content of the Next button on a non-final step. Default "Next".</summary>
    public object NextButtonContent
    {
        get => GetValue(NextButtonContentProperty);
        set => SetValue(NextButtonContentProperty, value);
    }

    /// <summary>The content of the Next button on the final step. Default "Done".</summary>
    public object FinishButtonContent
    {
        get => GetValue(FinishButtonContentProperty);
        set => SetValue(FinishButtonContentProperty, value);
    }

    /// <summary>The content of the End tour button. Default "End tour".</summary>
    public object EndButtonContent
    {
        get => GetValue(EndButtonContentProperty);
        set => SetValue(EndButtonContentProperty, value);
    }

    /// <summary>The content of the start prompt's Start button. Default "Start tour".</summary>
    public object StartButtonContent
    {
        get => GetValue(StartButtonContentProperty);
        set => SetValue(StartButtonContentProperty, value);
    }

    /// <summary>The content of the start prompt's Skip button. Default "Skip".</summary>
    public object SkipButtonContent
    {
        get => GetValue(SkipButtonContentProperty);
        set => SetValue(SkipButtonContentProperty, value);
    }

    /// <summary>The content of the "don't show this again" checkbox. Default "Don't show this again".</summary>
    public object DontShowAgainContent
    {
        get => GetValue(DontShowAgainContentProperty);
        set => SetValue(DontShowAgainContentProperty, value);
    }

    /// <summary>The <see cref="string.Format(string,object,object)"/> format used for the step counter. Default "{0} of {1}".</summary>
    public string StepCounterFormat
    {
        get => (string)GetValue(StepCounterFormatProperty);
        set => SetValue(StepCounterFormatProperty, value);
    }

    /// <summary>Whether a tour is currently prompting or running on this overlay.</summary>
    public bool IsTourActive => (bool)GetValue(IsTourActiveProperty);

    /// <summary>The step currently being shown, or <see langword="null"/>.</summary>
    public TourStep? CurrentStep => (TourStep?)GetValue(CurrentStepProperty);

    /// <summary>The zero-based index of <see cref="CurrentStep"/>, or -1 while prompting or idle.</summary>
    public int CurrentStepIndex => (int)GetValue(CurrentStepIndexProperty);

    /// <summary>The total number of steps in the active tour.</summary>
    public int StepCount => (int)GetValue(StepCountProperty);

    /// <summary>The placement currently used for the callout, or <see langword="null"/> while idle.</summary>
    public CalloutPlacement? CurrentPlacement => (CalloutPlacement?)GetValue(CurrentPlacementProperty);

    /// <summary>
    /// The tour service this overlay renders for. When unset, falls back to <see cref="SpyglassLocator.Current"/>.
    /// </summary>
    public ITourService? TourService
    {
        get => (ITourService?)GetValue(TourServiceProperty);
        set => SetValue(TourServiceProperty, value);
    }

    bool ITourPresenter.IsAttached => IsLoaded && _scrimPart is not null;

    bool ITourPresenter.DontShowAgainChecked => _dontShowAgain;

    /// <inheritdoc cref="ITourPresenter.StartRequested" />
    event EventHandler? ITourPresenter.StartRequested
    {
        add => _startRequested += value;
        remove => _startRequested -= value;
    }

    /// <inheritdoc cref="ITourPresenter.SkipRequested" />
    event EventHandler? ITourPresenter.SkipRequested
    {
        add => _skipRequested += value;
        remove => _skipRequested -= value;
    }

    private event EventHandler? _startRequested;
    private event EventHandler? _skipRequested;

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _rootPart = GetTemplateChild(PartRoot) as Grid;
        _scrimPart = GetTemplateChild(PartScrim) as Path;
        _calloutPart = GetTemplateChild(PartCallout) as ContentControl;
        _promptPart = GetTemplateChild(PartPrompt) as ContentControl;

        if (_scrimPart is not null)
        {
            _fullRectGeometry = new RectangleGeometry(new Rect(new Point(0, 0), RenderSize));
            _holeGeometry = new RectangleGeometry(new Rect(0, 0, 0, 0), SpotlightCornerRadius, SpotlightCornerRadius);

            var group = new GeometryGroup { FillRule = FillRule.EvenOdd };
            group.Children.Add(_fullRectGeometry);
            group.Children.Add(_holeGeometry);
            _scrimPart.Data = group;
        }

        if (_rootPart is not null)
        {
            _interactionBlocker = new Rectangle
            {
                Fill = Brushes.Transparent,
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Focusable = false,
                IsHitTestVisible = true,
            };
            Panel.SetZIndex(_interactionBlocker, 1);
            _rootPart.Children.Add(_interactionBlocker);
        }

        if (_calloutPart is not null)
        {
            _calloutPart.Visibility = Visibility.Collapsed;
            _calloutPart.HorizontalAlignment = HorizontalAlignment.Left;
            _calloutPart.VerticalAlignment = VerticalAlignment.Top;
            KeyboardNavigation.SetTabNavigation(_calloutPart, KeyboardNavigationMode.Cycle);
        }

        if (_promptPart is not null)
        {
            _promptPart.Visibility = Visibility.Collapsed;
            KeyboardNavigation.SetTabNavigation(_promptPart, KeyboardNavigationMode.Cycle);
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Handled || !IsTourActive)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Escape:
                ResolveService().End();
                e.Handled = true;
                break;
            case Key.Enter:
            case Key.Right:
                if (CurrentStepIndex >= 0)
                {
                    ResolveService().Next();
                    e.Handled = true;
                }

                break;
        }
    }

    void ITourPresenter.ShowPrompt(TourDefinition tour) => ShowPromptCore(tour);

    bool ITourPresenter.ShowStep(TourStep step, int index, int count, bool isFinal) => ShowStepCore(step, index, count, isFinal);

    void ITourPresenter.HideAll() => HideAllCore();

    private void ShowPromptCore(TourDefinition tour)
    {
        if (_promptPart is null)
        {
            return;
        }

        EnsurePreviousFocusCaptured();
        _dontShowAgain = false;

        ResetSpotlight();
        HideCalloutVisual();

        SetValue(IsTourActivePropertyKey, true);
        SetValue(StepCountPropertyKey, tour.Steps.Count);
        SetValue(CurrentStepIndexPropertyKey, -1);
        SetValue(CurrentStepPropertyKey, null);
        SetValue(CurrentPlacementPropertyKey, null);

        _promptPart.Content = BuildPromptVisual(tour);
        _promptPart.Visibility = Visibility.Visible;

        Dispatcher.InvokeAsync(() => _startButtonElement?.Focus(), DispatcherPriority.Input);
    }

    private bool ShowStepCore(TourStep step, int index, int count, bool isFinal)
    {
        var target = SpyglassTargetRegistry.Resolve(step.TargetId);
        if (target is null || !target.IsVisible || target.RenderSize.Width <= 0 || target.RenderSize.Height <= 0)
        {
            return false;
        }

        EnsurePreviousFocusCaptured();

        _currentTarget = target;
        HidePromptVisual();

        SetValue(IsTourActivePropertyKey, true);
        SetValue(CurrentStepPropertyKey, step);
        SetValue(CurrentStepIndexPropertyKey, index);
        SetValue(StepCountPropertyKey, count);

        target.BringIntoView();

        Dispatcher.InvokeAsync(() => RenderCurrentStep(step, target, index, count, isFinal), DispatcherPriority.Loaded);

        return true;
    }

    private void HideAllCore()
    {
        HidePromptVisual();
        HideCalloutVisual();
        ResetSpotlight();

        SetValue(IsTourActivePropertyKey, false);
        SetValue(CurrentStepPropertyKey, null);
        SetValue(CurrentStepIndexPropertyKey, -1);
        SetValue(StepCountPropertyKey, 0);
        SetValue(CurrentPlacementPropertyKey, null);

        var previous = _previouslyFocused;
        _previouslyFocused = null;

        if (previous is not null)
        {
            Dispatcher.InvokeAsync(
                () =>
                {
                    if (previous.IsVisible && PresentationSource.FromVisual(previous) is not null)
                    {
                        previous.Focus();
                    }
                },
                DispatcherPriority.Input);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var service = ResolveService();
        if (service is TourService concrete)
        {
            _boundService = concrete;
            concrete.AttachPresenter(this);
        }
        else
        {
            SpyglassTrace.Source.TraceEvent(
                TraceEventType.Warning,
                0,
                "TourOverlay is wired to a custom ITourService implementation; only the built-in TourService supports overlay attachment.");
        }

        SizeChanged += OnSizeChanged;
        LayoutUpdated += OnLayoutUpdated;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        SizeChanged -= OnSizeChanged;
        LayoutUpdated -= OnLayoutUpdated;

        HideAllCore();

        _boundService?.DetachPresenter(this);
        _boundService = null;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_fullRectGeometry is not null)
        {
            _fullRectGeometry.Rect = new Rect(new Point(0, 0), RenderSize);
        }

        RefreshCurrentStepGeometry();
    }

    private void OnLayoutUpdated(object? sender, EventArgs e) => RefreshCurrentStepGeometry();

    private void RefreshCurrentStepGeometry()
    {
        if (_currentTarget is null || CurrentStep is not { } step)
        {
            return;
        }

        var inflated = ComputeHoleBounds(step, _currentTarget);
        if (inflated is not { } hole)
        {
            return;
        }

        if (_lastHoleBounds is { } last && RectsClose(last, hole))
        {
            return;
        }

        _lastHoleBounds = hole;
        AnimateHoleTo(hole);
        UpdateInteractionBlocker(hole, step.AllowInteraction || IsSpotlightInteractive);
        RepositionCallout(step, hole);
    }

    private void RenderCurrentStep(TourStep step, FrameworkElement target, int index, int count, bool isFinal)
    {
        if (!ReferenceEquals(_currentTarget, target) || _scrimPart is null || _holeGeometry is null || _calloutPart is null)
        {
            return;
        }

        var hole = ComputeHoleBounds(step, target) ?? new Rect(new Point(0, 0), RenderSize);
        _lastHoleBounds = hole;

        AnimateHoleTo(hole);

        if (_previousSpotlightTarget is not null && !ReferenceEquals(_previousSpotlightTarget, target))
        {
            Spyglass.SetIsSpotlighted(_previousSpotlightTarget, false);
        }

        Spyglass.SetIsSpotlighted(target, true);
        _previousSpotlightTarget = target;

        UpdateInteractionBlocker(hole, step.AllowInteraction || IsSpotlightInteractive);

        _calloutPart.Content = BuildStepCalloutContent(step, index, count, isFinal);
        if (CalloutTemplate is not null)
        {
            _calloutPart.ContentTemplate = CalloutTemplate;
        }

        if (CalloutStyle is not null)
        {
            _calloutPart.Style = CalloutStyle;
        }

        _calloutPart.Visibility = Visibility.Visible;

        RepositionCallout(step, hole);

        AutomationProperties.SetName(_calloutPart, step.Title ?? step.Description);
        AutomationProperties.SetHelpText(_calloutPart, step.Description);

        Dispatcher.InvokeAsync(FocusNextButton, DispatcherPriority.Input);
    }

    private Rect? ComputeHoleBounds(TourStep step, FrameworkElement target)
    {
        Rect bounds;
        try
        {
            bounds = target.TransformToVisual(this).TransformBounds(new Rect(target.RenderSize));
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        var padding = step.SpotlightPadding ?? SpotlightPadding;
        var inflated = Inflate(bounds, padding);
        var hostBounds = new Rect(new Point(0, 0), RenderSize);
        inflated.Intersect(hostBounds);

        return inflated.IsEmpty ? hostBounds : inflated;
    }

    private void RepositionCallout(TourStep step, Rect hole)
    {
        if (_calloutPart is null)
        {
            return;
        }

        _calloutPart.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var calloutSize = _calloutPart.DesiredSize;

        var layout = CalloutPlacementCalculator.Resolve(
            RenderSize,
            hole,
            calloutSize,
            CalloutGap,
            HostMargin,
            EffectivePlacementPriority,
            step.PreferredPlacement);

        _calloutPart.Margin = new Thickness(layout.Bounds.X, layout.Bounds.Y, 0, 0);
        SetValue(CurrentPlacementPropertyKey, layout.Placement);
    }

    private IReadOnlyList<CalloutPlacement> EffectivePlacementPriority
        => PlacementPriority as IReadOnlyList<CalloutPlacement> ?? new List<CalloutPlacement>(PlacementPriority);

    private void AnimateHoleTo(Rect target)
    {
        if (_holeGeometry is null)
        {
            return;
        }

        _holeGeometry.RadiusX = SpotlightCornerRadius;
        _holeGeometry.RadiusY = SpotlightCornerRadius;

        if (IsAnimationEnabled)
        {
            var animation = new RectAnimation(target, AnimationDuration) { EasingFunction = new QuadraticEase() };
            _holeGeometry.BeginAnimation(RectangleGeometry.RectProperty, animation);
        }
        else
        {
            _holeGeometry.BeginAnimation(RectangleGeometry.RectProperty, null);
            _holeGeometry.Rect = target;
        }
    }

    private void UpdateInteractionBlocker(Rect hole, bool interactive)
    {
        if (_interactionBlocker is null)
        {
            return;
        }

        if (interactive)
        {
            _interactionBlocker.Visibility = Visibility.Collapsed;
            return;
        }

        _interactionBlocker.Visibility = Visibility.Visible;
        _interactionBlocker.Width = hole.Width;
        _interactionBlocker.Height = hole.Height;
        _interactionBlocker.Margin = new Thickness(hole.X, hole.Y, 0, 0);
    }

    private void ResetSpotlight()
    {
        if (_holeGeometry is not null)
        {
            _holeGeometry.BeginAnimation(RectangleGeometry.RectProperty, null);
            _holeGeometry.Rect = new Rect(0, 0, 0, 0);
        }

        if (_interactionBlocker is not null)
        {
            _interactionBlocker.Visibility = Visibility.Collapsed;
        }

        if (_previousSpotlightTarget is not null)
        {
            Spyglass.SetIsSpotlighted(_previousSpotlightTarget, false);
            _previousSpotlightTarget = null;
        }

        _currentTarget = null;
        _lastHoleBounds = null;
        _nextButtonElement = null;
    }

    private void HideCalloutVisual()
    {
        if (_calloutPart is null)
        {
            return;
        }

        _calloutPart.Visibility = Visibility.Collapsed;
        _calloutPart.Content = null;
    }

    private void HidePromptVisual()
    {
        if (_promptPart is null)
        {
            return;
        }

        _promptPart.Visibility = Visibility.Collapsed;
        _promptPart.Content = null;
        _startButtonElement = null;
    }

    private void EnsurePreviousFocusCaptured()
    {
        _previouslyFocused ??= Keyboard.FocusedElement as FrameworkElement;
    }

    private void FocusNextButton()
    {
        if (_nextButtonElement is not null)
        {
            _nextButtonElement.Focus();
            Keyboard.Focus(_nextButtonElement);
        }
        else
        {
            _calloutPart?.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }
    }

    private ITourService ResolveService() => TourService ?? SpyglassLocator.Current;

    private object BuildStepCalloutContent(TourStep step, int index, int count, bool isFinal)
    {
        _nextButtonElement = null;

        if (CalloutTemplate is not null)
        {
            _nextCommand ??= new RelayCommand(() => ResolveService().Next());
            _endCommand ??= new RelayCommand(() => ResolveService().End());

            ICommand nextCommand = _nextCommand;
            ICommand endCommand = _endCommand;

            return new StepCalloutContext(
                step.Title,
                step.Description,
                string.Format(StepCounterFormat, index + 1, count),
                isFinal,
                nextCommand,
                endCommand);
        }

        return BuildDefaultCalloutVisual(step, index, count, isFinal);
    }

    private FrameworkElement BuildDefaultCalloutVisual(TourStep step, int index, int count, bool isFinal)
    {
        var root = new StackPanel { Margin = new Thickness(12), MaxWidth = 280 };

        if (!string.IsNullOrEmpty(step.Title))
        {
            root.Children.Add(new TextBlock
            {
                Text = step.Title,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4),
                TextWrapping = TextWrapping.Wrap,
            });
        }

        root.Children.Add(new TextBlock
        {
            Text = step.Description,
            TextWrapping = TextWrapping.Wrap,
        });

        root.Children.Add(new TextBlock
        {
            Text = string.Format(StepCounterFormat, index + 1, count),
            Margin = new Thickness(0, 8, 0, 8),
            Opacity = 0.7,
            FontSize = 11,
        });

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var endButton = new Button { Content = EndButtonContent, Margin = new Thickness(0, 0, 8, 0) };
        if (EndButtonStyle is not null)
        {
            endButton.Style = EndButtonStyle;
        }

        endButton.Click += (_, _) => ResolveService().End();

        var nextButton = new Button { Content = isFinal ? FinishButtonContent : NextButtonContent, IsDefault = true };
        if (NextButtonStyle is not null)
        {
            nextButton.Style = NextButtonStyle;
        }

        nextButton.Click += (_, _) => ResolveService().Next();

        buttons.Children.Add(endButton);
        buttons.Children.Add(nextButton);
        root.Children.Add(buttons);

        _nextButtonElement = nextButton;

        return root;
    }

    private FrameworkElement BuildPromptVisual(TourDefinition tour)
    {
        var root = new StackPanel { Margin = new Thickness(20), MaxWidth = 320 };

        root.Children.Add(new TextBlock
        {
            Text = tour.Name,
            FontWeight = FontWeights.Bold,
            FontSize = 16,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8),
        });

        if (!string.IsNullOrEmpty(tour.Introduction))
        {
            root.Children.Add(new TextBlock
            {
                Text = tour.Introduction,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12),
            });
        }

        var dontShowAgain = new CheckBox { Content = DontShowAgainContent, Margin = new Thickness(0, 0, 0, 12) };
        if (DontShowAgainCheckBoxStyle is not null)
        {
            dontShowAgain.Style = DontShowAgainCheckBoxStyle;
        }

        dontShowAgain.Checked += (_, _) => _dontShowAgain = true;
        dontShowAgain.Unchecked += (_, _) => _dontShowAgain = false;
        root.Children.Add(dontShowAgain);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        var skipButton = new Button { Content = SkipButtonContent, Margin = new Thickness(0, 0, 8, 0) };
        if (SkipButtonStyle is not null)
        {
            skipButton.Style = SkipButtonStyle;
        }

        skipButton.Click += (_, _) => _skipRequested?.Invoke(this, EventArgs.Empty);

        var startButton = new Button { Content = StartButtonContent, IsDefault = true };
        if (StartButtonStyle is not null)
        {
            startButton.Style = StartButtonStyle;
        }

        startButton.Click += (_, _) => _startRequested?.Invoke(this, EventArgs.Empty);

        buttons.Children.Add(skipButton);
        buttons.Children.Add(startButton);
        root.Children.Add(buttons);

        AutomationProperties.SetName(root, tour.Name);
        AutomationProperties.SetHelpText(root, tour.Introduction ?? tour.Name);

        _startButtonElement = startButton;

        return root;
    }

    private static Rect Inflate(Rect rect, Thickness padding)
    {
        return new Rect(
            rect.Left - padding.Left,
            rect.Top - padding.Top,
            Math.Max(0, rect.Width + padding.Left + padding.Right),
            Math.Max(0, rect.Height + padding.Top + padding.Bottom));
    }

    private static bool RectsClose(Rect a, Rect b)
    {
        const double epsilon = 0.5;
        return Math.Abs(a.X - b.X) < epsilon
            && Math.Abs(a.Y - b.Y) < epsilon
            && Math.Abs(a.Width - b.Width) < epsilon
            && Math.Abs(a.Height - b.Height) < epsilon;
    }

    private static SolidColorBrush CreateFrozenBrush(byte a, byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        brush.Freeze();
        return brush;
    }

    private sealed class StepCalloutContext
    {
        public StepCalloutContext(string? title, string description, string stepCounterText, bool isFinal, ICommand nextCommand, ICommand endCommand)
        {
            Title = title;
            Description = description;
            StepCounterText = stepCounterText;
            IsFinal = isFinal;
            NextCommand = nextCommand;
            EndCommand = endCommand;
        }

        public string? Title { get; }

        public string Description { get; }

        public string StepCounterText { get; }

        public bool IsFinal { get; }

        public ICommand NextCommand { get; }

        public ICommand EndCommand { get; }
    }

    private sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute) => _execute = execute;

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}
