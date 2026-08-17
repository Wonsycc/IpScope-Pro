using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using IpScopePro.Models;
using IpScopePro.Services;
using IpScopePro.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace IpScopePro.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IServiceProvider _services;
    private readonly ThemeService _themeService;
    private readonly ApplicationOptions _options;

    private Rect _restoreBounds = Rect.Empty;
    private bool _isMaximized;

    public MainWindow(MainViewModel viewModel, IServiceProvider services)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _services = services;
        DataContext = viewModel;

        _themeService = services.GetRequiredService<ThemeService>();
        _options = services.GetRequiredService<ApplicationOptions>();
        _themeService.OnThemeChanged += HandleThemeChanged;
        StateChanged += OnWindowStateChanged;
        LocalizationService.Instance.LanguageChanged += _ =>
            Dispatcher.Invoke(() =>
            {
                UpdateThemeIcon();
                UpdateScrollViewLabel();
            });

        ScannerViewControl.DataContext = services.GetRequiredService<ScannerViewModel>();
        AppLogoImage.Source = LoadAppIcon();
        UpdateThemeIcon();
        UpdateScrollViewLabel();

        _viewModel.OnWindowsToastRequested += (title, message) =>
        {
            Dispatcher.Invoke(() =>
            {
                TrayIcon.ShowBalloonTip(title, message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            });
        };
    }

    private void HandleThemeChanged(bool isDark)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateThemeIcon();
        });
    }

    private void UpdateThemeIcon()
    {
        if (_themeService.IsDarkTheme)
        {
            ThemeIconGlyph.Text = "☀";
            ThemeLabel.Text = LocalizationService.Instance["LightTheme"];
        }
        else
        {
            ThemeIconGlyph.Text = "🌙";
            ThemeLabel.Text = LocalizationService.Instance["DarkTheme"];
        }
    }

    private void UpdateScrollViewLabel()
    {
        ScrollViewLabel.Text = _viewModel.IsScrollView
            ? LocalizationService.Instance["Scroll"]
            : LocalizationService.Instance["Fixed"];
    }

    private static BitmapSource? LoadAppIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Resources/app.ico");
            var info = Application.GetResourceStream(uri);
            if (info == null) return null;

            using var stream = info.Stream;
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return decoder.Frames.OrderByDescending(f => f.PixelWidth).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
            Maximize_Click(sender, e);
        else if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            var work = SystemParameters.WorkArea;
            MaxHeight = work.Height;
            MaxWidth = work.Width;
        }
        else if (WindowState == WindowState.Normal)
        {
            MaxHeight = double.PositiveInfinity;
            MaxWidth = double.PositiveInfinity;
        }

        if (WindowState == WindowState.Minimized && _options.MinimizeToTray)
            Hide();
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e) => RestoreFromTray();

    private void TrayOpen_Click(object sender, RoutedEventArgs e) => RestoreFromTray();

    private void TrayExit_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveProbesCommand.Execute(null);
        Application.Current.Shutdown();
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        if (_isMaximized || WindowState == WindowState.Maximized)
        {
            RestoreFromMaximize();
        }
        else
        {
            MaximizeToWorkArea();
        }
    }

    private void MaximizeToWorkArea()
    {
        _restoreBounds = new Rect(Left, Top, ActualWidth, ActualHeight);

        var work = GetCurrentMonitorWorkArea();

        MaxHeight = double.PositiveInfinity;
        MaxWidth = double.PositiveInfinity;
        WindowState = WindowState.Normal;

        Left = work.Left;
        Top = work.Top;
        Width = work.Width;
        Height = work.Height;
        _isMaximized = true;
    }

    private void RestoreFromMaximize()
    {
        _isMaximized = false;
        WindowState = WindowState.Normal;

        if (_restoreBounds != Rect.Empty)
        {
            Left = _restoreBounds.Left;
            Top = _restoreBounds.Top;
            Width = _restoreBounds.Width;
            Height = _restoreBounds.Height;
        }
    }

    private Rect GetCurrentMonitorWorkArea()
    {
        try
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (handle != IntPtr.Zero)
            {
                var screen = System.Windows.Forms.Screen.FromHandle(handle);
                var wa = screen.WorkingArea;
                return new Rect(wa.Left, wa.Top, wa.Width, wa.Height);
            }
        }
        catch { }
        return SystemParameters.WorkArea;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SaveProbesCommand.Execute(null);
        Application.Current.Shutdown();
    }

    private void ToggleTheme_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleThemeCommand.Execute(null);
        UpdateThemeIcon();
    }

    private void ToggleScrollView_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleScrollViewCommand.Execute(null);
        UpdateScrollViewLabel();
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsDialog = _services.GetRequiredService<SettingsDialog>();
        settingsDialog.Owner = this;
        settingsDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        settingsDialog.ShowDialog();
        _viewModel.RefreshAllProbeColors();
    }

    private void AddProbe_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AddProbeCommand.Execute(string.Empty);
    }

    private void RemoveAllProbes_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Probes.Count == 0) return;

        var result = MessageBox.Show(
            string.Format(LocalizationService.Instance["RemoveAllConfirm"], _viewModel.Probes.Count),
            LocalizationService.Instance["RemoveAllTitle"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
            _viewModel.RemoveAllProbesCommand.Execute(null);
    }

    private void BatchInput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new BatchInputDialog();
        dialog.Owner = this;
        dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Addresses))
        {
            foreach (var line in dialog.Addresses.Split('\n'))
            {
                var hostname = line.Trim();
                if (!string.IsNullOrWhiteSpace(hostname))
                    _viewModel.AddProbeCommand.Execute(hostname);
            }
        }
    }
}
