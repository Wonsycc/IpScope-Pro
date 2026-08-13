using System.Text.Json.Serialization;

namespace IpScopePro.Models;

public class ProbeOptions : ICloneable
{
    public int PingIntervalMs { get; set; } = 1000;
    public int PingTimeoutMs { get; set; } = 3000;
    public int FailedPingsBeforeDown { get; set; } = 3;
    public int HighLatencyThresholdMs { get; set; } = 200;
    public bool Enabled { get; set; } = true;

    public bool PopupOnDown { get; set; } = true;
    public bool PopupOnUp { get; set; } = false;
    public bool PopupOnError { get; set; } = true;
    public bool PopupOnLatencyHigh { get; set; } = true;

    public bool EmailOnDown { get; set; } = true;
    public bool EmailOnUp { get; set; } = true;
    public bool EmailOnError { get; set; } = false;

    public bool TelegramOnDown { get; set; } = true;
    public bool TelegramOnUp { get; set; } = true;
    public bool TelegramOnError { get; set; } = false;

    public bool AudioOnDown { get; set; } = true;
    public bool AudioOnUp { get; set; } = false;
    public bool AudioOnError { get; set; } = false;

    public bool LogStatusChanges { get; set; } = true;

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

    public ProbeOptions Copy()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(this);
        return System.Text.Json.JsonSerializer.Deserialize<ProbeOptions>(json)!;
    }

    public object Clone() => Copy();

    public string GetBackgroundForTheme(ProbeStatus status, bool isDark)
    {
        if (isDark) return GetBackgroundColor(status);

        return status switch
        {
            ProbeStatus.Up => string.IsNullOrEmpty(BackgroundColorUpLight) ? BackgroundColorUp : BackgroundColorUpLight,
            ProbeStatus.Down => string.IsNullOrEmpty(BackgroundColorDownLight) ? BackgroundColorDown : BackgroundColorDownLight,
            ProbeStatus.Error => string.IsNullOrEmpty(BackgroundColorErrorLight) ? BackgroundColorError : BackgroundColorErrorLight,
            ProbeStatus.Indeterminate => string.IsNullOrEmpty(BackgroundColorIndeterminateLight) ? BackgroundColorIndeterminate : BackgroundColorIndeterminateLight,
            ProbeStatus.LatencyHigh => string.IsNullOrEmpty(BackgroundColorLatencyHighLight) ? BackgroundColorLatencyHigh : BackgroundColorLatencyHighLight,
            ProbeStatus.LatencyNormal => string.IsNullOrEmpty(BackgroundColorUpLight) ? BackgroundColorUp : BackgroundColorUpLight,
            ProbeStatus.Inactive => string.IsNullOrEmpty(BackgroundColorInactiveLight) ? BackgroundColorInactive : BackgroundColorInactiveLight,
            _ => GetBackgroundColor(status)
        };
    }

    public string GetBackgroundColor(ProbeStatus status) => status switch
    {
        ProbeStatus.Up => BackgroundColorUp,
        ProbeStatus.Down => BackgroundColorDown,
        ProbeStatus.Error => BackgroundColorError,
        ProbeStatus.Indeterminate => BackgroundColorIndeterminate,
        ProbeStatus.LatencyHigh => BackgroundColorLatencyHigh,
        ProbeStatus.LatencyNormal => BackgroundColorUp,
        ProbeStatus.Inactive => BackgroundColorInactive,
        _ => BackgroundColorIndeterminate
    };

    public string GetTextColor(ProbeStatus status) => status switch
    {
        ProbeStatus.Up => TextColorUp,
        ProbeStatus.Down => TextColorDown,
        ProbeStatus.Error => TextColorError,
        ProbeStatus.Indeterminate => TextColorIndeterminate,
        ProbeStatus.LatencyHigh => TextColorLatencyHigh,
        ProbeStatus.LatencyNormal => TextColorUp,
        ProbeStatus.Inactive => TextColorInactive,
        _ => TextColorIndeterminate
    };

    public string GetStatsColor(ProbeStatus status) => status switch
    {
        ProbeStatus.Up => StatsColorUp,
        ProbeStatus.Down => StatsColorDown,
        ProbeStatus.Error => StatsColorError,
        ProbeStatus.Indeterminate => StatsColorIndeterminate,
        ProbeStatus.LatencyHigh => StatsColorLatencyHigh,
        ProbeStatus.LatencyNormal => StatsColorUp,
        ProbeStatus.Inactive => StatsColorInactive,
        _ => StatsColorIndeterminate
    };

    public string GetAliasColor(ProbeStatus status) => status switch
    {
        ProbeStatus.Up => AliasColorUp,
        ProbeStatus.Down => AliasColorDown,
        ProbeStatus.Error => AliasColorError,
        ProbeStatus.Indeterminate => AliasColorIndeterminate,
        ProbeStatus.LatencyHigh => AliasColorLatencyHigh,
        ProbeStatus.LatencyNormal => AliasColorUp,
        ProbeStatus.Inactive => AliasColorInactive,
        _ => AliasColorIndeterminate
    };
}
