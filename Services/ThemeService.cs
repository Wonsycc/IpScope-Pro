using System.Windows;

namespace IpScopePro.Services;

public class ThemeService
{
    public bool IsDarkTheme
    {
        get => _isDark;
        set
        {
            if (_isDark == value) return;
            _isDark = value;
            ApplyThemeResources();
            try { OnThemeChanged?.Invoke(value); } catch { }
        }
    }
    private bool _isDark = true;

    public string SeedColor
    {
        get => _seedColor;
        set
        {
            _seedColor = value;
            try { OnThemeChanged?.Invoke(IsDarkTheme); } catch { }
        }
    }
    private string _seedColor = "#859900";

    public event Action<bool>? OnThemeChanged;

    public void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
    }

    private void ApplyThemeResources()
    {
        try
        {
            var app = Application.Current;
            if (app == null) return;

            var resources = app.Resources;
            if (resources is not ResourceDictionary rd || rd.MergedDictionaries.Count == 0)
                return;

            var newTheme = new ResourceDictionary
            {
                Source = new Uri(
                    IsDarkTheme ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml",
                    UriKind.Relative)
            };

            rd.MergedDictionaries.Clear();
            rd.MergedDictionaries.Add(newTheme);
        }
        catch { }
    }

    public static Uri GetStartupThemeUri(bool isDark) =>
        new(isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml", UriKind.Relative);
}
