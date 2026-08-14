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

    public async Task<string> InstallAsync(string targetDir, bool desktopShortcut, bool startMenuShortcut)
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
            throw new InvalidOperationException("Cannot determine current executable path.");

        var installDir = Path.TrimEndingDirectorySeparator(Path.GetFullPath(targetDir));

        if (AppEnvironment.IsInstalled)
            return Path.Combine(installDir, Path.GetFileName(exe));

        Directory.CreateDirectory(installDir);
        EnsureWritable(installDir);

        await Task.Run(() => CopyDirectory(AppContext.BaseDirectory, installDir));

        var installedExe = Path.Combine(installDir, Path.GetFileName(exe));

        using (var key = Registry.CurrentUser.CreateSubKey(AppEnvironment.RegistryRootKey))
            key.SetValue("InstallDir", installDir);

        if (startMenuShortcut)
            CreateShortcut(installedExe, StartMenuShortcutPath, "IpScope Pro");

        if (desktopShortcut)
            CreateShortcut(installedExe, DesktopShortcutPath, "IpScope Pro");

        return installedExe;
    }

    public void Uninstall(bool deleteData)
    {
        SetStartWithWindows(false);

        string? installDir = null;
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AppEnvironment.RegistryRootKey);
            installDir = key?.GetValue("InstallDir") as string;
        }
        catch { }

        foreach (var shortcut in new[] { StartMenuShortcutPath, DesktopShortcutPath })
        {
            try
            {
                if (File.Exists(shortcut))
                    File.Delete(shortcut);
            }
            catch { }
        }

        try { Registry.CurrentUser.DeleteSubKey(AppEnvironment.RegistryRootKey, throwOnMissingSubKey: false); }
        catch { }

        if (!string.IsNullOrEmpty(installDir))
        {
            try
            {
                if (Directory.Exists(installDir))
                    DeleteDirectoryBestEffort(installDir);
            }
            catch { }
        }

        if (deleteData)
        {
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IpScopePro");
            try
            {
                if (Directory.Exists(dataDir))
                    DeleteDirectoryBestEffort(dataDir);
            }
            catch { }
        }
    }

    private static void DeleteDirectoryBestEffort(string dir)
    {
        foreach (var file in Directory.GetFiles(dir))
        {
            try { File.Delete(file); } catch { }
        }
        foreach (var sub in Directory.GetDirectories(dir))
        {
            try { DeleteDirectoryBestEffort(sub); } catch { }
        }
        try { Directory.Delete(dir, false); } catch { }
    }

    private static string StartMenuShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Programs), "IpScopePro.lnk");

    private static string DesktopShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "IpScopePro.lnk");

    private static void EnsureWritable(string dir)
    {
        var test = Path.Combine(dir, ".IpScopeProWriteTest");
        File.WriteAllText(test, string.Empty);
        File.Delete(test);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
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
            shortcut.IconLocation = target + ",0";
            shortcut.Save();
        }
        catch { }
    }

    public static void RelaunchElevated(string targetDir, bool desktopShortcut, bool startMenuShortcut)
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe)) return;

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = true,
            Verb = "runas"
        };
        psi.ArgumentList.Add("--install");
        psi.ArgumentList.Add("--install-dir");
        psi.ArgumentList.Add(targetDir);
        psi.ArgumentList.Add("--desktop-shortcut");
        psi.ArgumentList.Add(desktopShortcut ? "true" : "false");
        psi.ArgumentList.Add("--start-menu-shortcut");
        psi.ArgumentList.Add(startMenuShortcut ? "true" : "false");

        try { Process.Start(psi); }
        catch (System.ComponentModel.Win32Exception) { }
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
