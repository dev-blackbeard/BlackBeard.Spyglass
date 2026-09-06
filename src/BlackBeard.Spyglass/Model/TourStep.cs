using System.Windows;

namespace BlackBeard.Spyglass;

/// <summary>
/// A single step of a <see cref="TourDefinition"/>: a target to spotlight and the callout text to show beside it.
/// </summary>
public sealed class TourStep
{
    /// <summary>
    /// The id that must match a <see cref="Spyglass.TargetId"/> attached-property value on some element
    /// in the current window for this step to spotlight it.
    /// </summary>
    public string TargetId { get; set; } = "";

    /// <summary>The callout body text shown for this step.</summary>
    public string Description { get; set; } = "";

    /// <summary>An optional heading shown above <see cref="Description"/> in the callout.</summary>
    public string? Title { get; set; }

    /// <summary>
    /// Overrides the overlay's default <c>SpotlightPadding</c> for this step only. When <see langword="null"/>,
    /// the overlay's own default is used.
    /// </summary>
    public Thickness? SpotlightPadding { get; set; }

    /// <summary>
    /// The placement tried first for this step's callout, before falling back to the overlay's
    /// <c>PlacementPriority</c> list.
    /// </summary>
    public CalloutPlacement? PreferredPlacement { get; set; }

    /// <summary>
    /// When <see langword="true"/>, clicks inside the spotlight hole reach the underlying application
    /// instead of being swallowed by the overlay.
    /// </summary>
    public bool AllowInteraction { get; set; }
}
