namespace BlackBeard.Spyglass;

/// <summary>
/// The phase a tour is in when <see cref="ITourService.StateChanged"/> fires.
/// </summary>
public enum TourState
{
    /// <summary>The start prompt is showing; no step has been rendered yet.</summary>
    Prompting,

    /// <summary>A step is being shown.</summary>
    Running,

    /// <summary>The tour has finished, in any outcome.</summary>
    Ended,
}
