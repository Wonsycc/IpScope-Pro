using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IpScopePro.Models;
using IpScopePro.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace IpScopePro.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public event Action<string, string>? OnWindowsToastRequested; /* title, message */

    private readonly ProbeManagerService _probeManager;
    private readonly ApplicationOptions _globalOptions;
    private readonly NotificationService _notificationService;
    private readonly FloatingAlertsService _floatingAlerts;
    private readonly DataPersistenceService _dataPersistence;
    private readonly ThemeService _themeService;
    private readonly SemaphoreSlim _semaphore;

    private readonly List<ProbeViewModel> _probeVms = new();

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private bool _isDarkTheme = true;
    [ObservableProperty] private string _windowTitle = "IpScope Pro - Probes";
    [ObservableProperty] private bool _isScrollView;
    [ObservableProperty] private bool _isFixedOverflowing;
    [ObservableProperty] private ProbeViewModel? _maximizedProbeVm;

    [RelayCommand]
    public void SelectProbeTab() => SelectedTabIndex = 0;

    [RelayCommand]
    public void SelectScannerTab() => SelectedTabIndex = 1;

    public ObservableCollection<ProbeViewModel> Probes { get; } = new();

    public MainViewModel(
        ProbeManagerService probeManager,
        ApplicationOptions globalOptions,
        NotificationService notificationService,
        FloatingAlertsService floatingAlerts,
        DataPersistenceService dataPersistence,
        ThemeService themeService)
    {
        _probeManager = probeManager;
        _globalOptions = globalOptions;
        _notificationService = notificationService;
        _floatingAlerts = floatingAlerts;
        _dataPersistence = dataPersistence;
        _themeService = themeService;
        _semaphore = new SemaphoreSlim(_globalOptions.ConcurrentPingsLimit);

        _notificationService.OnPopupNotification += HandlePopupNotification;
        _notificationService.OnWindowsNotification += HandleWindowsNotification;

        IsDarkTheme = _globalOptions.IsDarkTheme;
        IsScrollView = _globalOptions.IsScrollView;
        WindowTitle = LocalizationService.Instance["WindowTitleProbes"];
        LocalizationService.Instance.LanguageChanged += _ =>
            WindowTitle = LocalizationService.Instance["WindowTitleProbes"];
        LoadProbes();
    }

    private void HandlePopupNotification(StatusChangeLogEntry entry)
    {
        var app = Application.Current;
        if (app == null) return;

        app.Dispatcher.BeginInvoke(() =>
        {
            _floatingAlerts.AddEntry(entry);
        });
    }

    private void HandleWindowsNotification(StatusChangeLogEntry entry)
    {
        var app = Application.Current;
        if (app == null) return;

        app.Dispatcher.BeginInvoke(() =>
        {
            _floatingAlerts.AddEntry(entry);
            OnWindowsToastRequested?.Invoke(entry.Title, $"{entry.Hostname}: {entry.Body}");
        });
    }

    [RelayCommand]
    public void AddProbe(string hostname)
    {
        var probe = new Probe
        {
            Status = ProbeStatus.Indeterminate
        };
        probe.ParseHostname(hostname);

        _probeManager.AddProbe(probe);
        var vm = CreateProbeVm(probe);
        _probeVms.Add(vm);
        Probes.Add(vm);
    }

    public void AddProbeFromScanner(string hostname, string? alias = null, int port = 0)
    {
        var probe = new Probe
        {
            Status = ProbeStatus.Indeterminate
        };

        var displayAlias = alias ?? string.Empty;
        if (port > 0)
        {
            probe.ParseHostname($"{hostname}:{port}");
            if (!string.IsNullOrWhiteSpace(displayAlias))
                displayAlias = $"{displayAlias}:{port}";
        }
        else
        {
            probe.ParseHostname(hostname);
        }

        probe.Alias = displayAlias;

        _probeManager.AddProbe(probe);
        var vm = CreateProbeVm(probe);
        _probeVms.Add(vm);
        Probes.Add(vm);
    }

    [RelayCommand]
    public void RemoveProbe(ProbeViewModel vm)
    {
        if (MaximizedProbeVm == vm)
            MaximizedProbeVm = null;

        vm.StopProbe();
        _probeManager.RemoveProbe(vm.Model);
        _probeVms.Remove(vm);
        Probes.Remove(vm);
        _probeManager.SaveProbes();
    }

    [RelayCommand]
    public void StartAllProbes()
    {
        foreach (var vm in Probes)
            vm.StartProbe();
    }

    [RelayCommand]
    public void StopAllProbes()
    {
        foreach (var vm in Probes)
            vm.StopProbe();
    }

    [RelayCommand]
    public void RemoveAllProbes()
    {
        MaximizedProbeVm = null;
        var toRemove = Probes.ToList();
        foreach (var vm in toRemove)
            RemoveProbe(vm);
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        _themeService.ToggleTheme();
        IsDarkTheme = _themeService.IsDarkTheme;
        _globalOptions.IsDarkTheme = IsDarkTheme;
        _globalOptions.Save();
        RefreshAllProbeColors();
    }

    [RelayCommand]
    public void ToggleScrollView()
    {
        IsScrollView = !IsScrollView;
        _globalOptions.IsScrollView = IsScrollView;
        _globalOptions.Save();
    }

    public void RequestMaximize(ProbeViewModel vm)
    {
        MaximizedProbeVm = vm.IsMaximized ? vm : null;
    }

    [RelayCommand]
    public void SaveProbes()
    {
        _probeManager.SaveProbes();
    }

    public void RefreshAllProbeColors()
    {
        foreach (var vm in Probes)
        {
            var opt = vm.Model.Options;
            opt.BackgroundColorUp = _globalOptions.BackgroundColorUp;
            opt.BackgroundColorDown = _globalOptions.BackgroundColorDown;
            opt.BackgroundColorError = _globalOptions.BackgroundColorError;
            opt.BackgroundColorIndeterminate = _globalOptions.BackgroundColorIndeterminate;
            opt.BackgroundColorLatencyHigh = _globalOptions.BackgroundColorLatencyHigh;
            opt.BackgroundColorInactive = _globalOptions.BackgroundColorInactive;
            opt.BackgroundColorUpLight = _globalOptions.BackgroundColorUpLight;
            opt.BackgroundColorDownLight = _globalOptions.BackgroundColorDownLight;
            opt.BackgroundColorErrorLight = _globalOptions.BackgroundColorErrorLight;
            opt.BackgroundColorIndeterminateLight = _globalOptions.BackgroundColorIndeterminateLight;
            opt.BackgroundColorLatencyHighLight = _globalOptions.BackgroundColorLatencyHighLight;
            opt.BackgroundColorInactiveLight = _globalOptions.BackgroundColorInactiveLight;
            opt.TextColorUp = _globalOptions.TextColorUp;
            opt.TextColorDown = _globalOptions.TextColorDown;
            opt.TextColorError = _globalOptions.TextColorError;
            opt.TextColorIndeterminate = _globalOptions.TextColorIndeterminate;
            opt.TextColorLatencyHigh = _globalOptions.TextColorLatencyHigh;
            opt.TextColorInactive = _globalOptions.TextColorInactive;
            opt.StatsColorUp = _globalOptions.StatsColorUp;
            opt.StatsColorDown = _globalOptions.StatsColorDown;
            opt.StatsColorError = _globalOptions.StatsColorError;
            opt.StatsColorIndeterminate = _globalOptions.StatsColorIndeterminate;
            opt.StatsColorLatencyHigh = _globalOptions.StatsColorLatencyHigh;
            opt.StatsColorInactive = _globalOptions.StatsColorInactive;
            opt.AliasColorUp = _globalOptions.AliasColorUp;
            opt.AliasColorDown = _globalOptions.AliasColorDown;
            opt.AliasColorError = _globalOptions.AliasColorError;
            opt.AliasColorIndeterminate = _globalOptions.AliasColorIndeterminate;
            opt.AliasColorLatencyHigh = _globalOptions.AliasColorLatencyHigh;
            opt.AliasColorInactive = _globalOptions.AliasColorInactive;
            vm.RefreshColors();
        }
    }

    public void LoadProbes()
    {
        _probeManager.LoadProbes();

        foreach (var probe in _probeManager.Probes)
        {
            var vm = CreateProbeVm(probe);
            _probeVms.Add(vm);
            Probes.Add(vm);
        }
    }

    private ProbeViewModel CreateProbeVm(Probe probe)
    {
        var vm = new ProbeViewModel(probe, _globalOptions, _notificationService, _semaphore);
        vm.OnMaximizeToggled += HandleMaximizeToggled;
        return vm;
    }

    private void HandleMaximizeToggled(ProbeViewModel vm)
    {
        MaximizedProbeVm = vm.IsMaximized ? vm : null;
    }

    [RelayCommand]
    public async Task ExportProbes()
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json"
            };

            if (dlg.ShowDialog() == true)
            {
                var content = _dataPersistence.ExportProbes(_probeManager.Probes);
                await File.WriteAllTextAsync(dlg.FileName, content);
            }
        }
        catch { }
    }

    [RelayCommand]
    public async Task ImportProbes()
    {
        try
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json"
            };

            if (dlg.ShowDialog() == true)
            {
                var content = await File.ReadAllTextAsync(dlg.FileName);
                var imported = _dataPersistence.ImportProbes(content);

                if (imported != null)
                {
                    foreach (var probe in imported)
                    {
                        _probeManager.AddProbe(probe);
                        var vm = CreateProbeVm(probe);
                        _probeVms.Add(vm);
                        Probes.Add(vm);
                    }
                    _probeManager.SaveProbes();
                }
            }
        }
        catch { }
    }
}
