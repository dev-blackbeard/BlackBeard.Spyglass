using System.Windows;
using BlackBeard.Spyglass;
using Prism.Ioc;
using Prism.Regions;
using Spyglass.Demo.Tours;
using Spyglass.Demo.Views;

namespace Spyglass.Demo;

public partial class App
{
    protected override Window CreateShell() => Container.Resolve<MainWindow>();

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSpyglass();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        var regionManager = Container.Resolve<IRegionManager>();
        regionManager.RegisterViewWithRegion("MainRegion", typeof(MainView));

        TourCatalog.RegisterAll(Container.Resolve<ITourService>());
    }
}
