using BlackBeard.Spyglass;
using Prism.Commands;
using Spyglass.Demo.Tours;

namespace Spyglass.Demo.ViewModels;

public sealed class MainWindowViewModel
{
    private readonly ITourService _tourService;

    public MainWindowViewModel(ITourService tourService)
    {
        _tourService = tourService;
        ReplayTourCommand = new DelegateCommand(ReplayTour);
    }

    public DelegateCommand ReplayTourCommand { get; }

    private async void ReplayTour()
    {
        await _tourService.StartAsync(TourCatalog.AdvancedFeaturesTourKey, showPrompt: true);
    }
}
