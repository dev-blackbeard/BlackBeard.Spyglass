using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using BlackBeard.Spyglass.Diagnostics;

namespace BlackBeard.Spyglass;

/// <summary>
/// The static, weak-reference-backed registry of elements tagged with <see cref="Spyglass.TargetId"/>.
/// Internal: hosts interact with it only through the attached property.
/// </summary>
internal static class SpyglassTargetRegistry
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, List<Entry>> Entries = new();

    public static void Register(string id, FrameworkElement element)
    {
        lock (Gate)
        {
            if (!Entries.TryGetValue(id, out var list))
            {
                list = new List<Entry>();
                Entries[id] = list;
            }

            Purge(list);

            var entry = new Entry(id, element);
            list.Add(entry);

            element.Loaded += entry.OnLoaded;
            element.Unloaded += entry.OnUnloaded;
        }
    }

    public static void Unregister(string id, FrameworkElement element)
    {
        lock (Gate)
        {
            UnregisterCore(id, element);
        }
    }

    /// <summary>
    /// Resolves the element currently tagged with <paramref name="id"/>. If several are tagged with the
    /// same id, prefers one that is loaded, visible, and connected to a <see cref="PresentationSource"/>;
    /// among ties, the most recently loaded one wins.
    /// </summary>
    public static FrameworkElement? Resolve(string id)
    {
        lock (Gate)
        {
            if (!Entries.TryGetValue(id, out var list))
            {
                return null;
            }

            Purge(list);
            if (list.Count == 0)
            {
                Entries.Remove(id);
                return null;
            }

            if (list.Count == 1)
            {
                return list[0].Element;
            }

            var qualified = list
                .Where(e => e.Element.IsLoaded && e.Element.IsVisible && PresentationSource.FromVisual(e.Element) is not null)
                .ToList();

            var pool = qualified.Count > 0 ? qualified : list;

            if (pool.Count > 1)
            {
                SpyglassTrace.Source.TraceEvent(
                    TraceEventType.Warning,
                    0,
                    "Multiple elements are tagged with Spyglass.TargetId '{0}'; using the most recently loaded.",
                    id);
            }

            return pool.OrderByDescending(e => e.LoadedAtUtc).First().Element;
        }
    }

    /// <summary>Test-only: the number of live (non-purged) entries registered for an id.</summary>
    internal static int CountForTests(string id)
    {
        lock (Gate)
        {
            if (!Entries.TryGetValue(id, out var list))
            {
                return 0;
            }

            Purge(list);
            return list.Count;
        }
    }

    private static void UnregisterCore(string id, FrameworkElement element)
    {
        if (!Entries.TryGetValue(id, out var list))
        {
            return;
        }

        var entry = list.FirstOrDefault(e => e.Reference.TryGetTarget(out var target) && ReferenceEquals(target, element));
        if (entry is not null)
        {
            element.Loaded -= entry.OnLoaded;
            element.Unloaded -= entry.OnUnloaded;
            list.Remove(entry);
        }

        Purge(list);

        if (list.Count == 0)
        {
            Entries.Remove(id);
        }
    }

    private static void Purge(List<Entry> list)
    {
        list.RemoveAll(e => !e.Reference.TryGetTarget(out _));
    }

    private sealed class Entry
    {
        public Entry(string id, FrameworkElement element)
        {
            Id = id;
            Reference = new WeakReference<FrameworkElement>(element);
            LoadedAtUtc = DateTime.UtcNow;
        }

        public string Id { get; }

        public WeakReference<FrameworkElement> Reference { get; }

        public DateTime LoadedAtUtc { get; private set; }

        public FrameworkElement Element => Reference.TryGetTarget(out var element)
            ? element
            : throw new InvalidOperationException("The target element has been garbage collected.");

        public void OnLoaded(object sender, RoutedEventArgs e) => LoadedAtUtc = DateTime.UtcNow;

        public void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                lock (Gate)
                {
                    UnregisterCore(Id, element);
                }
            }
        }
    }
}
