using System.Windows;
using System.Windows.Input;
using IpScopePro.ViewModels;

namespace IpScopePro.Views;

public partial class FloatingAlertsWindow : Window
{
    private readonly FloatingAlertsViewModel _viewModel;

    public FloatingAlertsWindow(FloatingAlertsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
}
