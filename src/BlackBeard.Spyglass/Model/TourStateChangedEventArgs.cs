using System;

namespace BlackBeard.Spyglass;

/// <summary>
/// Carries the details of a tour state transition, raised by <see cref="ITourService.StateChanged"/>
/// and published as <see cref="TourStateChangedEvent"/>.
/// </summary>
public sealed class TourStateChangedEventArgs : EventArgs
{
    /// <summary>Creates a new <see cref="TourStateChangedEventArgs"/>.</summary>
    /// <param name="tourKey">The key of the tour that changed state.</param>
    /// <param name="state">The new state.</param>
    /// <param name="stepIndex">The current step index, or -1 while prompting or after ending with no step shown.</param>
    /// <param name="stepCount">The total number of steps in the tour.</param>
    public TourStateChangedEventArgs(string tourKey, TourState state, int stepIndex, int stepCount)
    {
        TourKey = tourKey;
        State = state;
        StepIndex = stepIndex;
        StepCount = stepCount;
    }

    /// <summary>The key of the tour that changed state.</summary>
    public string TourKey { get; }

    /// <summary>The new state.</summary>
    public TourState State { get; }

    /// <summary>The current step index, or -1 while prompting or after ending with no step shown.</summary>
    public int StepIndex { get; }

    /// <summary>The total number of steps in the tour.</summary>
    public int StepCount { get; }
}
