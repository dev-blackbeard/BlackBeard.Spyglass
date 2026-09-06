namespace BlackBeard.Spyglass;

/// <summary>
/// Describes how a tour run finished.
/// </summary>
public enum TourOutcome
{
    /// <summary>The tour was not shown because the persisted "show every time" setting was false.</summary>
    Suppressed,

    /// <summary>The user dismissed the start prompt with the Skip button.</summary>
    Skipped,

    /// <summary>The user walked every step and advanced past the last one.</summary>
    Completed,

    /// <summary>The user, the host, or a cancellation token ended the tour before it completed.</summary>
    Ended,

    /// <summary>The tour could not run at all (unregistered key, no attached overlay, or every step failed to resolve).</summary>
    Failed,
}
