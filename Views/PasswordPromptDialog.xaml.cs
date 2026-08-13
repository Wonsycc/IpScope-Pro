using System.Windows;
using System.Windows.Input;

namespace IpScopePro.Views;

public partial class PasswordPromptDialog : Window
{
    public string? Password { get; private set; }

    public PasswordPromptDialog(string title, string message)
    {
        InitializeComponent();
        HeaderTitle.Text = title;
        MessageText.Text = message;
        PwBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) Ok_Click(this, new RoutedEventArgs());
        };
        Loaded += (_, _) => { PwBox.Focus(); };
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Password = PwBox.Password;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
