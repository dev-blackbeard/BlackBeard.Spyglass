using BlackBeard.Spyglass.Persistence;
using Prism.Events;

namespace BlackBeard.Spyglass;

/// <summary>
/// Configures the <see cref="ITourService"/> created by <see cref="SpyglassRegistration.RegisterSpyglass"/>.
/// </summary>
public sealed class SpyglassOptions
{
    /// <summary>
    /// The <see cref="ITourStateStore"/> to use. When <see langword="null"/>, a
    /// <see cref="JsonFileTourStateStore"/> is created, using <see cref="StorageFilePath"/> if set.
    /// </summary>
    public ITourStateStore? StateStore { get; set; }

    /// <summary>
    /// An explicit file path for the default <see cref="JsonFileTourStateStore"/>. Ignored if
    /// <see cref="StateStore"/> is set.
    /// </summary>
    public string? StorageFilePath { get; set; }

    /// <summary>
    /// When supplied, the tour service publishes <see cref="TourStateChangedEvent"/> through it, in
    /// addition to raising <see cref="ITourService.StateChanged"/>.
    /// </summary>
    public IEventAggregator? EventAggregator { get; set; }
}
