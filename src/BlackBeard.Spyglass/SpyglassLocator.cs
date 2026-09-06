using System;

namespace BlackBeard.Spyglass;

/// <summary>
/// A static service locator for <see cref="ITourService"/>, used by <see cref="Controls.TourOverlay"/>
/// when no <c>TourService</c> is set explicitly. <see cref="SpyglassRegistration.RegisterSpyglass"/>
/// sets this automatically; a non-Prism host can set it directly instead.
/// </summary>
public static class SpyglassLocator
{
    private static readonly object Gate = new();
    private static ITourService? _current;

    /// <summary>
    /// The current <see cref="ITourService"/>. Reading this before anything has been assigned lazily
    /// creates a default <see cref="TourService"/> backed by a <see cref="Persistence.JsonFileTourStateStore"/>.
    /// </summary>
    public static ITourService Current
    {
        get
        {
            lock (Gate)
            {
                return _current ??= new TourService();
            }
        }
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            lock (Gate)
            {
                _current = value;
            }
        }
    }
}
