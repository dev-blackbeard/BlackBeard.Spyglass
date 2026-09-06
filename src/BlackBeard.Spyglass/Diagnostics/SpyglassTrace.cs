using System.Diagnostics;

namespace BlackBeard.Spyglass.Diagnostics;

/// <summary>
/// The <see cref="TraceSource"/> Spyglass logs non-fatal warnings and diagnostics through. A host app
/// can attach a listener with <c>&lt;system.diagnostics&gt;</c> config or <c>Trace.Listeners</c>, named
/// "BlackBeard.Spyglass".
/// </summary>
internal static class SpyglassTrace
{
    /// <summary>The shared trace source instance.</summary>
    internal static readonly TraceSource Source = new("BlackBeard.Spyglass", SourceLevels.Warning);
}
