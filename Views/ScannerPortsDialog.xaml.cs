using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using IpScopePro.Services;

namespace IpScopePro.Views;

public class ScannerPortOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public int Port { get; set; }
    public string Display => Port.ToString();

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class ScannerHostOption : INotifyPropertyChanged
{
    private bool _includePing = true;

    public string Ip { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public List<ScannerPortOption> Ports { get; set; } = new();

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Hostname) ? Ip : $"{Hostname} ({Ip})";

    public bool IncludePing
    {
        get => _includePing;
        set
        {
            if (_includePing == value) return;
            _includePing = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IncludePing)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class ScannerTransferItem
{
    public string Ip { get; set; } = string.Empty;
    public string Hostname { get; set; } = string.Empty;
    public int Port { get; set; }
}

public partial class ScannerPortsDialog : Window
{
    private readonly List<ScannerHostOption> _hosts;

    public List<ScannerTransferItem> SelectedItems { get; private set; } = new();

    public ScannerPortsDialog(IEnumerable<ScanResult> results)
    {
        InitializeComponent();

        _hosts = results.Select(r => new ScannerHostOption
        {
            Ip = r.Ip,
            Hostname = r.Hostname,
            IncludePing = true,
            Ports = r.OpenPorts.Select(p => new ScannerPortOption { Port = p }).ToList()
        }).ToList();

        HostsList.ItemsSource = _hosts;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var host in _hosts)
        {
            host.IncludePing = true;
            foreach (var port in host.Ports)
                port.IsSelected = true;
        }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var host in _hosts)
        {
            host.IncludePing = false;
            foreach (var port in host.Ports)
                port.IsSelected = false;
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        SelectedItems = new List<ScannerTransferItem>();

        foreach (var host in _hosts)
        {
            if (host.IncludePing)
                SelectedItems.Add(new ScannerTransferItem { Ip = host.Ip, Hostname = host.Hostname, Port = 0 });

            foreach (var port in host.Ports.Where(p => p.IsSelected))
                SelectedItems.Add(new ScannerTransferItem { Ip = host.Ip, Hostname = host.Hostname, Port = port.Port });
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
