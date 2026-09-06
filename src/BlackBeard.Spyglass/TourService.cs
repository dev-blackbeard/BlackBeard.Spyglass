using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BlackBeard.Spyglass.Diagnostics;
using BlackBeard.Spyglass.Persistence;
using Prism.Events;

namespace BlackBeard.Spyglass;

/// <summary>
/// The default <see cref="ITourService"/> implementation. Every public member marshals to the
/// dispatcher captured at construction time (normally the application's UI thread) if called from
/// elsewhere.
/// </summary>
public sealed class TourService : ITourService
{
    private readonly Dictionary<string, TourDefinition> _tours = new();
    private readonly ITourStateStore _store;
    private readonly IEventAggregator? _eventAggregator;
    private readonly Dispatcher _dispatcher;

    private ITourPresenter? _presenter;
    private TourDefinition? _runningTour;
    private int _runningIndex = -1;
    private bool _anyStepShownThisRun;
    private TaskCompletionSource<TourResult>? _completion;
    private CancellationTokenRegistration _cancellationRegistration;
    private Action? _unsubscribeFromPrompt;

    /// <summary>
    /// Creates a <see cref="TourService"/> backed by a default <see cref="JsonFileTourStateStore"/>,
    /// with no Prism event publishing.
    /// </summary>
    public TourService()
        : this(new JsonFileTourStateStore(), eventAggregator: null)
    {
    }

    /// <summary>Creates a <see cref="TourService"/> with an explicit state store and, optionally, an event aggregator.</summary>
    /// <param name="store">The persistence store for "show every time" preferences.</param>
    /// <param name="eventAggregator">
    /// When supplied, <see cref="TourStateChangedEvent"/> is published alongside <see cref="StateChanged"/>.
    /// </param>
    public TourService(ITourStateStore store, IEventAggregator? eventAggregator = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _eventAggregator = eventAggregator;
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
    }

    /// <inheritdoc />
    public event EventHandler<TourStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public bool IsRunning => RunOnDispatcher(() => _runningTour is not null);

    /// <inheritdoc />
    public string? RunningTourKey => RunOnDispatcher(() => _runningTour?.Key);

    /// <inheritdoc />
    public IReadOnlyCollection<TourDefinition> RegisteredTours
        => RunOnDispatcher(() => (IReadOnlyCollection<TourDefinition>)_tours.Values.ToList());

    /// <inheritdoc />
    public void Register(TourDefinition tour)
    {
        if (tour is null)
        {
            throw new ArgumentNullException(nameof(tour));
        }

        RunOnDispatcher(() =>
        {
            if (_tours.ContainsKey(tour.Key))
            {
                SpyglassTrace.Source.TraceEvent(
                    TraceEventType.Warning,
                    0,
                    "A tour with key '{0}' is already registered; replacing it.",
                    tour.Key);
            }

            _tours[tour.Key] = tour;
        });
    }

    /// <inheritdoc />
    public bool TryGet(string key, out TourDefinition tour)
    {
        TourDefinition? found = null;
        var ok = false;
        RunOnDispatcher(() => ok = _tours.TryGetValue(key, out found));
        tour = found!;
        return ok;
    }

    /// <inheritdoc />
    public Task<TourResult> RequestAsync(string key, CancellationToken ct = default)
        => RunOnDispatcher(() => Begin(key, showPrompt: true, honorShowEveryTime: true, ct));

    /// <inheritdoc />
    public Task<TourResult> StartAsync(string key, bool showPrompt = false, CancellationToken ct = default)
        => RunOnDispatcher(() => Begin(key, showPrompt, honorShowEveryTime: false, ct));

    /// <inheritdoc />
    public void Next()
    {
        RunOnDispatcher(() =>
        {
            if (_runningTour is null)
            {
                return;
            }

            AdvanceTo(_runningTour, _runningIndex + 1);
        });
    }

    /// <inheritdoc />
    public void End()
    {
        RunOnDispatcher(() =>
        {
            if (_runningTour is null)
            {
                return;
            }

            CompleteRun(TourOutcome.Ended);
        });
    }

    /// <inheritdoc />
    public bool GetShowEveryTime(string key)
    {
        return RunOnDispatcher(() =>
        {
            var persisted = _store.GetShowEveryTime(key);
            if (persisted.HasValue)
            {
                return persisted.Value;
            }

            return !_tours.TryGetValue(key, out var tour) || tour.ShowEveryTime;
        });
    }

    /// <inheritdoc />
    public void SetShowEveryTime(string key, bool value)
    {
        RunOnDispatcher(() =>
        {
            _store.SetShowEveryTime(key, value);
            _store.Flush();
        });
    }

    /// <summary>Attaches the presenter (an overlay) that will render prompts and steps. Internal wiring, called by <see cref="Controls.TourOverlay"/>.</summary>
    internal void AttachPresenter(ITourPresenter presenter)
    {
        RunOnDispatcher(() => _presenter = presenter);
    }

    /// <summary>
    /// Detaches a presenter previously attached with <see cref="AttachPresenter"/>. If it was the
    /// running tour's presenter, the tour is ended first.
    /// </summary>
    internal void DetachPresenter(ITourPresenter presenter)
    {
        RunOnDispatcher(() =>
        {
            if (!ReferenceEquals(_presenter, presenter))
            {
                return;
            }

            if (_runningTour is not null)
            {
                CompleteRun(TourOutcome.Ended);
            }

            _presenter = null;
        });
    }

