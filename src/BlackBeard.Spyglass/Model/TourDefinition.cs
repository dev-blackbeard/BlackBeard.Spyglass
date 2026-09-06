using System.Collections.Generic;

namespace BlackBeard.Spyglass;

/// <summary>
/// A named, ordered sequence of <see cref="TourStep"/>s that <see cref="ITourService"/> can run.
/// </summary>
public sealed class TourDefinition
{
    /// <summary>The unique key this tour is registered and persisted under.</summary>
    public string Key { get; set; } = "";

    /// <summary>The tour's display name, shown on the start prompt.</summary>
    public string Name { get; set; } = "";

    /// <summary>An optional blurb shown on the start prompt, below <see cref="Name"/>.</summary>
    public string? Introduction { get; set; }

    /// <summary>
    /// The seed value used the first time this tour is requested, before anything has been persisted
    /// for its <see cref="Key"/>. Once a "don't show this again" choice is persisted, that value wins.
    /// </summary>
    public bool ShowEveryTime { get; set; } = true;

    /// <summary>The ordered steps that make up this tour.</summary>
    public IList<TourStep> Steps { get; } = new List<TourStep>();
}
