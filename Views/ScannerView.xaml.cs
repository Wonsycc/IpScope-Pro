using System.Windows;
using System.Windows.Controls;

namespace IpScopePro.Views;

public partial class ScannerView : UserControl
{
    public ScannerView()
    {
        InitializeComponent();
    }

    private void TransferToProbes_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModels.ScannerViewModel scannerVm)
            return;

        var selected = ResultsGrid.SelectedItems.Cast<Services.ScanResult>()
            .Where(r => r.IsAlive)
            .ToList();

        if (selected.Count == 0) return;

        var mainWindow = Window.GetWindow(this) as MainWindow;
        var mainVm = mainWindow?.DataContext as ViewModels.MainViewModel;
        if (mainVm == null) return;

        var dialog = new ScannerPortsDialog(selected)
        {
            Owner = mainWindow,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var item in dialog.SelectedItems)
                mainVm.AddProbeFromScanner(item.Ip, item.Hostname, item.Port);
        }
    }
}
