using System.Text.Json.Serialization;

namespace IpScopePro.Models;

public class ProbeConfig
{
    public string Hostname { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
}

public class Probe
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Hostname { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public ProbeType Type { get; set; } = ProbeType.Icmp;
    public int Port { get; set; } = 80;
    public ProbeStatus Status { get; set; } = ProbeStatus.Indeterminate;
    public ProbeStatus PreviousStatus { get; set; } = ProbeStatus.Indeterminate;
    public PingStatistics Statistics { get; set; } = new();
    public ProbeOptions Options { get; set; } = new();
    public List<PingHistoryEntry> History { get; set; } = new();
    public List<StatusChangeLogEntry> StatusChanges { get; set; } = new();
    public bool IsRunning { get; set; }
    public bool IsMaximized { get; set; }
    public int FailedPingCount { get; set; }
    public DateTime? LastResponseTime { get; set; }

    [JsonIgnore]
    public string ParsedHostname { get; set; } = string.Empty;

    [JsonIgnore]
    public object HistoryLock { get; } = new();

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Alias) ? Hostname : Alias;

    public void ParseHostname(string raw)
    {
        Hostname = raw ?? string.Empty;

        if (string.IsNullOrWhiteSpace(raw))
        {
            ParsedHostname = string.Empty;
            return;
        }

        var lastColon = raw.LastIndexOf(':');
        if (lastColon > 0 && lastColon < raw.Length - 1)
        {
            var portStr = raw[(lastColon + 1)..];
            if (int.TryParse(portStr, out var parsedPort) && parsedPort > 0 && parsedPort <= 65535)
            {
                ParsedHostname = raw[..lastColon];
                Port = parsedPort;
                Type = ProbeType.Tcp;
                return;
            }
        }

        ParsedHostname = raw;
    }

    public void AddHistory(PingHistoryEntry entry)
    {
        lock (HistoryLock)
        {
            History.Add(entry);
            if (History.Count > 200)
                History.RemoveAt(0);
        }
    }

    public void RecordStatusChange(ProbeStatus oldStatus, ProbeStatus newStatus)
    {
        var entry = new StatusChangeLogEntry
        {
            Hostname = Hostname,
            Alias = Alias,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            DownCount = FailedPingCount
        };
        StatusChanges.Add(entry);
        if (StatusChanges.Count > 500)
            StatusChanges.RemoveAt(0);
    }
}

public class PingHistoryEntry
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public ProbeStatus Status { get; set; }
    public double RttMs { get; set; }
    public int Ttl { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum ProbeType
{
    Icmp,
    Tcp
}
