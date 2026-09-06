using System;
using System.Collections.Generic;
using System.Windows;

namespace BlackBeard.Spyglass.Placement;

/// <summary>
/// Pure geometry for deciding where a step's callout goes relative to the spotlight hole. Has no
/// dependency on any control, so it can be unit tested directly.
/// </summary>
public static class CalloutPlacementCalculator
{
    private const double DominantHoleCoverageRatio = 0.6;

    /// <summary>
    /// Resolves the callout's placement and bounds for a spotlight hole.
    /// </summary>
    /// <param name="hostSize">The size of the overlay hosting the callout.</param>
    /// <param name="hole">The spotlight hole's bounds, in the host's coordinate space.</param>
    /// <param name="callout">The callout's desired (measured) size.</param>
    /// <param name="gap">The space to leave between the hole and the callout.</param>
    /// <param name="hostMargin">The minimum distance to keep the callout from the host's edges.</param>
    /// <param name="priority">The placements to try, in order, after <paramref name="preferred"/>.</param>
    /// <param name="preferred">A placement to try before <paramref name="priority"/>, or <see langword="null"/>.</param>
    /// <returns>The resolved placement and bounds.</returns>
    public static CalloutLayout Resolve(
        Size hostSize,
        Rect hole,
        Size callout,
        double gap,
        double hostMargin,
        IReadOnlyList<CalloutPlacement> priority,
        CalloutPlacement? preferred = null)
    {
        if (priority is null)
        {
            throw new ArgumentNullException(nameof(priority));
        }

        if (IsHoleDominant(hostSize, hole))
        {
            return new CalloutLayout(CalloutPlacement.Center, CenterRect(hostSize, callout, hostMargin));
        }

        foreach (var placement in EnumerateCandidates(priority, preferred))
        {
            var rect = ComputeRect(placement, hole, callout, gap);
            if (FitsPrimaryAxis(placement, rect, hostSize, hostMargin))
            {
                return new CalloutLayout(placement, ClampCrossAxis(placement, rect, hostSize, hostMargin));
            }
        }

        var fallback = ChooseMostFreeSpace(hostSize, hole, gap);
        var fallbackRect = ComputeRect(fallback, hole, callout, gap);
        return new CalloutLayout(fallback, ClampFully(fallbackRect, hostSize, callout, hostMargin));
    }

    private static IEnumerable<CalloutPlacement> EnumerateCandidates(
        IReadOnlyList<CalloutPlacement> priority,
        CalloutPlacement? preferred)
    {
        var seen = new HashSet<CalloutPlacement>();

        if (preferred.HasValue && preferred.Value != CalloutPlacement.Center && seen.Add(preferred.Value))
        {
            yield return preferred.Value;
        }

        foreach (var placement in priority)
        {
            if (placement != CalloutPlacement.Center && seen.Add(placement))
            {
                yield return placement;
            }
        }
    }

    private static bool IsHoleDominant(Size hostSize, Rect hole)
    {
        var hostArea = hostSize.Width * hostSize.Height;
        if (hostArea <= 0)
        {
            return false;
        }

        var holeArea = Math.Max(0, hole.Width) * Math.Max(0, hole.Height);
        return holeArea / hostArea > DominantHoleCoverageRatio;
    }

    private static Rect ComputeRect(CalloutPlacement placement, Rect hole, Size callout, double gap)
    {
        return placement switch
        {
            CalloutPlacement.Left => new Rect(
                hole.Left - gap - callout.Width,
                hole.Top + (hole.Height - callout.Height) / 2,
                callout.Width,
                callout.Height),
            CalloutPlacement.Right => new Rect(
                hole.Right + gap,
                hole.Top + (hole.Height - callout.Height) / 2,
                callout.Width,
                callout.Height),
            CalloutPlacement.Top => new Rect(
                hole.Left + (hole.Width - callout.Width) / 2,
                hole.Top - gap - callout.Height,
                callout.Width,
                callout.Height),
            CalloutPlacement.Bottom => new Rect(
                hole.Left + (hole.Width - callout.Width) / 2,
                hole.Bottom + gap,
                callout.Width,
                callout.Height),
            _ => throw new ArgumentOutOfRangeException(nameof(placement), placement, "Center is not a directional placement."),
        };
    }

    private static bool FitsPrimaryAxis(CalloutPlacement placement, Rect rect, Size hostSize, double hostMargin)
    {
        return placement switch
        {
            CalloutPlacement.Left or CalloutPlacement.Right =>
                rect.Left >= hostMargin && rect.Right <= hostSize.Width - hostMargin,
            CalloutPlacement.Top or CalloutPlacement.Bottom =>
                rect.Top >= hostMargin && rect.Bottom <= hostSize.Height - hostMargin,
            _ => true,
        };
    }

    private static Rect ClampCrossAxis(CalloutPlacement placement, Rect rect, Size hostSize, double hostMargin)
    {
        var x = rect.X;
        var y = rect.Y;

        switch (placement)
        {
            case CalloutPlacement.Left:
            case CalloutPlacement.Right:
                y = Clamp(y, hostMargin, hostSize.Height - rect.Height - hostMargin);
                break;
            case CalloutPlacement.Top:
            case CalloutPlacement.Bottom:
                x = Clamp(x, hostMargin, hostSize.Width - rect.Width - hostMargin);
                break;
        }

        return new Rect(x, y, rect.Width, rect.Height);
    }

    private static Rect ClampFully(Rect rect, Size hostSize, Size callout, double hostMargin)
    {
        var x = Clamp(rect.X, hostMargin, hostSize.Width - callout.Width - hostMargin);
        var y = Clamp(rect.Y, hostMargin, hostSize.Height - callout.Height - hostMargin);
        return new Rect(x, y, callout.Width, callout.Height);
    }

    private static Rect CenterRect(Size hostSize, Size callout, double hostMargin)
    {
        var x = Clamp((hostSize.Width - callout.Width) / 2, hostMargin, hostSize.Width - callout.Width - hostMargin);
        var y = Clamp((hostSize.Height - callout.Height) / 2, hostMargin, hostSize.Height - callout.Height - hostMargin);
        return new Rect(x, y, callout.Width, callout.Height);
    }

    private static CalloutPlacement ChooseMostFreeSpace(Size hostSize, Rect hole, double gap)
    {
        var candidates = new (CalloutPlacement Placement, double Free)[]
        {
            (CalloutPlacement.Left, hole.Left - gap),
            (CalloutPlacement.Right, hostSize.Width - hole.Right - gap),
            (CalloutPlacement.Top, hole.Top - gap),
            (CalloutPlacement.Bottom, hostSize.Height - hole.Bottom - gap),
        };

        var best = candidates[0];
        for (var i = 1; i < candidates.Length; i++)
        {
            if (candidates[i].Free > best.Free)
            {
                best = candidates[i];
            }
        }

        return best.Placement;
    }

    private static double Clamp(double value, double min, double max)
    {
        if (max < min)
        {
            return min;
        }

        return Math.Min(Math.Max(value, min), max);
    }
}
