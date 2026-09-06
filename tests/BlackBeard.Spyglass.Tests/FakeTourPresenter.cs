using System;
using System.Collections.Generic;
using BlackBeard.Spyglass;

namespace BlackBeard.Spyglass.Tests;

/// <summary>
/// A UI-free stand-in for the real overlay, so <see cref="TourService"/> can be exercised without any WPF object.
/// </summary>
internal sealed class FakeTourPresenter : ITourPresenter
{
    public bool IsAttached { get; set; } = true;

    public bool DontShowAgainChecked { get; set; }

    public bool NextShowStepSucceeds { get; set; } = true;

    public List<(TourStep Step, int Index, int Count, bool IsFinal)> ShownSteps { get; } = new();

    public TourDefinition? PromptedTour { get; private set; }

    public bool HideAllCalled { get; private set; }

    public event EventHandler? StartRequested;

    public event EventHandler? SkipRequested;

    public void ShowPrompt(TourDefinition tour) => PromptedTour = tour;

    public bool ShowStep(TourStep step, int index, int count, bool isFinal)
    {
        if (!NextShowStepSucceeds)
        {
            return false;
        }

        ShownSteps.Add((step, index, count, isFinal));
        return true;
    }

    public void HideAll() => HideAllCalled = true;

    public void RaiseStart() => StartRequested?.Invoke(this, EventArgs.Empty);

    public void RaiseSkip() => SkipRequested?.Invoke(this, EventArgs.Empty);
}
