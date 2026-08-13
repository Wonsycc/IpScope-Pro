using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IpScopePro.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

namespace IpScopePro.ViewModels;

public partial class ScannerViewModel : ObservableObject
{
    private readonly NetworkScannerService _scanner;

    public IReadOnlyList<string> ScanModes => new[]
    {
        LocalizationService.Instance["ScanModeFast"],
        LocalizationService.Instance["ScanModeExhaustive"]
    };

    [ObservableProperty] private string _ipRangeInput = "192.168.1.1-254";
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _scanPorts;
    [ObservableProperty] private string _portsInput = "1-1024";
    [ObservableProperty] private ScanMode _selectedScanMode = ScanMode.Fast;
    [ObservableProperty] private int _selectedScanModeIndex;
    [ObservableProperty] private int _progressCurrent;
    [ObservableProperty] private int _progressTotal;
    [ObservableProperty] private double _progressPercent;
    [ObservableProperty] private string _statusText = LocalizationService.Instance["StatusReady"];
    [ObservableProperty] private bool _isExportEnabled;
    [ObservableProperty] private bool _showAliveOnly = true;

    public ObservableCollection<ScanResult> Results { get; } = new();

    public ICollectionView FilteredResults { get; }

    public ScannerViewModel(NetworkScannerService scanner)
    {
        _scanner = scanner;
        _scanner.OnHostFound += HandleHostFound;
        _scanner.OnProgress += HandleProgress;

        FilteredResults = CollectionViewSource.GetDefaultView(Results);
        FilteredResults.Filter = FilterResult;

        LocalizationService.Instance.LanguageChanged += _ =>
        {
            OnPropertyChanged(nameof(ScanModes));
            if (!IsScanning)
                StatusText = LocalizationService.Instance["StatusReady"];
        };
    }

    partial void OnShowAliveOnlyChanged(bool value)
    {
        FilteredResults.Refresh();
    }

    private bool FilterResult(object obj)
    {
        if (obj is not ScanResult result) return true;
        return !ShowAliveOnly || result.IsAlive;
    }

    partial void OnSelectedScanModeIndexChanged(int value)
    {
        SelectedScanMode = value switch
        {
            0 => ScanMode.Fast,
            1 => ScanMode.Exhaustive,
            _ => ScanMode.Fast
        };
    }

    private void HandleHostFound(ScanResult result)
    {
        var app = Application.Current;
        if (app == null) return;

        try
        {
            app.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (!IsScanning) return;
                    Results.Add(result);
                }
                catch { }
            });
        }
        catch { }
    }

    private void HandleProgress(int current, int total)
    {
        var app = Application.Current;
        if (app == null) return;

        try
        {
            app.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (!IsScanning) return;
                    ProgressCurrent = current;
                    ProgressTotal = total;
                    ProgressPercent = total > 0 ? (double)current / total * 100 : 0;
                    StatusText = LocalizationService.Instance["StatusScanning"];
                }
                catch { }
            });
        }
        catch { }
    }

    [RelayCommand]
    public async Task StartScan()
    {
        if (string.IsNullOrWhiteSpace(IpRangeInput)) return;

        Results.Clear();
        IsScanning = true;
        IsExportEnabled = false;
        StatusText = LocalizationService.Instance["StatusExpanding"];

        try
        {
            var expanded = NetworkScannerService.ExpandIpRange(IpRangeInput);
            var ips = expanded.Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList();

            if (ips.Count == 0)
            {
                StatusText = LocalizationService.Instance["StatusNoIps"];
                return;
            }

            var ports = new List<int>();
            if (ScanPorts)
            {
                ports = NetworkScannerService.ParsePortList(PortsInput);
                if (ports.Count == 0)
                {
                    StatusText = LocalizationService.Instance["StatusNoPorts"];
                    return;
                }
            }

            var scanResults = await _scanner.ScanNetwork(ips, ScanPorts, ports, SelectedScanMode);

            var aliveCount = scanResults.Count(r => r.IsAlive);
            StatusText = string.Format(LocalizationService.Instance["ScanComplete"], aliveCount, scanResults.Count);
            IsExportEnabled = true;
        }
        catch (Exception ex)
        {
            StatusText = string.Format(LocalizationService.Instance["ScanError"], ex.Message);
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    public void CancelScan()
    {
        _scanner.Cancel();
        IsScanning = false;
        StatusText = LocalizationService.Instance["ScanCancelled"];
    }

    [RelayCommand]
    public void ExportCsv()
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                DefaultExt = ".csv"
            };

            if (dlg.ShowDialog() == true)
            {
                using var writer = new StreamWriter(dlg.FileName);
                writer.WriteLine(string.Join(",",
                    LocalizationService.Instance["ColIp"],
                    LocalizationService.Instance["ColHostname"],
                    LocalizationService.Instance["ColMacAddress"],
                    LocalizationService.Instance["ColVendor"],
                    LocalizationService.Instance["ColOpenPorts"]));
                foreach (var r in Results.Where(r => r.IsAlive))
                {
                    writer.WriteLine(
                        $"\"{r.Ip}\",\"{r.Hostname}\",\"{r.MacAddress}\",\"{r.Vendor}\"," +
                        $"\"{string.Join(";", r.OpenPorts)}\"");
                }
            }
        }
        catch { }
    }

    [RelayCommand]
    public void ExportExcel()
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dlg.ShowDialog() == true)
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var ws = workbook.Worksheets.Add(LocalizationService.Instance["ScanResults"]);
                ws.Cell(1, 1).Value = LocalizationService.Instance["ColIp"];
                ws.Cell(1, 2).Value = LocalizationService.Instance["ColHostname"];
                ws.Cell(1, 3).Value = LocalizationService.Instance["ColMacAddress"];
                ws.Cell(1, 4).Value = LocalizationService.Instance["ColVendor"];
                ws.Cell(1, 5).Value = LocalizationService.Instance["ColOpenPorts"];

                var row = 2;
                foreach (var r in Results.Where(r => r.IsAlive))
                {
                    ws.Cell(row, 1).Value = r.Ip;
                    ws.Cell(row, 2).Value = r.Hostname;
                    ws.Cell(row, 3).Value = r.MacAddress;
                    ws.Cell(row, 4).Value = r.Vendor;
                    ws.Cell(row, 5).Value = string.Join("; ", r.OpenPorts);
                    row++;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(dlg.FileName);
            }
        }
        catch { }
    }

    [RelayCommand]
    public void ExportJson()
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json"
            };

            if (dlg.ShowDialog() == true)
            {
                var alive = Results.Where(r => r.IsAlive)
                    .Select(r => new
                    {
                        IP = r.Ip,
                        Hostname = r.Hostname,
                        MacAddress = r.MacAddress,
                        Vendor = r.Vendor,
                        OpenPorts = string.Join("; ", r.OpenPorts)
                    });

                var json = System.Text.Json.JsonSerializer.Serialize(alive,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dlg.FileName, json);
            }
        }
        catch { }
    }
}
