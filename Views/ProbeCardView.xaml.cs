using System.Windows;
using System.Windows.Controls;

namespace IpScopePro.Views;

public partial class ProbeCardView : UserControl
{
    private const double CompactWidth = 200;
    private const double CompactHeight = 90;
    private bool _isCompact;
    private bool _loaded;

    public ProbeCardView()
    {
        InitializeComponent();
        Loaded += (_, _) => { _loaded = true; CheckCompact(); };
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_loaded) CheckCompact();
    }

    private void CheckCompact()
    {
        var shouldBeCompact = ActualWidth > 0
            && (ActualWidth < CompactWidth || ActualHeight < CompactHeight);

        if (shouldBeCompact == _isCompact) return;
        _isCompact = shouldBeCompact;

        if (_isCompact)
        {
            HistorySection.Visibility = Visibility.Collapsed;
            StatsSection.Visibility = Visibility.Collapsed;
            InputSection.Visibility = Visibility.Collapsed;
            HeaderRow.Height = new GridLength(30);
            HistoryRow.Height = new GridLength(1, GridUnitType.Star);
            StatsRow.Height = new GridLength(0);
            InputRow.Height = new GridLength(0);
        }
        else
        {
            HistorySection.Visibility = Visibility.Visible;
            StatsSection.Visibility = Visibility.Visible;
            InputSection.Visibility = Visibility.Visible;
            HeaderRow.Height = new GridLength(30);
            HistoryRow.Height = new GridLength(1, GridUnitType.Star);
            StatsRow.Height = new GridLength(20);
            InputRow.Height = new GridLength(35);
        }
    }

    private void RemoveProbe_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ProbeViewModel vm)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            var mainVm = mainWindow?.DataContext as ViewModels.MainViewModel;
            mainVm?.RemoveProbeCommand.Execute(vm);
        }
    }
}
