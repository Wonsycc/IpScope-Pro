using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IpScopePro.Helpers;
using IpScopePro.Models;
using IpScopePro.Services;

namespace IpScopePro.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ApplicationOptions _options;
    private readonly NotificationService _notificationService;
    private readonly TelegramService _telegramService;
    private readonly InstallerService _installerService;

    [ObservableProperty] private bool _isInstalled;

    [ObservableProperty] private int _pingIntervalMs;
    [ObservableProperty] private int _pingTimeoutMs;
    [ObservableProperty] private int _failedPingsBeforeDown;
    [ObservableProperty] private int _highLatencyThresholdMs;
    [ObservableProperty] private int _concurrentPingsLimit;
    [ObservableProperty] private LatencyMode _latencyMode;
    [ObservableProperty] private bool _notificationsEnabled;
    [ObservableProperty] private PopupNotificationOption _popupOption;
    [ObservableProperty] private bool _windowsNotificationsEnabled;
    [ObservableProperty] private int _cooldownSeconds;
    [ObservableProperty] private int _windowsCooldownSeconds;
    [ObservableProperty] private int _emailCooldownSeconds;
    [ObservableProperty] private int _telegramCooldownSeconds;
    [ObservableProperty] private int _audioCooldownSeconds;
    [ObservableProperty] private bool _emailEnabled;
    [ObservableProperty] private string _smtpServer = string.Empty;
    [ObservableProperty] private int _smtpPort = 587;
    [ObservableProperty] private string _smtpUsername = string.Empty;
    [ObservableProperty] private string _smtpPassword = string.Empty;
    [ObservableProperty] private string _emailFrom = string.Empty;
    [ObservableProperty] private string _emailTo = string.Empty;
    [ObservableProperty] private bool _smtpUseSsl = true;
    [ObservableProperty] private bool _telegramEnabled;
    [ObservableProperty] private string _telegramBotToken = string.Empty;
    [ObservableProperty] private string _telegramChatId = string.Empty;
    [ObservableProperty] private bool _audioEnabled;
    [ObservableProperty] private string _audioFilePath = string.Empty;
    [ObservableProperty] private bool _logToFile;
    [ObservableProperty] private string _logDirectory = string.Empty;
    [ObservableProperty] private StartMode _startMode;
    [ObservableProperty] private bool _minimizeToTray = true;
    [ObservableProperty] private bool _startWithWindows;
    [ObservableProperty] private string _fontFamily = "Consolas";
    [ObservableProperty] private double _fontSize = 12;
    [ObservableProperty] private string _seedColor = "#16A34A";
    [ObservableProperty] private AppLanguage _language = AppLanguage.Spanish;
    [ObservableProperty] private int _languageIndex;

    [ObservableProperty] private string _colorUp = "#16A34A";
    [ObservableProperty] private string _colorDown = "#DC322F";
    [ObservableProperty] private string _colorError = "#D97706";
    [ObservableProperty] private string _colorIndeterminate = "#555555";
    [ObservableProperty] private string _colorLatencyHigh = "#E77500";
    [ObservableProperty] private string _colorInactive = "#F0EFEE";

    [ObservableProperty] private string _colorUpLight = "#22C55E";
    [ObservableProperty] private string _colorDownLight = "#EF4444";
    [ObservableProperty] private string _colorErrorLight = "#F59E0B";
    [ObservableProperty] private string _colorIndeterminateLight = "#9CA3AF";
    [ObservableProperty] private string _colorLatencyHighLight = "#F97316";
    [ObservableProperty] private string _colorInactiveLight = "#F5F5F4";
    [ObservableProperty] private string _colorIpAddress = "#E8E8E8";
    [ObservableProperty] private string _colorIpAddressLight = "#111111";

    public SettingsViewModel(ApplicationOptions options, NotificationService notificationService,
        TelegramService telegramService, InstallerService installerService)
    {
        _options = options;
        _notificationService = notificationService;
        _telegramService = telegramService;
        _installerService = installerService;
        LoadFromOptions();
    }

    private void LoadFromOptions()
    {
        IsInstalled = AppEnvironment.IsInstalled;
        PingIntervalMs = _options.PingIntervalMs;
        PingTimeoutMs = _options.PingTimeoutMs;
        FailedPingsBeforeDown = _options.FailedPingsBeforeDown;
        HighLatencyThresholdMs = _options.HighLatencyThresholdMs;
        ConcurrentPingsLimit = _options.ConcurrentPingsLimit;
        LatencyMode = _options.LatencyMode;
        NotificationsEnabled = _options.NotificationsEnabled;
        PopupOption = _options.PopupOption;
        WindowsNotificationsEnabled = _options.WindowsNotificationsEnabled;
        CooldownSeconds = _options.CooldownSeconds;
        WindowsCooldownSeconds = _options.WindowsCooldownSeconds;
        EmailCooldownSeconds = _options.EmailCooldownSeconds;
        TelegramCooldownSeconds = _options.TelegramCooldownSeconds;
        AudioCooldownSeconds = _options.AudioCooldownSeconds;
        EmailEnabled = _options.EmailEnabled;
        SmtpServer = _options.SmtpServer;
        SmtpPort = _options.SmtpPort;
        SmtpUsername = _options.SmtpUsername;
        SmtpPassword = _options.SmtpPassword;
        EmailFrom = _options.EmailFrom;
        EmailTo = _options.EmailTo;
        SmtpUseSsl = _options.SmtpUseSsl;
        TelegramEnabled = _options.TelegramEnabled;
        TelegramBotToken = _options.TelegramBotToken;
        TelegramChatId = _options.TelegramChatId;
        AudioEnabled = _options.AudioEnabled;
        AudioFilePath = _options.AudioFilePath;
        LogToFile = _options.LogToFile;
        LogDirectory = _options.LogDirectory;
        StartMode = _options.StartMode;
        MinimizeToTray = _options.MinimizeToTray;
        StartWithWindows = IsInstalled ? _installerService.IsStartWithWindows() : false;
        FontFamily = _options.FontFamily;
        FontSize = _options.FontSize;
        SeedColor = _options.SeedColor;
        Language = _options.Language;
        LanguageIndex = _options.Language == AppLanguage.English ? 1 : 0;

        ColorUp = _options.BackgroundColorUp;
        ColorDown = _options.BackgroundColorDown;
        ColorError = _options.BackgroundColorError;
        ColorIndeterminate = _options.BackgroundColorIndeterminate;
        ColorLatencyHigh = _options.BackgroundColorLatencyHigh;
        ColorInactive = _options.BackgroundColorInactive;

        ColorUpLight = _options.BackgroundColorUpLight;
        ColorDownLight = _options.BackgroundColorDownLight;
        ColorErrorLight = _options.BackgroundColorErrorLight;
        ColorIndeterminateLight = _options.BackgroundColorIndeterminateLight;
        ColorLatencyHighLight = _options.BackgroundColorLatencyHighLight;
        ColorInactiveLight = _options.BackgroundColorInactiveLight;
        ColorIpAddress = _options.IpTextColor;
        ColorIpAddressLight = _options.IpTextColorLight;
    }

    [RelayCommand]
    public void Save()
    {
        _options.PingIntervalMs = PingIntervalMs;
        _options.PingTimeoutMs = PingTimeoutMs;
        _options.FailedPingsBeforeDown = FailedPingsBeforeDown;
        _options.HighLatencyThresholdMs = HighLatencyThresholdMs;
        _options.ConcurrentPingsLimit = ConcurrentPingsLimit;
        _options.LatencyMode = LatencyMode;
        _options.NotificationsEnabled = NotificationsEnabled;
        _options.PopupOption = PopupOption;
        _options.WindowsNotificationsEnabled = WindowsNotificationsEnabled;
        _options.CooldownSeconds = CooldownSeconds;
        _options.WindowsCooldownSeconds = WindowsCooldownSeconds;
        _options.EmailCooldownSeconds = EmailCooldownSeconds;
        _options.TelegramCooldownSeconds = TelegramCooldownSeconds;
        _options.AudioCooldownSeconds = AudioCooldownSeconds;
        _options.EmailEnabled = EmailEnabled;
        _options.SmtpServer = SmtpServer;
        _options.SmtpPort = SmtpPort;
        _options.SmtpUsername = SmtpUsername;
        _options.SmtpPassword = SmtpPassword;
        _options.EmailFrom = SmtpUsername;
        _options.EmailTo = EmailTo;
        _options.SmtpUseSsl = SmtpUseSsl;
        _options.TelegramEnabled = TelegramEnabled;
        _options.TelegramBotToken = TelegramBotToken;
        _options.TelegramChatId = TelegramChatId;
        _options.AudioEnabled = AudioEnabled;
        _options.AudioFilePath = AudioFilePath;
        _options.LogToFile = LogToFile;
        _options.LogDirectory = LogDirectory;
        _options.StartMode = StartMode;
        _options.MinimizeToTray = MinimizeToTray;
        _options.FontFamily = FontFamily;
        _options.FontSize = FontSize;
        _options.SeedColor = SeedColor;
        _options.Language = LanguageIndex == 1 ? AppLanguage.English : AppLanguage.Spanish;

        LocalizationService.Instance.Language = _options.Language;

        _options.BackgroundColorUp = ColorUp;
        _options.BackgroundColorDown = ColorDown;
        _options.BackgroundColorError = ColorError;
        _options.BackgroundColorIndeterminate = ColorIndeterminate;
        _options.BackgroundColorLatencyHigh = ColorLatencyHigh;
        _options.BackgroundColorInactive = ColorInactive;

        _options.BackgroundColorUpLight = ColorUpLight;
        _options.BackgroundColorDownLight = ColorDownLight;
        _options.BackgroundColorErrorLight = ColorErrorLight;
        _options.BackgroundColorIndeterminateLight = ColorIndeterminateLight;
        _options.BackgroundColorLatencyHighLight = ColorLatencyHighLight;
        _options.BackgroundColorInactiveLight = ColorInactiveLight;

        _options.IpTextColor = ColorIpAddress;
        _options.IpTextColorLight = ColorIpAddressLight;

        _options.StartWithWindows = StartWithWindows;
        _options.Save();

        if (IsInstalled)
            _installerService.SetStartWithWindows(StartWithWindows);
    }

    [RelayCommand]
    public void BrowseAudioFile()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Audio files (*.wav;*.mp3;*.ogg)|*.wav;*.mp3;*.ogg|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
            AudioFilePath = dlg.FileName;
    }

    [RelayCommand]
    public void BrowseLogDirectory()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = LocalizationService.Instance["LogDirectory"],
            InitialDirectory = LogDirectory
        };
        if (dlg.ShowDialog() == true)
            LogDirectory = dlg.FolderName;
    }

    [RelayCommand]
    public void ResetColors()
    {
        ColorUp = "#16A34A";
        ColorDown = "#DC322F";
        ColorError = "#D97706";
        ColorIndeterminate = "#555555";
        ColorLatencyHigh = "#E77500";
        ColorInactive = "#F0EFEE";
        ColorUpLight = "#22C55E";
        ColorDownLight = "#EF4444";
        ColorErrorLight = "#F59E0B";
        ColorIndeterminateLight = "#9CA3AF";
        ColorLatencyHighLight = "#F97316";
        ColorInactiveLight = "#F5F5F4";
        ColorIpAddress = "#E8E8E8";
        ColorIpAddressLight = "#111111";
    }

    [RelayCommand]
    public async Task TestEmail()
    {
        if (string.IsNullOrWhiteSpace(SmtpServer) || string.IsNullOrWhiteSpace(SmtpUsername) ||
            string.IsNullOrWhiteSpace(SmtpPassword) || string.IsNullOrWhiteSpace(EmailTo))
        {
            MessageBox.Show(LocalizationService.Instance["FillEmailFields"],
                LocalizationService.Instance["TestEmailTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var (success, error) = await _notificationService.SendTestEmail(
            SmtpServer, SmtpPort, SmtpUseSsl,
            SmtpUsername, SmtpPassword, SmtpUsername, EmailTo);

        if (success)
            MessageBox.Show(LocalizationService.Instance["TestEmailSent"], LocalizationService.Instance["TestEmailTitle"], MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show(string.Format(LocalizationService.Instance["TestEmailFailed"], error), LocalizationService.Instance["TestEmailTitle"], MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [RelayCommand]
    public async Task TestTelegram()
    {
        if (string.IsNullOrWhiteSpace(TelegramBotToken) || string.IsNullOrWhiteSpace(TelegramChatId))
        {
            MessageBox.Show(LocalizationService.Instance["FillTelegram"],
                LocalizationService.Instance["TestTelegramTitle"], MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var (success, error) = await _telegramService.SendTestMessage(TelegramBotToken, TelegramChatId);

        if (success)
            MessageBox.Show(LocalizationService.Instance["TestTelegramSent"], LocalizationService.Instance["TestTelegramTitle"], MessageBoxButton.OK, MessageBoxImage.Information);
        else
            MessageBox.Show(string.Format(LocalizationService.Instance["TestTelegramFailed"], error), LocalizationService.Instance["TestTelegramTitle"], MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
