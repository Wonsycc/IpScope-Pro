using System.Collections.ObjectModel;
using System.Text.Json;
using IpScopePro.Helpers;
using IpScopePro.Models;

namespace IpScopePro.Services;

public class ProbeManagerService
{
    private static string ProbesPath => Path.Combine(AppEnvironment.DataDirectory, "probes.json");

    public ObservableCollection<Probe> Probes { get; } = new();

    public void AddProbe(Probe probe)
    {
        if (Probes.Any(p => p.Id == probe.Id)) return;
        Probes.Add(probe);
    }

    public void RemoveProbe(Probe probe)
    {
        Probes.Remove(probe);
    }

    public void SwapProbes(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= Probes.Count ||
            indexB < 0 || indexB >= Probes.Count) return;
        Probes.Move(indexA, indexB);
    }

    public void SaveProbes()
    {
        if (!AppEnvironment.IsInstalled) return;

        try
        {
            var configs = Probes.Select(p => new ProbeConfig
            {
                Hostname = p.Hostname,
                Alias = p.Alias
            }).ToList();

            var dir = Path.GetDirectoryName(ProbesPath)!;
            Directory.CreateDirectory(dir);
            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(ProbesPath, JsonSerializer.Serialize(configs, opts));
        }
        catch { }
    }

    public void LoadProbes()
    {
        if (!AppEnvironment.IsInstalled) return;

        try
        {
            if (!File.Exists(ProbesPath)) return;
            var json = File.ReadAllText(ProbesPath);
            var configs = JsonSerializer.Deserialize<List<ProbeConfig>>(json);
            if (configs == null) return;

            Probes.Clear();
            foreach (var cfg in configs)
            {
                Probes.Add(new Probe
                {
                    Hostname = cfg.Hostname,
                    Alias = cfg.Alias,
                    Status = ProbeStatus.Indeterminate
                });
            }
        }
        catch { }
    }
}
