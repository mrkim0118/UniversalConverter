using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using Velopack;

namespace UniversalConverter.Services;

public static class AppUpdateService
{
    private const string AppId = "ImageConverter";
    private const string UpdateBaseUrl = "https://tech.trendarchive.io/Utility";

    public static async Task TryCheckForUpdatesAsync()
    {
        try
        {
            var manager = new UpdateManager(GetFeedUrl());
            if (!manager.IsInstalled)
                return;

            if (manager.UpdatePendingRestart is not null)
            {
                if (ShouldRestartForPendingUpdate(GetVersionText(manager.UpdatePendingRestart.Version?.ToString())))
                    manager.ApplyUpdatesAndRestart(manager.UpdatePendingRestart);
                return;
            }

            var update = await manager.CheckForUpdatesAsync();
            if (update is null)
                return;

            var currentVersion = GetCurrentVersion();
            var targetVersion = GetVersionText(update.TargetFullRelease.Version?.ToString());
            if (!ShouldInstallUpdate(currentVersion, targetVersion))
                return;

            SetMainWindowBusy(true);
            var progressWindow = new UpdateDownloadProgressWindow(Application.Current?.MainWindow);
            progressWindow.Show();
            try
            {
                await manager.DownloadUpdatesAsync(update, progressWindow.ReportProgress);
            }
            catch (Exception ex)
            {
                ShowUpdateError(ex);
                return;
            }
            finally
            {
                progressWindow.CloseSafely();
                SetMainWindowBusy(false);
            }

            manager.ApplyUpdatesAndRestart(update);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Velopack update check failed: {ex}");
        }
    }

    private static string GetFeedUrl() => $"{UpdateBaseUrl}/{AppId}";

    private static string GetCurrentVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version;
        return version is null ? "1.0.5" : $"{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";
    }

    private static string GetVersionText(string? versionText) => string.IsNullOrWhiteSpace(versionText) ? GetCurrentVersion() : versionText;

    private static bool ShouldInstallUpdate(string currentVersion, string targetVersion)
    {
        var message = $"An update is available.\nCurrent version: {currentVersion}\nNew version: {targetVersion}\n\nWould you like to download and install it now?\nThe app will restart automatically after the update.";
        var title = "Update Available";
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes;
    }

    private static bool ShouldRestartForPendingUpdate(string targetVersion)
    {
        var message = $"An update is ready to be applied.\nCurrent version: {GetCurrentVersion()}\nNew version: {targetVersion}\n\nWould you like to restart now to finish the update?";
        var title = "Update Ready";
        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    private static void ShowUpdateError(Exception ex)
    {
        var isKorean = LanguageService.IsKorean;
        var message = isKorean
            ? $"\uC5C5\uB370\uC774\uD2B8\uB97C \uC9C4\uD589\uD558\uC9C0 \uBABB\uD588\uC2B5\uB2C8\uB2E4.\n{ex.Message}"
            : $"The update could not be completed.\n{ex.Message}";
        var title = isKorean ? "\uC5C5\uB370\uC774\uD2B8 \uC2E4\uD328" : "Update Failed";
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private static void SetMainWindowBusy(bool isBusy)
    {
        if (Application.Current?.MainWindow is not Window window)
            return;

        window.IsEnabled = !isBusy;
        window.Cursor = isBusy ? Cursors.Wait : null;
    }
}
