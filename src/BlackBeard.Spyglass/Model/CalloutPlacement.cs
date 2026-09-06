namespace BlackBeard.Spyglass;

/// <summary>
/// Identifies where a tour step's callout is placed relative to the spotlighted target.
/// </summary>
public enum CalloutPlacement
{
    /// <summary>The callout is placed to the left of the target, vertically centred on it.</summary>
    Left,

    /// <summary>The callout is placed below the target, horizontally centred on it.</summary>
    Bottom,

    /// <summary>The callout is placed to the right of the target, vertically centred on it.</summary>
    Right,

    /// <summary>The callout is placed above the target, horizontally centred on it.</summary>
    Top,

    /// <summary>The callout is centred over the overlay, used when no side placement fits.</summary>
    Center,
}
