using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using SimpleErp.Core.Abstractions;
using SimpleErp.Core.DependencyInjection;
using SimpleErp.Services;

namespace SimpleErp;

/// <summary>Composition root: registers the core and the WPF services, then shows the main window.</summary>
public partial class App : Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        var services = new ServiceCollection();
        services.AddSimpleErpCore();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IShellService, ShellService>();
        services.AddSingleton<MainWindow>();
        _services = services.BuildServiceProvider();

        MainWindow = _services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Keep the application alive and tell the user; the data on disk is never left half-written.
        e.Handled = true;
        _services?.GetService<IDialogService>()?.ShowError("Unexpected error", $"Something went wrong:\n\n{e.Exception.Message}");
    }
}
