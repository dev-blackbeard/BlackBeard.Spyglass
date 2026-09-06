using System.Collections.Generic;
using System.Windows;
using BlackBeard.Spyglass;
using BlackBeard.Spyglass.Placement;
using Xunit;

namespace BlackBeard.Spyglass.Tests;

public class CalloutPlacementCalculatorTests
{
    private static readonly IReadOnlyList<CalloutPlacement> DefaultPriority =
        new[] { CalloutPlacement.Left, CalloutPlacement.Bottom, CalloutPlacement.Right, CalloutPlacement.Top };

    [Fact]
    public void Resolve_HonoursPriorityOrder_WhenTheFirstCandidateFits()
    {
        var hostSize = new Size(800, 600);
        var hole = new Rect(350, 250, 100, 100);
        var callout = new Size(150, 80);

        var layout = CalloutPlacementCalculator.Resolve(hostSize, hole, callout, gap: 12, hostMargin: 16, DefaultPriority);

        Assert.Equal(CalloutPlacement.Left, layout.Placement);
        Assert.Equal(188, layout.Bounds.X, precision: 3);
        Assert.Equal(260, layout.Bounds.Y, precision: 3);
    }

    [Fact]
    public void Resolve_FallsBackFromLeftToBottom_WhenHoleHugsLeftEdge()
    {
        var hostSize = new Size(800, 600);
        var hole = new Rect(0, 250, 100, 100);
        var callout = new Size(150, 80);

        var layout = CalloutPlacementCalculator.Resolve(hostSize, hole, callout, gap: 12, hostMargin: 16, DefaultPriority);

        Assert.Equal(CalloutPlacement.Bottom, layout.Placement);
        Assert.Equal(16, layout.Bounds.X, precision: 3);
        Assert.Equal(362, layout.Bounds.Y, precision: 3);
    }

    [Fact]
    public void Resolve_FallsBackFromBottomToTop_WhenHoleIsAtTheBottomEdge()
    {
        var hostSize = new Size(800, 600);
        var hole = new Rect(50, 540, 700, 50);
        var callout = new Size(150, 80);

        var layout = CalloutPlacementCalculator.Resolve(hostSize, hole, callout, gap: 12, hostMargin: 16, DefaultPriority);

        Assert.Equal(CalloutPlacement.Top, layout.Placement);
        Assert.Equal(448, layout.Bounds.Y, precision: 3);
    }

    [Fact]
    public void Resolve_ClampsIntoTheHost_WhenTheChosenSideOverflows()
    {
        var hostSize = new Size(300, 200);
        var hole = new Rect(20, 80, 60, 40);
        var callout = new Size(250, 150);

        var layout = CalloutPlacementCalculator.Resolve(hostSize, hole, callout, gap: 8, hostMargin: 10, DefaultPriority);

        Assert.Equal(CalloutPlacement.Right, layout.Placement);
        Assert.Equal(40, layout.Bounds.X, precision: 3);
        Assert.Equal(25, layout.Bounds.Y, precision: 3);
        Assert.True(layout.Bounds.X >= 10);
        Assert.True(layout.Bounds.Right <= hostSize.Width - 10 + 0.001);
    }

    [Fact]
    public void Resolve_Centers_WhenTheHoleFillsTheWindow()
    {
        var hostSize = new Size(400, 300);
        var hole = new Rect(0, 0, 400, 300);
        var callout = new Size(200, 100);

        var layout = CalloutPlacementCalculator.Resolve(hostSize, hole, callout, gap: 12, hostMargin: 10, DefaultPriority);

        Assert.Equal(CalloutPlacement.Center, layout.Placement);
        Assert.Equal(100, layout.Bounds.X, precision: 3);
        Assert.Equal(100, layout.Bounds.Y, precision: 3);
    }

    [Fact]
    public void Resolve_RespectsPreferredPlacement_OverThePriorityList()
    {
        var hostSize = new Size(800, 600);
        var hole = new Rect(350, 250, 100, 100);
        var callout = new Size(150, 80);

        var layout = CalloutPlacementCalculator.Resolve(
            hostSize, hole, callout, gap: 12, hostMargin: 16, DefaultPriority, preferred: CalloutPlacement.Bottom);

        Assert.Equal(CalloutPlacement.Bottom, layout.Placement);
    }
}
