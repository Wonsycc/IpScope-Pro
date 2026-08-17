using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;

namespace IpScopePro.Services;

public class ScanResult : INotifyPropertyChanged
{
    private bool _isAlive;

    public string Ip { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;

    public bool IsAlive
    {
        get => _isAlive;
        set
        {
            if (_isAlive == value) return;
            _isAlive = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAlive)));
        }
    }

    public double RttMs { get; set; }
    public List<int> OpenPorts { get; set; } = new();
    public string OsGuess { get; set; } = string.Empty;
    public string OpenPortsString => string.Join("; ", OpenPorts);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void NotifyPortsChanged()
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OpenPortsString)));
}

public enum ScanMode { Fast, Exhaustive }

public class NetworkScannerService
{
    private readonly SemaphoreSlim _portLimiter = new(512);
    private CancellationTokenSource? _cts;
    private static Dictionary<string, string>? _ouiDatabase;
    private long _lastProgressTick;

    public event Action<ScanResult>? OnHostFound;
    public event Action<int, int>? OnProgress;

    public async Task<List<ScanResult>> ScanNetwork(
        List<string> ips, bool scanPorts, List<int> ports, ScanMode mode)
    {
        _cts = new CancellationTokenSource();
        Interlocked.Exchange(ref _lastProgressTick, 0);

        var results = new List<ScanResult>(ips.Count);
        var pingTotal = ips.Count;
        var pingDone = 0;

        // Phase 1: ping every host first, collecting the full result set.
        await Parallel.ForEachAsync(ips,
            new ParallelOptions { MaxDegreeOfParallelism = 100, CancellationToken = _cts.Token },
            async (ip, ct) =>
            {
                var result = await PingHost(ip, mode, ct);
                lock (results) results.Add(result);
                var p = Interlocked.Increment(ref pingDone);
                ReportProgress(pingTotal > 0 ? (int)(p * 500L / pingTotal) : 0, 1000);
            });

        // Enrich alive hosts (hostname, MAC, vendor) using a single ARP read.
        var alive = results.Where(r => r.IsAlive).ToList();
        if (alive.Count > 0)
        {
            var arp = GetMacTable();
            await Parallel.ForEachAsync(alive,
                new ParallelOptions { MaxDegreeOfParallelism = 64, CancellationToken = _cts.Token },
                async (r, ct) =>
                {
                    try { r.Hostname = await ResolveHostname(r.Ip, ct); } catch { }
                    if (arp.TryGetValue(r.Ip, out var mac))
                    {
                        r.MacAddress = mac;
                        try { r.Vendor = ResolveVendor(mac); } catch { }
                    }
                });
        }

        // Stream the ping results to the UI.
        foreach (var r in results)
            OnHostFound?.Invoke(r);

        // Phase 2: only now analyze ports. Fast = alive hosts, Exhaustive = all hosts.
        var toScan = results
            .Where(r => scanPorts && ports.Count > 0 && (r.IsAlive || mode == ScanMode.Exhaustive))
            .ToList();

        var portTotal = toScan.Count > 0 ? toScan.Count * ports.Count : 0;

        if (toScan.Count > 0)
        {
            var portDone = 0;
            var portWork = toScan
                .SelectMany(r => ports.Select(p => (Host: r, Port: p)))
                .ToList();

            await Parallel.ForEachAsync(portWork,
                new ParallelOptions { MaxDegreeOfParallelism = 256, CancellationToken = _cts.Token },
                async (item, ct) =>
                {
                    await _portLimiter.WaitAsync(ct).ConfigureAwait(false);
                    try
                    {
                        if (await TryConnect(item.Host.Ip, item.Port, 200, ct))
                        {
                            lock (item.Host.OpenPorts) item.Host.OpenPorts.Add(item.Port);
                        }
                    }
                    finally
                    {
                        _portLimiter.Release();
                    }

                    var d = Interlocked.Increment(ref portDone);
                    ReportProgress(500 + (int)(d * 500L / portTotal), 1000);
                });

            foreach (var r in toScan)
            {
                r.OpenPorts.Sort();
                if (r.OpenPorts.Count > 0 && !r.IsAlive)
                    r.IsAlive = true;
                r.NotifyPortsChanged();
            }
        }

        ReportProgress(1000, 1000);
        return results;
    }

