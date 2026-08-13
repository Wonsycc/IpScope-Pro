using IpScopePro.Services;

namespace IpScopePro.Models;

public class StatusChangeLogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Hostname { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public ProbeStatus OldStatus { get; set; }
    public ProbeStatus NewStatus { get; set; }
    public int DownCount { get; set; }
    public bool SentAsNotification { get; set; }

    public string NewStatusText => LocalizationService.Instance.TranslateStatus(NewStatus);

    public string Title => $"{GetDisplayName()} - {NewStatusText}";

    public string Body => string.Format(
        LocalizationService.Instance["StatusChangedBody"],
        LocalizationService.Instance.TranslateStatus(OldStatus),
        NewStatusText,
        Timestamp.ToString("HH:mm:ss"));

    public string GetDisplayName() =>
        string.IsNullOrWhiteSpace(Alias) ? Hostname : Alias;
}
