using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IpScopePro.Helpers;

namespace IpScopePro.Models;

public enum PopupNotificationOption { AllProbes, OnlyGlobal, None }
public enum LatencyMode { Average, Latest }
public enum StartMode { Normal, Minimized, MinimizedToTray }
public enum AppLanguage { Spanish, English }

public class ApplicationOptions
{
    private static string ConfigPath => Path.Combine(AppEnvironment.DataDirectory, "settings.json");

    public int PingIntervalMs { get; set; } = 1000;
    public int PingTimeoutMs { get; set; } = 3000;
    public int FailedPingsBeforeDown { get; set; } = 3;
    public int HighLatencyThresholdMs { get; set; } = 200;
    public int ConcurrentPingsLimit { get; set; } = 50;
    public LatencyMode LatencyMode { get; set; } = LatencyMode.Average;

    public bool NotificationsEnabled { get; set; } = true;
    public PopupNotificationOption PopupOption { get; set; } = PopupNotificationOption.AllProbes;
    public bool WindowsNotificationsEnabled { get; set; } = true;
    public int CooldownSeconds { get; set; } = 30;

    public int WindowsCooldownSeconds { get; set; } = 30;
    public int EmailCooldownSeconds { get; set; } = 30;
    public int TelegramCooldownSeconds { get; set; } = 30;
    public int AudioCooldownSeconds { get; set; } = 30;

    public bool EmailEnabled { get; set; }
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string EmailFrom { get; set; } = string.Empty;
    public string EmailTo { get; set; } = string.Empty;
    public bool SmtpUseSsl { get; set; } = true;

    public bool TelegramEnabled { get; set; }
    public string TelegramBotToken { get; set; } = string.Empty;
    public string TelegramChatId { get; set; } = string.Empty;

    public bool AudioEnabled { get; set; }
    public string AudioFilePath { get; set; } = string.Empty;

    public bool LogToFile { get; set; }
    public string LogDirectory { get; set; } = Path.Combine(AppEnvironment.DataDirectory, "logs");

    public StartMode StartMode { get; set; } = StartMode.Normal;
    public bool MinimizeToTray { get; set; } = true;
    public bool StartWithWindows { get; set; }

    public bool IsDarkTheme { get; set; } = true;
    public bool IsScrollView { get; set; }
    public AppLanguage Language { get; set; } = AppLanguage.Spanish;
    public string SeedColor { get; set; } = "#16A34A";
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 12;

    public bool CheckForUpdates { get; set; } = true;

    public string BackgroundColorUp { get; set; } = "#16A34A";
    public string BackgroundColorDown { get; set; } = "#DC322F";
    public string BackgroundColorError { get; set; } = "#D97706";
    public string BackgroundColorIndeterminate { get; set; } = "#555555";
    public string BackgroundColorLatencyHigh { get; set; } = "#E77500";
    public string BackgroundColorInactive { get; set; } = "#F0EFEE";

    public string BackgroundColorUpLight { get; set; } = "#22C55E";
    public string BackgroundColorDownLight { get; set; } = "#EF4444";
    public string BackgroundColorErrorLight { get; set; } = "#F59E0B";
    public string BackgroundColorIndeterminateLight { get; set; } = "#9CA3AF";
    public string BackgroundColorLatencyHighLight { get; set; } = "#F97316";
    public string BackgroundColorInactiveLight { get; set; } = "#F5F5F4";

    public string TextColorUp { get; set; } = "#FFFFFF";
    public string TextColorDown { get; set; } = "#FFFFFF";
    public string TextColorError { get; set; } = "#FFFFFF";
    public string TextColorIndeterminate { get; set; } = "#FFFFFF";
    public string TextColorLatencyHigh { get; set; } = "#FFFFFF";
    public string TextColorInactive { get; set; } = "#000000";

    public string StatsColorUp { get; set; } = "#F0F9FF";
    public string StatsColorDown { get; set; } = "#FEF2F2";
    public string StatsColorError { get; set; } = "#FFFBEB";
    public string StatsColorIndeterminate { get; set; } = "#F5F5F5";
    public string StatsColorLatencyHigh { get; set; } = "#FFF7ED";
    public string StatsColorInactive { get; set; } = "#57534E";

    public string AliasColorUp { get; set; } = "#FFFFFF";
    public string AliasColorDown { get; set; } = "#FFFFFF";
    public string AliasColorError { get; set; } = "#FFFFFF";
    public string AliasColorIndeterminate { get; set; } = "#E5E5E5";
    public string AliasColorLatencyHigh { get; set; } = "#FFFFFF";
    public string AliasColorInactive { get; set; } = "#292524";

    public string IpTextColor { get; set; } = "#E8E8E8";
    public string IpTextColorLight { get; set; } = "#111111";

