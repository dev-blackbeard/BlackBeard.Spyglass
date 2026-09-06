namespace BlackBeard.Spyglass;

/// <summary>
/// The rendering seam between <see cref="TourService"/> and an attached overlay
/// (<see cref="Controls.TourOverlay"/> in production, a fake in tests). Internal: hosts never implement
/// this themselves.
/// </summary>
internal interface ITourPresenter
{
    /// <summary>Whether this presenter is currently attached to a live overlay.</summary>
    bool IsAttached { get; }

    /// <summary>The current state of the presenter's "don't show this again" checkbox.</summary>
    bool DontShowAgainChecked { get; }

    /// <summary>Shows the start prompt for a tour. Raises <see cref="StartRequested"/> or <see cref="SkipRequested"/> when the user acts.</summary>
    /// <param name="tour">The tour to prompt for.</param>
    void ShowPrompt(TourDefinition tour);

    /// <summary>
    /// Shows a step's spotlight and callout. Returns whether the step's target could be resolved and
    /// rendered; the service skips a step that returns <see langword="false"/>.
    /// </summary>
    /// <param name="step">The step to show.</param>
    /// <param name="index">The step's zero-based index.</param>
    /// <param name="count">The total number of steps in the tour.</param>
    /// <param name="isFinal">Whether this is the last step of the tour.</param>
    bool ShowStep(TourStep step, int index, int count, bool isFinal);

    /// <summary>Hides the prompt and any step chrome, and resets presenter-owned visual state.</summary>
    void HideAll();

    /// <summary>Raised when the user clicks the start-prompt's Start button.</summary>
    event System.EventHandler? StartRequested;

    /// <summary>Raised when the user clicks the start-prompt's Skip button.</summary>
    event System.EventHandler? SkipRequested;
}
