using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using IpScopePro.Helpers;
using IpScopePro.Models;
using IpScopePro.Services;
using IpScopePro.Views;

namespace IpScopePro;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var msg = $"Unexpected UI error:\n\n{e.Exception.GetType().Name}: {e.Exception.Message}";
        MessageBox.Show(msg, "IpScope Pro - Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            var msg = $"Fatal error:\n\n{ex.GetType().Name}: {ex.Message}";
            MessageBox.Show(msg, "IpScope Pro - Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            var services = new ServiceCollection();
            services.AddIpScopeServices();
            Services = services.BuildServiceProvider();

            var options = Services.GetRequiredService<ApplicationOptions>();
            var themeService = Services.GetRequiredService<ThemeService>();
            themeService.IsDarkTheme = options.IsDarkTheme;
            LocalizationService.Instance.Language = options.Language;

            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = ThemeService.GetStartupThemeUri(themeService.IsDarkTheme)
            });

            var mainWindow = Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            var autostart = e.Args.Any(a => a.Equals("--autostart", StringComparison.OrdinalIgnoreCase));
            if (autostart || options.StartMode != StartMode.Normal)
                mainWindow.WindowState = WindowState.Minimized;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start application: {ex.Message}", "IpScope Pro",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }
}
