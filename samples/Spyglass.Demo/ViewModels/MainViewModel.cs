using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BlackBeard.Spyglass;
using Prism.Commands;
using Prism.Mvvm;
using Spyglass.Demo.Tours;

namespace Spyglass.Demo.ViewModels;

public sealed class MainViewModel : BindableBase
{
    private readonly ITourService _tourService;
    private bool _showTourEveryTime;

    public MainViewModel(ITourService tourService)
    {
        _tourService = tourService;
        _showTourEveryTime = _tourService.GetShowEveryTime(TourCatalog.WelcomeTourKey);
        AddItemCommand = new DelegateCommand(AddItem);
    }

    public ObservableCollection<string> Items { get; } = new() { "First item", "Second item" };

    public DelegateCommand AddItemCommand { get; }

    public bool ShowTourEveryTime
    {
        get => _showTourEveryTime;
        set
        {
            if (SetProperty(ref _showTourEveryTime, value))
            {
                _tourService.SetShowEveryTime(TourCatalog.WelcomeTourKey, value);
            }
        }
    }

    public Task RequestWelcomeTourAsync() => _tourService.RequestAsync(TourCatalog.WelcomeTourKey);

    private void AddItem() => Items.Add($"Item {Items.Count + 1}");
}
