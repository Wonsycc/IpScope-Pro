using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IpScopePro.Models;
using IpScopePro.Services;
using System.Collections.ObjectModel;

namespace IpScopePro.ViewModels;

public partial class FloatingAlertsViewModel : ObservableObject
{
    private readonly FloatingAlertsService _service;

    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private bool _isMinimized;
    [ObservableProperty] private double _windowLeft;
    [ObservableProperty] private double _windowTop;
    [ObservableProperty] private string _headerText = "IpScope Alerts";

    public ObservableCollection<StatusChangeLogEntry> Entries => _service.Entries;

    public FloatingAlertsViewModel(FloatingAlertsService service)
    {
        _service = service;
        _windowLeft = SystemParameters.PrimaryScreenWidth - 350;
        _windowTop = SystemParameters.PrimaryScreenHeight - 300;
    }

    [RelayCommand]
    public void ToggleMinimize()
    {
        IsMinimized = !IsMinimized;
        _service.IsMinimized = IsMinimized;
    }

    [RelayCommand]
    public void ToggleVisibility()
    {
        _service.IsVisible = !_service.IsVisible;
    }
}
