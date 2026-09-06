namespace BlackBeard.Spyglass;

/// <summary>
/// The outcome of a completed or abandoned tour run, returned from
/// <see cref="ITourService.RequestAsync"/> and <see cref="ITourService.StartAsync"/>.
/// </summary>
/// <param name="TourKey">The key of the tour this result describes.</param>
/// <param name="Outcome">How the run finished.</param>
/// <param name="LastStepIndex">
/// The zero-based index of the last step shown, or -1 if no step was ever shown.
/// </param>
public sealed record TourResult(string TourKey, TourOutcome Outcome, int LastStepIndex);
