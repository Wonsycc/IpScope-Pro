using Microsoft.Extensions.DependencyInjection;
using IpScopePro.Models;
using IpScopePro.Services;
using IpScopePro.ViewModels;
using IpScopePro.Views;

namespace IpScopePro.Helpers;

public static class ServiceExtensions
{
    public static IServiceCollection AddIpScopeServices(this IServiceCollection services)
    {
        services.AddSingleton(ApplicationOptions.Load());
        services.AddSingleton(LocalizationService.Instance);
        services.AddSingleton<EncryptionService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<TelegramService>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton<ProbeManagerService>();
        services.AddSingleton<DataPersistenceService>();
        services.AddSingleton<FloatingAlertsService>();
        services.AddSingleton<NetworkScannerService>();
        services.AddSingleton<InstallerService>();

        services.AddSingleton<MainViewModel>();
        services.AddTransient<ScannerViewModel>();
        services.AddSingleton<FloatingAlertsViewModel>();
        services.AddTransient<SettingsViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddTransient<ScannerView>();
        services.AddTransient<FloatingAlertsWindow>();
        services.AddTransient<SettingsDialog>();

        return services;
    }
}
