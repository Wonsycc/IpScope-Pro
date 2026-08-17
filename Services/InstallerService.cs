using System.Diagnostics;
using Microsoft.Win32;
using IpScopePro.Helpers;

namespace IpScopePro.Services;

public class InstallerService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "IpScopePro";
    private const string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\IpScopePro";
    private const string AppPathsKeyPath = @"Software\Microsoft\Windows\CurrentVersion\App Paths\IpScopePro.exe";

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

<<<<<<< Updated upstream
        var installedExe = Path.Combine(AppEnvironment.InstallDir, Path.GetFileName(exe));
=======
        await Task.Run(() => CopyDirectory(AppContext.BaseDirectory, installDir)).ConfigureAwait(false);

        var installedExe = Path.Combine(installDir, Path.GetFileName(exe));
>>>>>>> Stashed changes

        using (var key = Registry.CurrentUser.CreateSubKey(AppEnvironment.RegistryRootKey))
            key.SetValue("InstallDir", AppEnvironment.InstallDir);

        CreateShortcut(installedExe,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft", "Windows", "Start Menu", "Programs", "IpScopePro.lnk"),
            "IpScope Pro");

        RegisterUninstall(installDir, installedExe);
        RegisterAppPaths(installDir, installedExe);

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

<<<<<<< Updated upstream
        try { Registry.CurrentUser.DeleteSubKey(AppEnvironment.RegistryRootKey, throwOnMissingSubKey: false); }
        catch { }
=======
        foreach (var shortcut in new[] { StartMenuShortcutPath, DesktopShortcutPath })
        {
            try
            {
                if (File.Exists(shortcut))
                    File.Delete(shortcut);
            }
            catch { }
        }

        try { Registry.CurrentUser.DeleteSubKeyTree(AppEnvironment.RegistryRootKey, throwOnMissingSubKey: false); }
        catch { }

        try { Registry.CurrentUser.DeleteSubKey(UninstallKeyPath, throwOnMissingSubKey: false); }
        catch { }

        try { Registry.CurrentUser.DeleteSubKey(AppPathsKeyPath, throwOnMissingSubKey: false); }
        catch { }

        if (!string.IsNullOrEmpty(installDir) && Directory.Exists(installDir))
        {
            DeleteDirectoryBestEffort(installDir);
            ScheduleDirectoryDeletion(installDir);
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

    private static void RegisterUninstall(string installDir, string installedExe)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(UninstallKeyPath);
            if (key == null) return;

            key.SetValue("DisplayName", "IpScope Pro");
            key.SetValue("DisplayVersion", AppVersion);
            key.SetValue("Publisher", "IpScope");
            key.SetValue("InstallLocation", installDir);
            key.SetValue("DisplayIcon", installedExe);
            key.SetValue("UninstallString", $"\"{installedExe}\" --uninstall");
            key.SetValue("NoModify", 1);
            key.SetValue("NoRepair", 1);
        }
        catch { }
    }

    private static void RegisterAppPaths(string installDir, string installedExe)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(AppPathsKeyPath);
            if (key == null) return;

            key.SetValue("", installedExe);
            key.SetValue("Path", installDir);
        }
        catch { }
    }

    private static void ScheduleDirectoryDeletion(string dir)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c ping 127.0.0.1 -n 3 >nul & rmdir /s /q \"{dir}\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(psi);
        }
        catch { }
    }

    private static string AppVersion
    {
        get
        {
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return v is null ? "1.0.0" : $"{v.Major}.{v.Minor}.{v.Build}";
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
>>>>>>> Stashed changes
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

<<<<<<< Updated upstream
=======
    public static void RelaunchElevated(string targetDir, bool desktopShortcut, bool startMenuShortcut)
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe)) return;

        var args = $"--install --install-dir \"{targetDir.Replace("\"", "\\\"")}\" " +
                   $"--desktop-shortcut {desktopShortcut.ToString().ToLowerInvariant()} " +
                   $"--start-menu-shortcut {startMenuShortcut.ToString().ToLowerInvariant()}";

        var psi = new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = true,
            Verb = "runas",
            Arguments = args
        };

        try { Process.Start(psi); }
        catch (System.ComponentModel.Win32Exception) { }
        catch { }
    }

>>>>>>> Stashed changes
    public static void LaunchInstalled(string installedExe)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = installedExe,
            UseShellExecute = true
        });
    }
}
