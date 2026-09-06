using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BlackBeard.Spyglass;

/// <summary>
/// Registers, runs, and tracks the persisted "show every time" preference for guided tours.
/// All members are safe to call from any thread; calls made off the UI thread are marshalled to it.
/// </summary>
public interface ITourService
{
    /// <summary>
    /// Registers a tour, keyed by <see cref="TourDefinition.Key"/>. Registering the same key again
    /// replaces the previous definition and traces a warning.
    /// </summary>
    /// <param name="tour">The tour to register.</param>
    void Register(TourDefinition tour);

    /// <summary>Attempts to look up a registered tour by key.</summary>
    /// <param name="key">The tour key to look up.</param>
    /// <param name="tour">The registered tour, if found.</param>
    /// <returns><see langword="true"/> if a tour with that key is registered.</returns>
    bool TryGet(string key, out TourDefinition tour);

    /// <summary>All currently registered tours.</summary>
    IReadOnlyCollection<TourDefinition> RegisteredTours { get; }

    /// <summary>Whether a tour is currently prompting or running.</summary>
    bool IsRunning { get; }

    /// <summary>The key of the tour that is currently prompting or running, or <see langword="null"/> if none.</summary>
    string? RunningTourKey { get; }

    /// <summary>
    /// Requests that a tour run, honouring its persisted "show every time" setting: if that setting is
    /// <see langword="false"/>, returns <see cref="TourOutcome.Suppressed"/> immediately without
    /// rendering anything. Intended for automatic prompts on view activation.
    /// </summary>
    /// <param name="key">The key of a registered tour.</param>
    /// <param name="ct">Cancels the tour, ending it as <see cref="TourOutcome.Ended"/>.</param>
    Task<TourResult> RequestAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Starts a tour unconditionally, ignoring its persisted "show every time" setting. Intended for an
    /// explicit "Take the tour" command.
    /// </summary>
    /// <param name="key">The key of a registered tour.</param>
    /// <param name="showPrompt">When <see langword="true"/>, shows the start prompt before step 0.</param>
    /// <param name="ct">Cancels the tour, ending it as <see cref="TourOutcome.Ended"/>.</param>
    Task<TourResult> StartAsync(string key, bool showPrompt = false, CancellationToken ct = default);

    /// <summary>Advances the running tour to its next step, or completes it if the current step is the last one.</summary>
    void Next();

    /// <summary>Ends the running tour immediately. Safe to call when no tour is running.</summary>
    void End();

    /// <summary>Reads the effective "show every time" setting for a tour: the persisted value if one exists, else the tour's own default.</summary>
    /// <param name="key">The tour key.</param>
    bool GetShowEveryTime(string key);

    /// <summary>Persists a "show every time" setting for a tour.</summary>
    /// <param name="key">The tour key.</param>
    /// <param name="value">The value to persist.</param>
    void SetShowEveryTime(string key, bool value);

    /// <summary>Raised whenever a tour transitions between prompting, running, and ended.</summary>
    event EventHandler<TourStateChangedEventArgs>? StateChanged;
}
