using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IpScopePro.Models;
using IpScopePro.Services;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace IpScopePro.ViewModels;

public partial class ProbeViewModel : ObservableObject
{
    private readonly Probe _probe;
    private readonly ApplicationOptions _globalOptions;
    private readonly NotificationService _notificationService;
    private readonly SemaphoreSlim _semaphore;
    private ProbeEngine? _engine;

    public event Action<ProbeViewModel>? OnMaximizeToggled;

    public Probe Model => _probe;

    [ObservableProperty] private string _hostname = string.Empty;
    [ObservableProperty] private string _alias = string.Empty;

    partial void OnHostnameChanged(string value) => OnPropertyChanged(nameof(DisplayName));

    partial void OnAliasChanged(string value)
    {
        _probe.Alias = value ?? string.Empty;
        OnPropertyChanged(nameof(DisplayName));
    }

    [ObservableProperty] private ProbeType _type;
    [ObservableProperty] private int _port = 80;
    [ObservableProperty] private ProbeStatus _status = ProbeStatus.Indeterminate;
    [ObservableProperty] private bool _isRunning;
    [ObservableProperty] private bool _isMaximized;
    [ObservableProperty] private string _statusText = "?";
    [ObservableProperty] private string _statsText = "Sent: 0 | Recv: 0 | Lost: 0 (0%) | Avg: 0ms";
    [ObservableProperty] private string _latestRtt = "-";
    [ObservableProperty] private int _failedPingCount;
    [ObservableProperty] private SolidColorBrush _backgroundColor = new(Color.FromRgb(0x55, 0x55, 0x55));
    [ObservableProperty] private SolidColorBrush _textColor = new(Color.FromRgb(0xFF, 0xFF, 0xFF));
    [ObservableProperty] private SolidColorBrush _statsColor = new(Color.FromRgb(0x11, 0x11, 0x11));
    [ObservableProperty] private SolidColorBrush _aliasColor = new(Color.FromRgb(0xFF, 0xFF, 0xFF));
    [ObservableProperty] private SolidColorBrush _ipTextColor = new(Color.FromRgb(0xE8, 0xE8, 0xE8));
    [ObservableProperty] private bool _showStats = true;

    public string DisplayName => string.IsNullOrWhiteSpace(Alias) ? Hostname : Alias;

    public ObservableCollection<PingHistoryEntry> History { get; } = new();

    public ProbeViewModel(Probe probe, ApplicationOptions globalOptions,
        NotificationService notificationService, SemaphoreSlim semaphore)
    {
        _probe = probe;
        _globalOptions = globalOptions;
        _notificationService = notificationService;
        _semaphore = semaphore;

        SyncFromModel();
    }

    private void SyncFromModel()
    {
        Hostname = _probe.Hostname;
        Alias = _probe.Alias;
        Type = _probe.Type;
        Port = _probe.Port;
        Status = _probe.Status;
        IsRunning = _probe.IsRunning;
        IsMaximized = _probe.IsMaximized;
        FailedPingCount = _probe.FailedPingCount;
        UpdateColors();
    }

    [RelayCommand]
    public void TogglePing()
    {
        if (IsRunning)
        {
            StopProbe();
        }
        else
        {
            StartProbe();
        }
    }

    [RelayCommand]
    public void StartProbe()
    {
        if (string.IsNullOrWhiteSpace(Hostname)) return;

        try
        {
            StopProbeInternal();

            _probe.ParseHostname(Hostname);
            Type = _probe.Type;
            Port = _probe.Port;
            _probe.Alias = Alias;
            _probe.Statistics.Reset();

            lock (_probe.HistoryLock)
            {
                _probe.History.Clear();
            }
            History.Clear();

            _engine = new ProbeEngine(_probe, _globalOptions, _semaphore);
            _engine.OnProbeUpdated += HandleProbeUpdated;
            _engine.OnStatusChanged += HandleStatusChanged;
            _engine.Start();

            IsRunning = true;
            _probe.IsRunning = true;
        }
        catch { }
    }

    private void StopProbeInternal()
    {
        if (_engine == null) return;
        try
        {
            _engine.OnProbeUpdated -= HandleProbeUpdated;
            _engine.OnStatusChanged -= HandleStatusChanged;
            _engine.Stop();
            _engine.Dispose();
        }
        catch { }
        finally
        {
            _engine = null;
        }
    }

    [RelayCommand]
    public void StopProbe()
    {
        StopProbeInternal();

        IsRunning = false;
        _probe.IsRunning = false;
        Status = ProbeStatus.Inactive;
        _probe.Status = ProbeStatus.Inactive;

        UpdateColors();
    }

    [RelayCommand]
    public void ToggleMaximize()
    {
        IsMaximized = !IsMaximized;
        _probe.IsMaximized = IsMaximized;
        try { OnMaximizeToggled?.Invoke(this); } catch { }
    }

    private void HandleProbeUpdated(Probe probe)
    {
        var app = System.Windows.Application.Current;
        if (app == null) return;

        app.Dispatcher.BeginInvoke(() =>
        {
            Status = probe.Status;
            FailedPingCount = probe.FailedPingCount;

            StatusText = LocalizationService.Instance.TranslateStatus(probe.Status);

            if (probe.Statistics.RttHistory.Count > 0)
            {
                LatestRtt = probe.Statistics.LatestRtt >= 1
                    ? $"{probe.Statistics.LatestRtt:F0}ms"
                    : $"{(probe.Statistics.LatestRtt * 1000):F0}us";
            }

            StatsText = probe.Statistics.GetSummary(LocalizationService.Instance["StatsSummary"]);
            UpdateColors();
            SyncHistory(probe);
        });
    }

    private async void HandleStatusChanged(Probe probe, StatusChangeLogEntry entry)
    {
        try
        {
            await _notificationService.HandleStatusChange(probe, entry);
        }
        catch { }
    }

    public void RefreshColors() => UpdateColors();

    private void UpdateColors()
    {
        try
        {
            var isDark = _globalOptions.IsDarkTheme;
            var bg = ParseColor(_probe.Options.GetBackgroundForTheme(Status, isDark))
                ?? _globalOptions.GetBackgroundForTheme(Status, isDark);
            var fg = ParseColor(_probe.Options.GetTextColor(Status))
                ?? _globalOptions.GetStatusColor(Status, "text");
            var st = ParseColor(_probe.Options.GetStatsColor(Status))
                ?? _globalOptions.GetStatusColor(Status, "stats");
            var al = ParseColor(_probe.Options.GetAliasColor(Status))
                ?? _globalOptions.GetStatusColor(Status, "alias");

            var ipText = isDark ? _globalOptions.IpTextColor : _globalOptions.IpTextColorLight;

            BackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg));
            TextColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg));
            StatsColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(st));
            AliasColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(al));
            IpTextColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ipText));
        }
        catch { }
    }

    private void SyncHistory(Probe probe)
    {
        try
        {
            lock (probe.HistoryLock)
            {
                History.Clear();
                foreach (var entry in probe.History.TakeLast(100))
                    History.Add(entry);
            }
        }
        catch { }
    }

    private static string? ParseColor(string hex)
    {
        return hex?.StartsWith("#") == true ? hex : null;
    }
}
