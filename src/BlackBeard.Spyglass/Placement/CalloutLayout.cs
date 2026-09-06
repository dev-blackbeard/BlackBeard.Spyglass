using System.Windows;

namespace BlackBeard.Spyglass.Placement;

/// <summary>
/// The result of <see cref="CalloutPlacementCalculator.Resolve"/>: where a callout was placed, and on
/// which side of the spotlight hole.
/// </summary>
public readonly struct CalloutLayout
{
    /// <summary>Creates a <see cref="CalloutLayout"/>.</summary>
    /// <param name="placement">The side (or <see cref="CalloutPlacement.Center"/>) the callout was placed on.</param>
    /// <param name="bounds">The callout's resolved bounds, in the host's coordinate space.</param>
    public CalloutLayout(CalloutPlacement placement, Rect bounds)
    {
        Placement = placement;
        Bounds = bounds;
    }

    /// <summary>The side (or <see cref="CalloutPlacement.Center"/>) the callout was placed on.</summary>
    public CalloutPlacement Placement { get; }

    /// <summary>The callout's resolved bounds, in the host's coordinate space.</summary>
    public Rect Bounds { get; }
}
