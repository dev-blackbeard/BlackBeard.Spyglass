using System;
using BlackBeard.Spyglass.Persistence;
using Prism.Ioc;

namespace BlackBeard.Spyglass;

/// <summary>
/// Prism <see cref="IContainerRegistry"/> extension for wiring up Spyglass.
/// </summary>
public static class SpyglassRegistration
{
    /// <summary>
    /// Registers <see cref="ITourService"/> and <see cref="ITourStateStore"/> as singletons in the
    /// Prism container, and sets <see cref="SpyglassLocator.Current"/> to the same instance so that a
    /// <see cref="Controls.TourOverlay"/> not bound to a DI-resolved service still finds it.
    /// </summary>
    /// <param name="registry">The Prism container registry.</param>
    /// <param name="configure">An optional callback to configure the tour service before it is created.</param>
    /// <returns><paramref name="registry"/>, for chaining.</returns>
    public static IContainerRegistry RegisterSpyglass(
        this IContainerRegistry registry,
        Action<SpyglassOptions>? configure = null)
    {
        if (registry is null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        var options = new SpyglassOptions();
        configure?.Invoke(options);

        var store = options.StateStore ?? new JsonFileTourStateStore(options.StorageFilePath);
        var service = new TourService(store, options.EventAggregator);

        SpyglassLocator.Current = service;

        registry.RegisterInstance<ITourStateStore>(store);
        registry.RegisterInstance<ITourService>(service);

        return registry;
    }
}
