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
        if (DataContext is ViewModels.ScannerViewModel scannerVm)
        {
            var selected = ResultsGrid.SelectedItems.Cast<Services.ScanResult>();

            var mainWindow = Window.GetWindow(this) as MainWindow;
            var mainVm = mainWindow?.DataContext as ViewModels.MainViewModel;

            foreach (var result in selected)
            {
                if (result.IsAlive && mainVm != null)
                {
                    mainVm.AddProbeFromScanner(result.Ip, result.Hostname);
                }
            }
        }
    }
}