    public static ApplicationOptions Load()
    {
        if (!AppEnvironment.IsInstalled)
            return new ApplicationOptions();

        try
        {
            if (File.Exists(ConfigPath))
            {
                var content = File.ReadAllText(ConfigPath).Trim();

                var json = content.StartsWith('{')
                    ? content
                    : Encoding.UTF8.GetString(
                        ProtectedData.Unprotect(Convert.FromBase64String(content), null, DataProtectionScope.CurrentUser));

                return JsonSerializer.Deserialize<ApplicationOptions>(json) ?? new ApplicationOptions();
            }
        }
        catch { }
        return new ApplicationOptions();
    }

    public void Save()
    {
        if (!AppEnvironment.IsInstalled)
            return;

        try
        {
            var dir = Path.GetDirectoryName(ConfigPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            var cipher = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), null, DataProtectionScope.CurrentUser);
            File.WriteAllText(ConfigPath, Convert.ToBase64String(cipher));
        }
        catch { }
    }

    public string GetBackgroundForTheme(ProbeStatus status, bool isDark)
    {
        if (isDark) return GetStatusColor(status, "background");

        return status switch
        {
            ProbeStatus.Up => string.IsNullOrEmpty(BackgroundColorUpLight) ? BackgroundColorUp : BackgroundColorUpLight,
            ProbeStatus.Down => string.IsNullOrEmpty(BackgroundColorDownLight) ? BackgroundColorDown : BackgroundColorDownLight,
            ProbeStatus.Error => string.IsNullOrEmpty(BackgroundColorErrorLight) ? BackgroundColorError : BackgroundColorErrorLight,
            ProbeStatus.Indeterminate => string.IsNullOrEmpty(BackgroundColorIndeterminateLight) ? BackgroundColorIndeterminate : BackgroundColorIndeterminateLight,
            ProbeStatus.LatencyHigh => string.IsNullOrEmpty(BackgroundColorLatencyHighLight) ? BackgroundColorLatencyHigh : BackgroundColorLatencyHighLight,
            ProbeStatus.LatencyNormal => string.IsNullOrEmpty(BackgroundColorUpLight) ? BackgroundColorUp : BackgroundColorUpLight,
            ProbeStatus.Inactive => string.IsNullOrEmpty(BackgroundColorInactiveLight) ? BackgroundColorInactive : BackgroundColorInactiveLight,
            _ => "#37474F"
        };
    }

    public static string GetDefaultStatusColors(ProbeStatus status, string colorType) =>
        new ApplicationOptions().GetStatusColor(status, colorType);

    public string GetStatusColor(ProbeStatus status, string colorType)
    {
        return colorType switch
        {
            "background" => status switch
            {
                ProbeStatus.Up => BackgroundColorUp,
                ProbeStatus.Down => BackgroundColorDown,
                ProbeStatus.Error => BackgroundColorError,
                ProbeStatus.Indeterminate => BackgroundColorIndeterminate,
                ProbeStatus.LatencyHigh => BackgroundColorLatencyHigh,
                ProbeStatus.LatencyNormal => BackgroundColorUp,
                ProbeStatus.Inactive => BackgroundColorInactive,
                _ => "#37474F"
            },
            "text" => status switch
            {
                ProbeStatus.Up => TextColorUp,
                ProbeStatus.Down => TextColorDown,
                ProbeStatus.Error => TextColorError,
                ProbeStatus.Indeterminate => TextColorIndeterminate,
                ProbeStatus.LatencyHigh => TextColorLatencyHigh,
                ProbeStatus.LatencyNormal => TextColorUp,
                ProbeStatus.Inactive => TextColorInactive,
                _ => "#FFFFFF"
            },
            "stats" => status switch
            {
                ProbeStatus.Up => StatsColorUp,
                ProbeStatus.Down => StatsColorDown,
                ProbeStatus.Error => StatsColorError,
                ProbeStatus.Indeterminate => StatsColorIndeterminate,
                ProbeStatus.LatencyHigh => StatsColorLatencyHigh,
                ProbeStatus.LatencyNormal => StatsColorUp,
                ProbeStatus.Inactive => StatsColorInactive,
                _ => "#90A4AE"
            },
            "alias" => status switch
            {
                ProbeStatus.Up => AliasColorUp,
                ProbeStatus.Down => AliasColorDown,
                ProbeStatus.Error => AliasColorError,
                ProbeStatus.Indeterminate => AliasColorIndeterminate,
                ProbeStatus.LatencyHigh => AliasColorLatencyHigh,
                ProbeStatus.LatencyNormal => AliasColorUp,
                ProbeStatus.Inactive => AliasColorInactive,
                _ => "#B0BEC5"
            },
            _ => "#FFFFFF"
        };
    }
}