    private Task<TourResult> Begin(string key, bool showPrompt, bool honorShowEveryTime, CancellationToken ct)
    {
        if (!_tours.TryGetValue(key, out var tour))
        {
            return Task.FromResult(new TourResult(key, TourOutcome.Failed, -1));
        }

        if (honorShowEveryTime)
        {
            var persisted = _store.GetShowEveryTime(key) ?? tour.ShowEveryTime;
            if (!persisted)
            {
                return Task.FromResult(new TourResult(key, TourOutcome.Suppressed, -1));
            }
        }

        if (_runningTour is not null)
        {
            CompleteRun(TourOutcome.Ended);
        }

        if (_presenter is null || !_presenter.IsAttached)
        {
            SpyglassTrace.Source.TraceEvent(
                TraceEventType.Warning,
                0,
                "Spyglass tour '{0}' was requested but no TourOverlay is attached.",
                key);
            return Task.FromResult(new TourResult(key, TourOutcome.Failed, -1));
        }

        if (tour.Steps.Count == 0)
        {
            return Task.FromResult(new TourResult(key, TourOutcome.Completed, -1));
        }

        var completion = new TaskCompletionSource<TourResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _completion = completion;
        _cancellationRegistration = ct.CanBeCanceled
            ? ct.Register(() => RunOnDispatcher(End))
            : default;

        _runningTour = tour;
        _runningIndex = -1;
        _anyStepShownThisRun = false;

        if (showPrompt)
        {
            RaiseStateChanged(TourState.Prompting, tour.Key, -1, tour.Steps.Count);
            SubscribeToPrompt(tour);
            _presenter.ShowPrompt(tour);
        }
        else
        {
            AdvanceTo(tour, 0);
        }

        // AdvanceTo above may have already run the whole tour synchronously (e.g. every step
        // failed to resolve), which clears the _completion field via CompleteRun - so return the
        // local capture, not the field.
        return completion.Task;
    }

    private void SubscribeToPrompt(TourDefinition tour)
    {
        var presenter = _presenter!;

        void OnStart(object? sender, EventArgs e) => RunOnDispatcher(() =>
        {
            _unsubscribeFromPrompt?.Invoke();
            _unsubscribeFromPrompt = null;
            PersistDontShowAgainIfChecked(tour);
            AdvanceTo(tour, 0);
        });

        void OnSkip(object? sender, EventArgs e) => RunOnDispatcher(() =>
        {
            _unsubscribeFromPrompt?.Invoke();
            _unsubscribeFromPrompt = null;
            CompleteRun(TourOutcome.Skipped);
        });

        presenter.StartRequested += OnStart;
        presenter.SkipRequested += OnSkip;

        // Cleared eagerly by OnStart/OnSkip above, and also from CompleteRun - covering the case
        // where this prompt is preempted (by another Begin(), or by DetachPresenter) before the
        // user ever resolves it. Without this, a preempted prompt's handlers stayed attached
        // forever, so a later prompt for the same tour fired every stale handler on top of the
        // new one (issue #6: starting the same tour a second time didn't restart it correctly).
        _unsubscribeFromPrompt = () =>
        {
            presenter.StartRequested -= OnStart;
            presenter.SkipRequested -= OnSkip;
        };
    }

    private void AdvanceTo(TourDefinition tour, int startIndex)
    {
        var index = startIndex;

        while (index < tour.Steps.Count)
        {
            var shown = _presenter is not null
                && _presenter.ShowStep(tour.Steps[index], index, tour.Steps.Count, index == tour.Steps.Count - 1);

            if (shown)
            {
                _anyStepShownThisRun = true;
                _runningIndex = index;
                RaiseStateChanged(TourState.Running, tour.Key, index, tour.Steps.Count);
                return;
            }

            SpyglassTrace.Source.TraceEvent(
                TraceEventType.Warning,
                0,
                "Spyglass step {0} of tour '{1}' targets '{2}', which could not be resolved; skipping it.",
                index,
                tour.Key,
                tour.Steps[index].TargetId);
            index++;
        }

        CompleteRun(_anyStepShownThisRun ? TourOutcome.Completed : TourOutcome.Failed);
    }

    private void CompleteRun(TourOutcome outcome)
    {
        var tour = _runningTour;
        if (tour is null)
        {
            return;
        }

        _unsubscribeFromPrompt?.Invoke();
        _unsubscribeFromPrompt = null;

        var lastIndex = _runningIndex;
        PersistDontShowAgainIfChecked(tour);

        _presenter?.HideAll();

        _runningTour = null;
        _runningIndex = -1;
        _anyStepShownThisRun = false;

        RaiseStateChanged(TourState.Ended, tour.Key, lastIndex, tour.Steps.Count);

        var completion = _completion;
        _completion = null;
        _cancellationRegistration.Dispose();
        _cancellationRegistration = default;

        completion?.TrySetResult(new TourResult(tour.Key, outcome, lastIndex));
    }

    private void PersistDontShowAgainIfChecked(TourDefinition tour)
    {
        if (_presenter?.DontShowAgainChecked == true)
        {
            _store.SetShowEveryTime(tour.Key, false);
            _store.Flush();
        }
    }

    private void RaiseStateChanged(TourState state, string tourKey, int stepIndex, int stepCount)
    {
        var args = new TourStateChangedEventArgs(tourKey, state, stepIndex, stepCount);
        StateChanged?.Invoke(this, args);
        _eventAggregator?.GetEvent<TourStateChangedEvent>().Publish(args);
    }

    private void RunOnDispatcher(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _dispatcher.Invoke(action);
        }
    }

    private T RunOnDispatcher<T>(Func<T> func)
    {
        return _dispatcher.CheckAccess() ? func() : _dispatcher.Invoke(func);
    }
}
