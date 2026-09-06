using System;
using System.IO;
using BlackBeard.Spyglass.Persistence;
using Xunit;

namespace BlackBeard.Spyglass.Tests;

public class JsonFileTourStateStoreTests : IDisposable
{
    private readonly string _filePath = Path.Combine(Path.GetTempPath(), $"spyglass-tests-{Guid.NewGuid():N}.json");

    public void Dispose()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    [Fact]
    public void SetAndFlush_ThenReload_RoundTripsTheValue()
    {
        var store = new JsonFileTourStateStore(_filePath);
        store.SetShowEveryTime("welcome-tour", false);
        store.Flush();

        var reloaded = new JsonFileTourStateStore(_filePath);

        Assert.False(reloaded.GetShowEveryTime("welcome-tour"));
    }

    [Fact]
    public void GetShowEveryTime_ReturnsNull_ForAnUnknownKey()
    {
        var store = new JsonFileTourStateStore(_filePath);

        Assert.Null(store.GetShowEveryTime("never-registered"));
    }

    [Fact]
    public void GetShowEveryTime_DegradesGracefully_WhenTheFileIsCorrupt()
    {
        File.WriteAllText(_filePath, "{ not valid json ] ");

        var store = new JsonFileTourStateStore(_filePath);

        Assert.Null(store.GetShowEveryTime("welcome-tour"));

        store.SetShowEveryTime("welcome-tour", true);
        store.Flush();

        var reloaded = new JsonFileTourStateStore(_filePath);
        Assert.True(reloaded.GetShowEveryTime("welcome-tour"));
    }
}