    public void Cancel() => _cts?.Cancel();

    private void ReportProgress(int current, int total)
    {
        var now = Environment.TickCount64;
        var last = Interlocked.Read(ref _lastProgressTick);
        if (current < total && now - last < 50) return;
        if (Interlocked.CompareExchange(ref _lastProgressTick, now, last) != last) return;
        OnProgress?.Invoke(current, total);
    }

    private async Task<ScanResult> PingHost(string ip, ScanMode mode, CancellationToken ct)
    {
        var result = new ScanResult { Ip = ip };

        try
        {
            using var ping = new Ping();
            var timeout = mode == ScanMode.Fast ? 500 : 3000;
            var reply = await ping.SendPingAsync(ip, timeout);
            result.IsAlive = reply.Status == IPStatus.Success;
            result.RttMs = reply.RoundtripTime;
        }
        catch
        {
            result.IsAlive = false;
        }

        return result;
    }

    private static async Task<string> ResolveHostname(string ip, CancellationToken ct)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(1000);
            var entry = await Dns.GetHostEntryAsync(ip, timeoutCts.Token);
            return entry.HostName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static async Task<bool> TryConnect(string ip, int port, int timeoutMs, CancellationToken ct)
    {
        using var client = new TcpClient();
        try
        {
            // Use the Task-based overload (not the ValueTask one with a cancellation
            // token) to avoid unobserved SocketException on cancellation/app exit.
            var connectTask = client.ConnectAsync(ip, port);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(timeoutMs);
            var timeoutTask = Task.Delay(Timeout.Infinite, timeoutCts.Token);

            var completed = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);

            if (completed != connectTask)
            {
                client.Dispose();
                try { await connectTask.ConfigureAwait(false); } catch { }
                return false;
            }

            await connectTask.ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static List<int> ParsePortList(string input)
    {
        var ports = new HashSet<int>();
        if (string.IsNullOrWhiteSpace(input)) return new List<int>();

        foreach (var part in input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Contains('-'))
            {
                var range = part.Split('-');
                if (range.Length == 2 &&
                    int.TryParse(range[0].Trim(), out var start) &&
                    int.TryParse(range[1].Trim(), out var end) &&
                    start <= end)
                {
                    start = Math.Max(1, start);
                    end = Math.Min(65535, end);
                    for (var p = start; p <= end; p++)
                        ports.Add(p);
                }
            }
            else if (int.TryParse(part, out var port) && port is >= 1 and <= 65535)
            {
                ports.Add(port);
            }
        }

        return ports.OrderBy(p => p).ToList();
    }

