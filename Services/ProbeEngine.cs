using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;
using IpScopePro.Models;

namespace IpScopePro.Services;

public class ProbeEngine : IDisposable
{
    private readonly Probe _probe;
    private readonly ApplicationOptions _globalOptions;
    private CancellationTokenSource? _cts;
    private Task? _pingLoop;
    private readonly SemaphoreSlim _semaphore;
    private readonly TaskCompletionSource _loopExited = new();

    public event Action<Probe, StatusChangeLogEntry>? OnStatusChanged;
    public event Action<Probe>? OnProbeUpdated;

    public ProbeEngine(Probe probe, ApplicationOptions globalOptions, SemaphoreSlim semaphore)
    {
        _probe = probe;
        _globalOptions = globalOptions;
        _semaphore = semaphore;
    }

    public void Start()
    {
        if (_probe.IsRunning) return;
        _probe.IsRunning = true;
        _cts = new CancellationTokenSource();
        _pingLoop = RunPingLoop(_cts.Token);
    }

    public async Task StopAsync()
    {
        _probe.IsRunning = false;
        _cts?.Cancel();
        if (_pingLoop != null)
        {
            try { await _pingLoop.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); }
            catch { }
        }
    }

    public void Stop()
    {
        _probe.IsRunning = false;
        _cts?.Cancel();
    }

    private async Task RunPingLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var acquired = await _semaphore.WaitAsync(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
                    if (!acquired)
                        continue;

                    try
                    {
                        await ExecutePing().ConfigureAwait(false);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { }

                var interval = _probe.Options.PingIntervalMs > 0
                    ? _probe.Options.PingIntervalMs
                    : _globalOptions.PingIntervalMs;

                try { await Task.Delay(interval, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }
        finally
        {
            _loopExited.TrySetResult();
        }
    }

    private async Task ExecutePing()
    {
        try
        {
            _probe.Statistics.Sent++;

            PingHistoryEntry entry;
            var timeout = _probe.Options.PingTimeoutMs > 0
                ? _probe.Options.PingTimeoutMs
                : _globalOptions.PingTimeoutMs;

            if (_probe.Type == ProbeType.Tcp)
                entry = await TcpPing(timeout).ConfigureAwait(false);
            else
                entry = await IcmpPing(timeout).ConfigureAwait(false);

            _probe.AddHistory(entry);

            switch (entry.Status)
            {
                case ProbeStatus.Up:
                    _probe.Statistics.Received++;
                    try { _probe.Statistics.AddRtt(entry.RttMs); } catch { }
                    _probe.LastResponseTime = DateTime.Now;
                    _probe.FailedPingCount = 0;

                    var threshold = _probe.Options.HighLatencyThresholdMs > 0
                        ? _probe.Options.HighLatencyThresholdMs
                        : _globalOptions.HighLatencyThresholdMs;

                    var rtt = _globalOptions.LatencyMode == LatencyMode.Average
                        ? _probe.Statistics.AvgRtt
                        : _probe.Statistics.LatestRtt;

                    if (rtt > threshold)
                        entry.Status = ProbeStatus.LatencyHigh;

                    break;

                case ProbeStatus.Down:
                    _probe.Statistics.Lost++;
                    _probe.FailedPingCount++;
                    break;

                case ProbeStatus.Error:
                    _probe.Statistics.Errors++;
                    break;
            }

            UpdateProbeStatus(entry.Status);
            try { OnProbeUpdated?.Invoke(_probe); } catch { }
        }
        catch (OperationCanceledException) { throw; }
        catch { }
    }

    private async Task<PingHistoryEntry> IcmpPing(int timeout)
    {
        try
        {
            var hostname = _probe.ParsedHostname;
            var safeTimeout = Math.Min(timeout, 5000);

            var reply = await Task.Run(() =>
            {
                using var ping = new Ping();
                return ping.Send(hostname, safeTimeout);
            }).ConfigureAwait(false);

            if (reply.Status == IPStatus.Success)
            {
                return new PingHistoryEntry
                {
                    Status = ProbeStatus.Up,
                    RttMs = reply.RoundtripTime,
                    Ttl = reply.Options?.Ttl ?? 0
                };
            }

            return new PingHistoryEntry
            {
                Status = ProbeStatus.Down,
                ErrorMessage = reply.Status.ToString()
            };
        }
        catch (PingException ex)
        {
            return new PingHistoryEntry
            {
                Status = ProbeStatus.Error,
                ErrorMessage = ex.InnerException?.Message ?? ex.Message
            };
        }
        catch (Exception ex)
        {
            return new PingHistoryEntry
            {
                Status = ProbeStatus.Error,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<PingHistoryEntry> TcpPing(int timeout)
    {
        var hostname = _probe.ParsedHostname;
        var port = _probe.Port > 0 ? _probe.Port : 80;
        var sw = Stopwatch.StartNew();
        try
        {
            var safeTimeout = Math.Min(timeout, 5000);

            await Task.Run(() =>
            {
                using var client = new TcpClient();
                var result = client.BeginConnect(hostname, port, null, null);
                if (!result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(safeTimeout)))
                {
                    client.Close();
                    throw new TimeoutException("Connection timed out");
                }
                client.EndConnect(result);
            }).ConfigureAwait(false);

            sw.Stop();
            return new PingHistoryEntry
            {
                Status = ProbeStatus.Up,
                RttMs = sw.Elapsed.TotalMilliseconds
            };
        }
        catch (TimeoutException)
        {
            return new PingHistoryEntry { Status = ProbeStatus.Down, ErrorMessage = "Connection timed out" };
        }
        catch (SocketException ex)
        {
            return new PingHistoryEntry { Status = ProbeStatus.Down, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new PingHistoryEntry { Status = ProbeStatus.Error, ErrorMessage = ex.Message };
        }
    }

    private void UpdateProbeStatus(ProbeStatus pingResult)
    {
        try
        {
            var oldStatus = _probe.Status;
            var failedThreshold = _probe.Options.FailedPingsBeforeDown > 0
                ? _probe.Options.FailedPingsBeforeDown
                : _globalOptions.FailedPingsBeforeDown;

            ProbeStatus newStatus;

            if (pingResult == ProbeStatus.Up)
            {
                newStatus = ProbeStatus.Up;
            }
            else if (pingResult == ProbeStatus.LatencyHigh)
            {
                newStatus = ProbeStatus.LatencyHigh;
            }
            else if (pingResult == ProbeStatus.Error)
            {
                newStatus = ProbeStatus.Error;
            }
            else
            {
                newStatus = _probe.FailedPingCount >= failedThreshold
                    ? ProbeStatus.Down
                    : ProbeStatus.Indeterminate;
            }

            if (oldStatus != newStatus)
            {
                _probe.PreviousStatus = oldStatus;
                _probe.Status = newStatus;
                _probe.RecordStatusChange(oldStatus, newStatus);

                var logEntry = _probe.StatusChanges.Count > 0
                    ? _probe.StatusChanges[^1]
                    : null;

                if (logEntry != null)
                    try { OnStatusChanged?.Invoke(_probe, logEntry); } catch { }
            }
        }
        catch { }
    }

    public void Dispose()
    {
        try
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
        catch { }
    }
}
