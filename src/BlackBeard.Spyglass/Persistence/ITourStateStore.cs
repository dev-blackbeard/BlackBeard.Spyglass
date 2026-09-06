namespace BlackBeard.Spyglass.Persistence;

/// <summary>
/// Persists the "show every time" preference for tours, keyed by <see cref="TourDefinition.Key"/>.
/// Implementations must never throw out of these members; a store that cannot persist should degrade
/// to an in-memory fallback and trace a warning instead.
/// </summary>
public interface ITourStateStore
{
    /// <summary>
    /// Reads the persisted "show every time" value for a tour.
    /// </summary>
    /// <param name="tourKey">The tour key.</param>
    /// <returns><see langword="null"/> if nothing has ever been persisted for this key.</returns>
    bool? GetShowEveryTime(string tourKey);

    /// <summary>Sets the "show every time" value for a tour, in memory.</summary>
    /// <param name="tourKey">The tour key.</param>
    /// <param name="value">The value to store.</param>
    void SetShowEveryTime(string tourKey, bool value);

    /// <summary>Persists any pending changes made via <see cref="SetShowEveryTime"/> to durable storage.</summary>
    void Flush();
}
