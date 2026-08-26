using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Vexel.App.Infrastructure;
using Vexel.App.ViewModels;
using Vexel.Core.Logging;
using Vexel.Core.Minecraft;
using Vexel.Core.Settings;
using Vexel.Platform.Windows.Detection;

namespace Vexel.App;

public partial class App : Application
{
    private readonly ServiceProvider _services;

    public App()
    {
        var services = new ServiceCollection();
        services.AddSingleton<VexelPaths>();
        services.AddSingleton<ISettingsStore>(provider =>
            new JsonSettingsStore(provider.GetRequiredService<VexelPaths>().Settings));
        services.AddSingleton<IAppLogger>(provider =>
            new JsonFileLogger(provider.GetRequiredService<VexelPaths>().Logs));
        services.AddSingleton<IMinecraftDetector, MinecraftDetector>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        _services = services.BuildServiceProvider();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var mainWindow = _services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        await ((MainViewModel)mainWindow.DataContext).InitializeAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services.Dispose();
        base.OnExit(e);
    }
}
