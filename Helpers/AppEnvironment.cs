using Microsoft.Win32;

namespace IpScopePro.Helpers;

public static class AppEnvironment
{
    private const string RegistryRoot = @"Software\IpScopePro";

    public static string InstallDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Programs", "IpScopePro");

    public static bool IsInstalled
    {
        get
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegistryRoot);
                var installDir = key?.GetValue("InstallDir") as string;
                return !string.IsNullOrEmpty(installDir) &&
                       string.Equals(Normalize(AppContext.BaseDirectory), Normalize(installDir),
                           StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
    }

    public static bool IsPortable => !IsInstalled;

    public static string DataDirectory => IsInstalled
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IpScopePro")
        : AppContext.BaseDirectory;

    public static string RegistryRootKey => RegistryRoot;

    private static string Normalize(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
}
