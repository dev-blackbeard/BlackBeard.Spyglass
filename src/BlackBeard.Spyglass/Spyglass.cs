using System.Windows;

namespace BlackBeard.Spyglass;

/// <summary>
/// Attached properties used to tag elements as tour targets.
/// </summary>
public static class Spyglass
{
    /// <summary>
    /// Identifies the <c>Spyglass.TargetId</c> attached property. Set it on any <see cref="FrameworkElement"/>
    /// to make it resolvable by <see cref="TourStep.TargetId"/>.
    /// </summary>
    public static readonly DependencyProperty TargetIdProperty = DependencyProperty.RegisterAttached(
        "TargetId",
        typeof(string),
        typeof(Spyglass),
        new PropertyMetadata(null, OnTargetIdChanged));

    private static readonly DependencyPropertyKey IsSpotlightedPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "IsSpotlighted",
        typeof(bool),
        typeof(Spyglass),
        new PropertyMetadata(false));

    /// <summary>
    /// Identifies the read-only <c>Spyglass.IsSpotlighted</c> attached property, set to <see langword="true"/>
    /// on an element while it is the active step's target, so styles can react to it (e.g. a glow).
    /// </summary>
    public static readonly DependencyProperty IsSpotlightedProperty = IsSpotlightedPropertyKey.DependencyProperty;

    /// <summary>Sets the <c>Spyglass.TargetId</c> attached property.</summary>
    /// <param name="element">The element to tag.</param>
    /// <param name="value">The id tours will use to find this element.</param>
    public static void SetTargetId(DependencyObject element, string? value)
    {
        if (element is null)
        {
            throw new System.ArgumentNullException(nameof(element));
        }

        element.SetValue(TargetIdProperty, value);
    }

    /// <summary>Gets the <c>Spyglass.TargetId</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    public static string? GetTargetId(DependencyObject element)
    {
        if (element is null)
        {
            throw new System.ArgumentNullException(nameof(element));
        }

        return (string?)element.GetValue(TargetIdProperty);
    }

    /// <summary>Gets the <c>Spyglass.IsSpotlighted</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    public static bool GetIsSpotlighted(DependencyObject element)
    {
        if (element is null)
        {
            throw new System.ArgumentNullException(nameof(element));
        }

        return (bool)element.GetValue(IsSpotlightedProperty);
    }

    /// <summary>Sets the read-only <c>Spyglass.IsSpotlighted</c> attached property. Called by <see cref="Controls.TourOverlay"/> only.</summary>
    internal static void SetIsSpotlighted(DependencyObject element, bool value)
    {
        element.SetValue(IsSpotlightedPropertyKey, value);
    }

    private static void OnTargetIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        if (e.OldValue is string oldId && !string.IsNullOrEmpty(oldId))
        {
            SpyglassTargetRegistry.Unregister(oldId, element);
        }

        if (e.NewValue is string newId && !string.IsNullOrEmpty(newId))
        {
            SpyglassTargetRegistry.Register(newId, element);
        }
    }
}
