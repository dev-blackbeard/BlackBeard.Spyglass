using System.Threading.Tasks;
using BlackBeard.Spyglass;
using BlackBeard.Spyglass.Persistence;
using Xunit;

namespace BlackBeard.Spyglass.Tests;

public class TourServiceTests
{
    private static TourDefinition CreateTour(string key = "demo", int steps = 2, bool showEveryTime = true)
    {
        var tour = new TourDefinition { Key = key, Name = "Demo", ShowEveryTime = showEveryTime };
        for (var i = 0; i < steps; i++)
        {
            tour.Steps.Add(new TourStep { TargetId = $"target-{i}", Description = $"Step {i}" });
        }

        return tour;
    }

    [Fact]
    public async Task RequestAsync_ReturnsSuppressed_WhenPersistedShowEveryTimeIsFalse()
    {
        var store = new InMemoryTourStateStore();
        store.SetShowEveryTime("demo", false);
        var service = new TourService(store);
        var presenter = new FakeTourPresenter();
        service.AttachPresenter(presenter);
        service.Register(CreateTour());

        var result = await service.RequestAsync("demo");

        Assert.Equal(TourOutcome.Suppressed, result.Outcome);
        Assert.Null(presenter.PromptedTour);
        Assert.Empty(presenter.ShownSteps);
    }

    [Fact]
    public async Task StartAsync_RunsRegardlessOfPersistedShowEveryTime()
    {
        var store = new InMemoryTourStateStore();
        store.SetShowEveryTime("demo", false);
        var service = new TourService(store);
        var presenter = new FakeTourPresenter();
        service.AttachPresenter(presenter);
        service.Register(CreateTour());

        var runTask = service.StartAsync("demo");

        Assert.Single(presenter.ShownSteps);

        service.End();
        var result = await runTask;

        Assert.Equal(TourOutcome.Ended, result.Outcome);
    }

    [Fact]
    public async Task DontShowAgain_Persists_WhenTheTourCompletes()
    {
        var store = new InMemoryTourStateStore();
        var service = new TourService(store);
        var presenter = new FakeTourPresenter { DontShowAgainChecked = true };
        service.AttachPresenter(presenter);
        service.Register(CreateTour(steps: 1));

        var runTask = service.StartAsync("demo");
        service.Next();
        var result = await runTask;

        Assert.Equal(TourOutcome.Completed, result.Outcome);
        Assert.False(store.GetShowEveryTime("demo"));
    }

    [Fact]
    public async Task DontShowAgain_Persists_WhenTheTourIsEnded()
    {
        var store = new InMemoryTourStateStore();
        var service = new TourService(store);
        var presenter = new FakeTourPresenter { DontShowAgainChecked = true };
        service.AttachPresenter(presenter);
        service.Register(CreateTour());

        var runTask = service.StartAsync("demo");
        service.End();
        var result = await runTask;

        Assert.Equal(TourOutcome.Ended, result.Outcome);
        Assert.False(store.GetShowEveryTime("demo"));
    }

    [Fact]
    public async Task DontShowAgain_Persists_WhenSkippedFromThePrompt()
    {
        var store = new InMemoryTourStateStore();
        var service = new TourService(store);
        var presenter = new FakeTourPresenter { DontShowAgainChecked = true };
        service.AttachPresenter(presenter);
        service.Register(CreateTour());

        var runTask = service.RequestAsync("demo");
        presenter.RaiseSkip();
        var result = await runTask;

        Assert.Equal(TourOutcome.Skipped, result.Outcome);
        Assert.False(store.GetShowEveryTime("demo"));
    }

    [Fact]
    public async Task DontShowAgain_PersistsImmediately_WhenTheTourIsStarted()
    {
        var store = new InMemoryTourStateStore();
        var service = new TourService(store);
        var presenter = new FakeTourPresenter { DontShowAgainChecked = true };
        service.AttachPresenter(presenter);
        service.Register(CreateTour());

        var runTask = service.RequestAsync("demo");
        presenter.RaiseStart();

        Assert.False(store.GetShowEveryTime("demo"));

        service.End();
        await runTask;
    }

    [Fact]
    public void End_IsIdempotentAndSafeWhenNoTourIsRunning()
    {
        var service = new TourService(new InMemoryTourStateStore());
        var presenter = new FakeTourPresenter();
        service.AttachPresenter(presenter);
        service.Register(CreateTour());

        service.End();

        _ = service.StartAsync("demo");
        service.End();
        service.End();

        Assert.False(service.IsRunning);
    }

    [Fact]
    public void Register_ReplacesAPreviouslyRegisteredTourWithTheSameKey()
    {
        var service = new TourService(new InMemoryTourStateStore());
        service.Register(CreateTour(steps: 1));
        service.Register(CreateTour(steps: 5));

        service.TryGet("demo", out var tour);

        Assert.Equal(5, tour.Steps.Count);
    }

    [Fact]
    public async Task StartAsync_ReturnsFailed_ForAnUnregisteredKey()
    {
        var service = new TourService(new InMemoryTourStateStore());
        service.AttachPresenter(new FakeTourPresenter());

        var result = await service.StartAsync("missing");

        Assert.Equal(TourOutcome.Failed, result.Outcome);
    }

    [Fact]
    public async Task StartAsync_ReturnsFailed_WhenNoPresenterIsAttached()
    {
        var service = new TourService(new InMemoryTourStateStore());
        service.Register(CreateTour());

        var result = await service.StartAsync("demo");

        Assert.Equal(TourOutcome.Failed, result.Outcome);
    }

    [Fact]
    public async Task StartAsync_ReturnsCompletedImmediately_ForATourWithNoSteps()
    {
        var service = new TourService(new InMemoryTourStateStore());
        service.AttachPresenter(new FakeTourPresenter());
        service.Register(CreateTour(steps: 0));

        var result = await service.StartAsync("demo");

        Assert.Equal(TourOutcome.Completed, result.Outcome);
    }

    [Fact]
    public async Task StartAsync_ReturnsFailed_WhenEveryStepFailsToResolve()
    {
        var service = new TourService(new InMemoryTourStateStore());
        var presenter = new FakeTourPresenter { NextShowStepSucceeds = false };
        service.AttachPresenter(presenter);
        service.Register(CreateTour());

        var result = await service.StartAsync("demo");

        Assert.Equal(TourOutcome.Failed, result.Outcome);
    }

    [Fact]
    public async Task StartAsync_EndsTheFirstTour_WhenASecondIsRequestedWhileOneIsRunning()
    {
        var service = new TourService(new InMemoryTourStateStore());
        service.AttachPresenter(new FakeTourPresenter());
        service.Register(CreateTour("first"));
        service.Register(CreateTour("second"));

        var firstTask = service.StartAsync("first");
        var secondTask = service.StartAsync("second");

        var firstResult = await firstTask;
        Assert.Equal(TourOutcome.Ended, firstResult.Outcome);
        Assert.Equal("second", service.RunningTourKey);

        service.End();
        await secondTask;
    }
}
