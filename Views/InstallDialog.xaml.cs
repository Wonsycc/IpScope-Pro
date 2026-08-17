using System.Windows;
using System.Windows.Input;
using IpScopePro.Helpers;
using IpScopePro.Services;

namespace IpScopePro.Views;

public partial class InstallDialog : Window
{
    public string InstallDirectory => InstallPathBox.Text.Trim();
    public bool CreateDesktopShortcut => DesktopShortcutBox.IsChecked == true;
    public bool CreateStartMenuShortcut => StartMenuShortcutBox.IsChecked == true;

    public InstallDialog()
    {
        InitializeComponent();
        InstallPathBox.Text = AppEnvironment.DefaultInstallDir;
        Loaded += (_, _) => InstallPathBox.Focus();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = LocalizationService.Instance["InstallLocation"],
            InitialDirectory = string.IsNullOrWhiteSpace(InstallPathBox.Text)
                ? AppEnvironment.DefaultInstallDir
                : InstallPathBox.Text
        };
        if (dlg.ShowDialog() == true)
            InstallPathBox.Text = dlg.FolderName;
    }

    private void Install_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(InstallPathBox.Text))
        {
            MessageBox.Show(LocalizationService.Instance["InstallPathRequired"], "IpScope Pro",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
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
