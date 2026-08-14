using System.Diagnostics;
using Microsoft.Win32;
using IpScopePro.Helpers;

namespace IpScopePro.Services;

public class InstallerService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "IpScopePro";

    public bool IsStartWithWindows()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return key?.GetValue(RunValueName) != null;
        }
        catch { return false; }
    }

    public void SetStartWithWindows(bool enabled)
    {
        if (!AppEnvironment.IsInstalled) return;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (key == null) return;

            if (enabled)
            {
                var exe = Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "IpScopePro.exe");
                key.SetValue(RunValueName, $"\"{exe}\" --autostart");
            }
            else
            {
                key.DeleteValue(RunValueName, throwOnMissingValue: false);
            }
        }
        catch { }
    }

    public async Task<string> InstallAsync()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
            throw new InvalidOperationException("Cannot determine current executable path.");

        if (AppEnvironment.IsInstalled)
            return exe;

        Directory.CreateDirectory(AppEnvironment.InstallDir);
        await Task.Run(() => CopyDirectory(AppContext.BaseDirectory, AppEnvironment.InstallDir));

        var installedExe = Path.Combine(AppEnvironment.InstallDir, Path.GetFileName(exe));

        using (var key = Registry.CurrentUser.CreateSubKey(AppEnvironment.RegistryRootKey))
            key.SetValue("InstallDir", AppEnvironment.InstallDir);

        CreateShortcut(installedExe,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "Windows", "Start Menu", "Programs", "IpScopePro.lnk"),
            "IpScope Pro");

        return installedExe;
    }

    public void Uninstall()
    {
        SetStartWithWindows(false);

        try
        {
            var shortcut = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "Windows", "Start Menu", "Programs", "IpScopePro.lnk");
            if (File.Exists(shortcut))
                File.Delete(shortcut);
        }
        catch { }

        try { Registry.CurrentUser.DeleteSubKey(AppEnvironment.RegistryRootKey, throwOnMissingSubKey: false); }
        catch { }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            try
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
            }
            catch { }
        }
        foreach (var dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
    }

    private static void CreateShortcut(string target, string linkPath, string description)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(linkPath);
            shortcut.TargetPath = target;
            shortcut.WorkingDirectory = Path.GetDirectoryName(target);
            shortcut.Description = description;
            shortcut.Save();
        }
        catch { }
    }

    public static void LaunchInstalled(string installedExe)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = installedExe,
            UseShellExecute = true
        });
    }
}
