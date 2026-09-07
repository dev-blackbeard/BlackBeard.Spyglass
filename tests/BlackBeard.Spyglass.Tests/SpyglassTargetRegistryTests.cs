using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
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

    [StaFact]
    public void Resolve_StillFindsTheElement_AfterItLeavesAndRejoinsTheVisualTree()
    {
        // Simulates a Prism region deactivating and later reactivating the same view instance: the
        // element's Unloaded event fires, then Loaded fires again, but Spyglass.TargetId is never
        // re-set on it (it's the same instance, not a freshly-constructed one), so OnTargetIdChanged
        // - the only thing that registers an entry - never runs a second time. Regression test for
        // https://github.com/dev-blackbeard/BlackBeard.Spyglass/issues/6: the target must still be
        // resolvable afterwards, not permanently dropped just because it briefly left the tree.
        var id = "region-reload-" + Guid.NewGuid();
        var button = new Button();
        Spyglass.SetTargetId(button, id);

        button.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        button.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

        var resolved = SpyglassTargetRegistry.Resolve(id);

        Assert.Same(button, resolved);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateAndRegisterTransientElement(string id)
    {
        var element = new Button();
        Spyglass.SetTargetId(element, id);
    }
}
