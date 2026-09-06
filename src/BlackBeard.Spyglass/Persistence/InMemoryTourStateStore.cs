using System.Collections.Generic;

namespace BlackBeard.Spyglass.Persistence;

/// <summary>
/// An <see cref="ITourStateStore"/> that keeps its state only in memory. Useful for tests, and for
/// hosts that want to back Spyglass's persistence with their own settings mechanism.
/// </summary>
public sealed class InMemoryTourStateStore : ITourStateStore
{
    private readonly Dictionary<string, bool> _values = new();

    /// <inheritdoc />
    public bool? GetShowEveryTime(string tourKey)
        => _values.TryGetValue(tourKey, out var value) ? value : null;

    /// <inheritdoc />
    public void SetShowEveryTime(string tourKey, bool value) => _values[tourKey] = value;

    /// <inheritdoc />
    public void Flush()
    {
        // Nothing to flush; values are already the store of record.
    }
}
