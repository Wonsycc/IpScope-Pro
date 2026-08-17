using System.IO;
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
        e.Handled = true;
        ShowError("IpScope Pro - Error inesperado", e.Exception, canContinue: true);
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ShowError("IpScope Pro - Error fatal", ex, canContinue: false);
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        e.SetObserved();
        try
        {
            var ex = e.Exception?.Flatten();
            if (ex != null)
                ShowError("IpScope Pro - Error en segundo plano", ex, canContinue: true);
        }
        catch { }
    }

    private static bool _errorDialogShowing;

    private static void ShowError(string title, Exception ex, bool canContinue)
    {
        try
        {
            LogError(ex);

            var detail = ex.ToString();
            if (detail.Length > 1800)
                detail = detail[..1800] + "\n... (truncado)";

            var msg = canContinue
                ? $"Se ha producido un error inesperado, pero la aplicación seguirá funcionando.\n\n" +
                  $"Tipo: {ex.GetType().FullName}\nMensaje: {ex.Message}\n\nDetalle:\n{detail}"
                : $"Se ha producido un error irrecuperable y la aplicación debe cerrarse.\n\n" +
                  $"Tipo: {ex.GetType().FullName}\nMensaje: {ex.Message}\n\nDetalle:\n{detail}";

            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
            {
                dispatcher.BeginInvoke(() => ShowMessageBox(title, msg));
                return;
            }

            ShowMessageBox(title, msg);
        }
        catch { }
    }

    private static void ShowMessageBox(string title, string msg)
    {
        if (_errorDialogShowing)
            return;

        _errorDialogShowing = true;
        try
        {
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch
        {
            // Ignore: no podemos hacer nada si hasta el cuadro de diálogo falla.
        }
        finally
        {
            _errorDialogShowing = false;
        }
    }

    private static void LogError(Exception ex)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IpScopePro");
        Directory.CreateDirectory(dir);
        File.AppendAllText(
            Path.Combine(dir, "errors.log"),
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}{Environment.NewLine}");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
<<<<<<< Updated upstream
=======
        if (e.Args.Any(a => a.Equals("--install", StringComparison.OrdinalIgnoreCase)))
        {
            RunHeadlessInstall(e.Args);
            return;
        }

        if (e.Args.Any(a => a.Equals("--uninstall", StringComparison.OrdinalIgnoreCase)))
        {
            RunHeadlessUninstall();
            return;
        }

>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
=======

    private void RunHeadlessInstall(string[] args)
    {
        try
        {
            var dir = GetArg(args, "--install-dir") ?? AppEnvironment.DefaultInstallDir;
            var desktop = bool.TryParse(GetArg(args, "--desktop-shortcut"), out var d) && d;
            var startMenu = bool.TryParse(GetArg(args, "--start-menu-shortcut"), out var s) && s;

            var installer = new InstallerService();
            var installedExe = installer.InstallAsync(dir, desktop, startMenu).GetAwaiter().GetResult();
            InstallerService.LaunchInstalled(installedExe);
            Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to install application: {ex.Message}", "IpScope Pro",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static string? GetArg(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }

    private void RunHeadlessUninstall()
    {
        try
        {
            var options = ApplicationOptions.Load();
            LocalizationService.Instance.Language = options.Language;

            Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = ThemeService.GetStartupThemeUri(options.IsDarkTheme)
            });

            var dialog = new UninstallDialog();
            if (dialog.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            var installer = new InstallerService();
            installer.Uninstall(dialog.DeleteData);
            MessageBox.Show(LocalizationService.Instance["UninstallSuccess"], "IpScope Pro",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(LocalizationService.Instance["UninstallFailed"], ex.Message),
                "IpScope Pro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Shutdown();
        }
    }
>>>>>>> Stashed changes
}
