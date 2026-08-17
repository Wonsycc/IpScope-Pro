using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using IpScopePro.Models;
using IpScopePro.Services;
using IpScopePro.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace IpScopePro.Views;

public partial class SettingsDialog : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly DataPersistenceService _dataPersistence;
    private readonly ApplicationOptions _options;
    private readonly InstallerService _installerService;
    private string _currentColorProperty = string.Empty;

    public SettingsDialog(SettingsViewModel viewModel, DataPersistenceService dataPersistence,
        ApplicationOptions options, InstallerService installerService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _dataPersistence = dataPersistence;
        _options = options;
        _installerService = installerService;
        DataContext = viewModel;

        Loaded += (s, e) =>
        {
            SmtpPasswordBox.Password = _viewModel.SmtpPassword ?? string.Empty;
        };
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SmtpPassword = SmtpPasswordBox.Password;
        _viewModel.SaveCommand.Execute(null);
        Close();
    }

    private async void TestEmail_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _viewModel.SmtpPassword = SmtpPasswordBox.Password;
            await _viewModel.TestEmailCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, LocalizationService.Instance["TestEmailTitle"],
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void TestTelegram_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.TestTelegramCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, LocalizationService.Instance["TestTelegramTitle"],
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement el && el.Tag is string prop)
        {
            _currentColorProperty = prop;
            ColorPopup.IsOpen = true;
            PopupHexInput.Text = GetColorValue(prop);
            PopupHexInput.Focus();
            PopupHexInput.SelectAll();
        }
    }

    private void PresetColor_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Border border && border.Background is SolidColorBrush brush)
        {
            var hex = brush.Color.ToString();
            SetColorValue(_currentColorProperty, hex);
        }
        ColorPopup.IsOpen = false;
    }

    private void PopupHexOk_Click(object sender, RoutedEventArgs e)
    {
        var hex = PopupHexInput.Text.Trim();
        if (!string.IsNullOrEmpty(hex))
        {
            if (!hex.StartsWith("#")) hex = "#" + hex;
            try { _ = (Color)ColorConverter.ConvertFromString(hex); SetColorValue(_currentColorProperty, hex); }
            catch { }
        }
        ColorPopup.IsOpen = false;
    }

    private string GetColorValue(string prop)
    {
        var pi = typeof(SettingsViewModel).GetProperty(prop);
        if (pi != null) return pi.GetValue(_viewModel) as string ?? "#808080";
        return "#808080";
    }

    private void SetColorValue(string prop, string hex)
    {
        var pi = typeof(SettingsViewModel).GetProperty(prop);
        if (pi != null) pi.SetValue(_viewModel, hex);
    }

    private void ExportSettings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Encrypted (*.enc)|*.enc|JSON (*.json)|*.json",
            DefaultExt = ".enc",
            FileName = "IpScopePro_Settings.enc"
        };
        if (dlg.ShowDialog() != true) return;

        var encrypt = dlg.FileName.EndsWith(".enc");
        var pw = encrypt ? PromptPassword(LocalizationService.Instance["ExportSettings"], LocalizationService.Instance["EnterEncryptionPassword"]) : null;
        if (encrypt && string.IsNullOrEmpty(pw)) return;

        try
        {
            SaveCurrentToOptions();
            var content = encrypt
                ? _dataPersistence.ExportSettingsEncrypted(_options, pw!)
                : _dataPersistence.ExportSettings(_options);
            System.IO.File.WriteAllText(dlg.FileName, content);
        }
        catch { MessageBox.Show(LocalizationService.Instance["ExportFailed"], LocalizationService.Instance["ErrorTitle"], MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void ImportSettings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Encrypted (*.enc)|*.enc|JSON (*.json)|*.json",
            DefaultExt = ".enc"
        };
        if (dlg.ShowDialog() != true) return;

        var isEncrypted = dlg.FileName.EndsWith(".enc");
        var pw = isEncrypted ? PromptPassword(LocalizationService.Instance["ImportSettings"], LocalizationService.Instance["EnterDecryptionPassword"]) : null;
        if (isEncrypted && string.IsNullOrEmpty(pw)) return;

        try
        {
            var content = System.IO.File.ReadAllText(dlg.FileName);
            ApplicationOptions? imported;
            if (isEncrypted)
                imported = _dataPersistence.ImportSettingsEncrypted(content, pw!);
            else
                imported = _dataPersistence.ImportSettings(content);

            if (imported == null)
            {
                MessageBox.Show(LocalizationService.Instance["InvalidFile"], LocalizationService.Instance["ErrorTitle"], MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            CopyOptions(imported, _options);
            _viewModel.ReloadFromOptions();
            SmtpPasswordBox.Password = _viewModel.SmtpPassword ?? string.Empty;
            _options.Save();
            LocalizationService.Instance.Language = _options.Language;
            MessageBox.Show(LocalizationService.Instance["ImportSuccess"], LocalizationService.Instance["ImportTitle"], MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch { MessageBox.Show(LocalizationService.Instance["ImportFailed"], LocalizationService.Instance["ErrorTitle"], MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void SaveCurrentToOptions()
    {
        _viewModel.SmtpPassword = SmtpPasswordBox.Password;
        _viewModel.SaveCommand.Execute(null);
    }

    private static void CopyOptions(ApplicationOptions from, ApplicationOptions to)
    {
        foreach (var prop in typeof(ApplicationOptions).GetProperties())
        {
            if (prop.CanWrite) prop.SetValue(to, prop.GetValue(from));
        }
    }

    private static string? PromptPassword(string title, string message)
    {
        var dialog = new PasswordPromptDialog(title, message);
        dialog.Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        return dialog.ShowDialog() == true ? dialog.Password : null;
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var installedExe = await _installerService.InstallAsync();
            MessageBox.Show(LocalizationService.Instance["InstallSuccess"], "IpScope Pro",
                MessageBoxButton.OK, MessageBoxImage.Information);
            InstallerService.LaunchInstalled(installedExe);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(LocalizationService.Instance["InstallFailed"], ex.Message),
                "IpScope Pro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Uninstall_Click(object sender, RoutedEventArgs e)
    {
<<<<<<< Updated upstream
=======
        var dialog = new UninstallDialog { Owner = this };
        if (dialog.ShowDialog() != true) return;

        var deleteData = dialog.DeleteData;

>>>>>>> Stashed changes
        try
        {
            _installerService.Uninstall();
            _viewModel.StartWithWindows = false;
            _viewModel.IsInstalled = false;
            MessageBox.Show(LocalizationService.Instance["UninstallSuccess"], "IpScope Pro",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(LocalizationService.Instance["UninstallFailed"], ex.Message),
                "IpScope Pro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
