using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Spyglass.Demo.ViewModels;

namespace Spyglass.Demo.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;

        if (DataContext is MainViewModel viewModel)
        {
            Dispatcher.InvokeAsync(() => viewModel.RequestWelcomeTourAsync(), DispatcherPriority.ApplicationIdle);
        }
    }
}
