using System.Windows;
using System.Windows.Input;

namespace IpScopePro.Views;

public partial class BatchInputDialog : Window
{
    public string? Addresses { get; private set; }

    public BatchInputDialog()
    {
        InitializeComponent();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        Addresses = AddressesBox.Text;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