    private static Dictionary<string, string> GetMacTable()
    {
        var table = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            uint size = 0;
            GetIpNetTable(IntPtr.Zero, ref size, false);
            var buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                if (GetIpNetTable(buffer, ref size, false) != 0)
                    return table;

                var count = Marshal.ReadInt32(buffer);
                var rowSize = Marshal.SizeOf<MIB_IPNETROW>();
                var rowPtr = buffer + sizeof(int);

                for (var i = 0; i < count; i++)
                {
                    var row = Marshal.PtrToStructure<MIB_IPNETROW>(rowPtr + i * rowSize);
                    if (row.dwPhysAddrLen != 6) continue;

                    var mac = $"{row.bPhysAddr[0]:X2}-{row.bPhysAddr[1]:X2}-{row.bPhysAddr[2]:X2}-{row.bPhysAddr[3]:X2}-{row.bPhysAddr[4]:X2}-{row.bPhysAddr[5]:X2}";
                    if (mac == "00-00-00-00-00-00") continue;

                    var b = BitConverter.GetBytes(row.dwAddr);
                    table[$"{b[0]}.{b[1]}.{b[2]}.{b[3]}"] = mac;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
        catch { }
        return table;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetIpNetTable(IntPtr pIpNetTable, ref uint pdwSize, bool bOrder);

    [StructLayout(LayoutKind.Sequential)]
    private struct MIB_IPNETROW
    {
        public uint dwIndex;
        public uint dwPhysAddrLen;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] bPhysAddr;
        public uint dwAddr;
        public uint dwType;
    }

    private static string ResolveVendor(string mac)
    {
        if (string.IsNullOrWhiteSpace(mac) || mac.Length < 8)
            return "Unknown";

        var clean = mac.Replace(":", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
        _ouiDatabase ??= LoadOuiDatabase();
        if (_ouiDatabase.Count == 0)
            return "Unknown";

        // MA-S (36-bit, 9 hex) > MA-M (28-bit, 7 hex) > MA-L (24-bit, 6 hex)
        foreach (var len in new[] { 9, 7, 6 })
        {
            if (clean.Length >= len && _ouiDatabase.TryGetValue(clean[..len], out var vendor))
                return vendor;
        }

        return "Unknown";
    }

    private static Dictionary<string, string> LoadOuiDatabase()
    {
        var dict = new Dictionary<string, string>();
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var resourceNames = asm.GetManifestResourceNames();

            foreach (var file in new[] { "oui.csv", "mam.csv", "oui36.csv" })
            {
                var resourceName = resourceNames
                    .FirstOrDefault(n => n.EndsWith("." + file, StringComparison.OrdinalIgnoreCase));
                if (resourceName == null) continue;

                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream == null) continue;

                using var reader = new StreamReader(stream);
                while (reader.ReadLine() is { } line)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = ParseCsvLine(line);
                    if (fields.Count < 3) continue;

                    var assignment = fields[1].Trim().ToUpperInvariant();
                    var org = fields[2].Trim();
                    if (assignment == "ASSIGNMENT" || assignment.Length == 0 || org.Length == 0)
                        continue;

                    if (!dict.ContainsKey(assignment))
                        dict[assignment] = org;
                }
            }
        }
        catch { }
        return dict;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQuotes = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
        }

        fields.Add(sb.ToString());
        return fields;
    }

    public static string ExpandIpRange(string input)
    {
        var ips = new List<string>();
        var parts = input.Split(',', StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part)) continue;

            if (part.Contains('-'))
            {
                var lastDot = part.LastIndexOf('.');
                var prefix = part[..lastDot];
                var rangePart = part[(lastDot + 1)..];
                var range = rangePart.Split('-');
                if (range.Length == 2 &&
                    int.TryParse(range[0], out var start) &&
                    int.TryParse(range[1], out var end))
                {
                    for (var i = start; i <= end; i++)
                        ips.Add($"{prefix}.{i}");
                }
            }
            else if (part.Contains('/'))
            {
                ips.AddRange(ExpandCidr(part));
            }
            else
            {
                ips.Add(part.Trim());
            }
        }

        return string.Join(", ", ips);
    }

    private static List<string> ExpandCidr(string cidr)
    {
        var results = new List<string>();
        try
        {
            var parts = cidr.Split('/');
            var ip = IPAddress.Parse(parts[0]);
            var prefixLength = int.Parse(parts[1]);
            var ipBytes = ip.GetAddressBytes();
            var mask = uint.MaxValue << (32 - prefixLength);
            var network = BitConverter.ToUInt32(ipBytes.Reverse().ToArray(), 0) & mask;

            for (var i = 1u; i < ~mask; i++)
            {
                var hostIp = BitConverter.GetBytes(network | i);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(hostIp);
                results.Add(new IPAddress(hostIp).ToString());
            }
        }
        catch { }
        return results;
    }
}
