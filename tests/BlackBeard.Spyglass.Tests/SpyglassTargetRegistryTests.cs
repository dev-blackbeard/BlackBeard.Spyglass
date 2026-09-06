using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Controls;
using BlackBeard.Spyglass;
using Xunit;

namespace BlackBeard.Spyglass.Tests;

public class SpyglassTargetRegistryTests
{
    [StaFact]
    public void Resolve_ReturnsTheTaggedElement()
    {
        var id = "target-" + Guid.NewGuid();
        var button = new Button();
        Spyglass.SetTargetId(button, id);

        var resolved = SpyglassTargetRegistry.Resolve(id);

        Assert.Same(button, resolved);
    }

    [StaFact]
    public void Resolve_PrefersTheMostRecentlyRegistered_WhenMultipleElementsShareAnId()
    {
        var id = "dup-" + Guid.NewGuid();
        var first = new Button();
        Spyglass.SetTargetId(first, id);

        Thread.Sleep(15);

        var second = new Button();
        Spyglass.SetTargetId(second, id);

        var resolved = SpyglassTargetRegistry.Resolve(id);

        Assert.Same(second, resolved);
    }

    [StaFact]
    public void Unregister_RemovesTheElement_SoResolveNoLongerFindsIt()
    {
        var id = "unregister-" + Guid.NewGuid();
        var button = new Button();
        Spyglass.SetTargetId(button, id);
        Assert.Same(button, SpyglassTargetRegistry.Resolve(id));

        Spyglass.SetTargetId(button, null);

        Assert.Null(SpyglassTargetRegistry.Resolve(id));
        Assert.Equal(0, SpyglassTargetRegistry.CountForTests(id));
    }

    [StaFact]
    public void DeadEntries_ArePurged_AfterTheElementIsCollected()
    {
        var id = "gc-" + Guid.NewGuid().ToString("N");

        CreateAndRegisterTransientElement(id);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.Equal(0, SpyglassTargetRegistry.CountForTests(id));
        Assert.Null(SpyglassTargetRegistry.Resolve(id));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateAndRegisterTransientElement(string id)
    {
        var element = new Button();
        Spyglass.SetTargetId(element, id);
    }
}
