namespace IpScopePro.Models;

public class PingStatistics
{
    public int Sent { get; set; }
    public int Received { get; set; }
    public int Lost { get; set; }
    public int Errors { get; set; }
    public List<double> RttHistory { get; set; } = new();

    public double MinRtt => RttHistory.Count > 0 ? RttHistory.Min() : 0;
    public double MaxRtt => RttHistory.Count > 0 ? RttHistory.Max() : 0;
    public double AvgRtt => RttHistory.Count > 0 ? RttHistory.Average() : 0;
    public double LatestRtt => RttHistory.Count > 0 ? RttHistory[^1] : 0;
    public double PacketLossPercent => Sent > 0 ? (double)Lost / Sent * 100.0 : 0;

    public void AddRtt(double rttMs)
    {
        RttHistory.Add(rttMs);
        if (RttHistory.Count > 1000)
            RttHistory.RemoveAt(0);
    }

    public void Reset()
    {
        Sent = 0;
        Received = 0;
        Lost = 0;
        Errors = 0;
        RttHistory.Clear();
    }

    public string GetSummary(string format = "Sent: {0} | Recv: {1} | Lost: {2} ({3:F1}%) | Avg: {4:F1}ms") =>
        string.Format(format, Sent, Received, Lost, PacketLossPercent, AvgRtt);

    public PingStatistics Copy() => new()
    {
        Sent = Sent,
        Received = Received,
        Lost = Lost,
        Errors = Errors,
        RttHistory = new List<double>(RttHistory)
    };
}
