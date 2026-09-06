using System.Windows;
using Spyglass.Demo.ViewModels;

namespace Spyglass.Demo;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
