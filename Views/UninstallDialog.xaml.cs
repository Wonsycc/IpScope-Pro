using System.Windows;
using System.Windows.Input;

namespace IpScopePro.Views;

public partial class UninstallDialog : Window
{
    public bool DeleteData => DeleteDataOption.IsChecked == true;

    public UninstallDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => KeepDataOption.Focus();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
