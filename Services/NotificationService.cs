using System.Collections.Concurrent;
using IpScopePro.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace IpScopePro.Services;

public class NotificationService
{
    private readonly ApplicationOptions _options;
    private readonly TelegramService _telegram;
    private readonly ConcurrentDictionary<string, DateTime> _cooldowns = new();

    public event Action<StatusChangeLogEntry>? OnPopupNotification;
    public event Action<StatusChangeLogEntry>? OnWindowsNotification;
    public event Action<string>? OnAudioAlert;
    public event Action<StatusChangeLogEntry>? OnLogEntry;

    public NotificationService(ApplicationOptions options, TelegramService telegram)
    {
        _options = options;
        _telegram = telegram;
    }

    public async Task HandleStatusChange(Probe probe, StatusChangeLogEntry entry)
    {
        try
        {
            await HandleStatusChangeCore(probe, entry);
        }
        catch { }
    }

    private async Task HandleStatusChangeCore(Probe probe, StatusChangeLogEntry entry)
    {
        LogToFile(entry);

        if (!_options.NotificationsEnabled)
            return;

        if (entry.NewStatus == ProbeStatus.Inactive)
            return;

        if ((entry.OldStatus == ProbeStatus.Inactive || entry.OldStatus == ProbeStatus.Indeterminate)
            && entry.NewStatus == ProbeStatus.Up)
        {
            return;
        }

        if (entry.NewStatus == ProbeStatus.Down)
        {
            await SendIfEnabled(probe, entry,
                probe.Options.PopupOnDown, probe.Options.EmailOnDown,
                probe.Options.TelegramOnDown, probe.Options.AudioOnDown);
        }
        else if (entry.NewStatus == ProbeStatus.Up)
        {
            await SendIfEnabled(probe, entry,
                probe.Options.PopupOnUp, probe.Options.EmailOnUp,
                probe.Options.TelegramOnUp, probe.Options.AudioOnUp);
        }
        else if (entry.NewStatus == ProbeStatus.Error)
        {
            await SendIfEnabled(probe, entry,
                probe.Options.PopupOnError, probe.Options.EmailOnError,
                probe.Options.TelegramOnError, probe.Options.AudioOnError);
        }
    }

    private async Task SendIfEnabled(Probe probe, StatusChangeLogEntry entry,
        bool popup, bool email, bool telegram, bool audio)
    {
        if (popup && _options.PopupOption != PopupNotificationOption.None)
        {
            if (!ShouldCooldown(probe.Hostname, "windows", _options.WindowsCooldownSeconds))
            {
                try { OnPopupNotification?.Invoke(entry); } catch { }
                if (_options.WindowsNotificationsEnabled)
                    try { OnWindowsNotification?.Invoke(entry); } catch { }
            }
        }

        if (email && _options.EmailEnabled)
        {
            if (!ShouldCooldown(probe.Hostname, "email", _options.EmailCooldownSeconds))
                await SendEmail(entry);
        }

        if (telegram && _options.TelegramEnabled)
        {
            if (!ShouldCooldown(probe.Hostname, "telegram", _options.TelegramCooldownSeconds))
            {
                await _telegram.SendMessage(
                    _options.TelegramBotToken,
                    _options.TelegramChatId,
                    $"<b>{entry.Title}</b>\n{entry.Body}");
            }
        }

        if (audio && _options.AudioEnabled)
        {
            if (!ShouldCooldown(probe.Hostname, "audio", _options.AudioCooldownSeconds))
                try { OnAudioAlert?.Invoke(_options.AudioFilePath); } catch { }
        }
    }

    private bool ShouldCooldown(string hostname, string channel, int cooldownSeconds)
    {
        var key = $"{hostname}_{channel}";
        if (_cooldowns.TryGetValue(key, out var last) &&
            (DateTime.Now - last).TotalSeconds < cooldownSeconds)
            return true;

        _cooldowns[key] = DateTime.Now;
        return false;
    }

    private async Task SendEmail(StatusChangeLogEntry entry)
    {
        await SendEmailInternal(
            _options.SmtpServer, _options.SmtpPort, _options.SmtpUseSsl,
            _options.SmtpUsername, _options.SmtpPassword,
            _options.EmailFrom, _options.EmailTo,
            $"[IpScope] {entry.Title}",
            $"<h3>{entry.Title}</h3><p>{entry.Body}</p><p>{LocalizationService.Instance["EmailTimestamp"]}: {entry.Timestamp:yyyy-MM-dd HH:mm:ss}</p>");
    }

    public async Task<(bool success, string error)> SendTestEmail(
        string smtpServer, int smtpPort, bool useSsl,
        string username, string password, string from, string to)
    {
        return await SendEmailInternal(
            smtpServer, smtpPort, useSsl,
            username, password, from, to,
            LocalizationService.Instance["TestEmailSubject"],
            $"<h3>IpScope Pro</h3><p>{LocalizationService.Instance["TestEmailBody"]}</p>");
    }

    private async Task<(bool success, string error)> SendEmailInternal(
        string smtpServer, int smtpPort, bool useSsl,
        string username, string password, string from, string to,
        string subject, string body)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(from));
            foreach (var addr in to.Split(',', StringSplitOptions.RemoveEmptyEntries))
                message.To.Add(MailboxAddress.Parse(addr.Trim()));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

            using var client = new SmtpClient();
            client.Timeout = 15000;

            var options = useSsl
                ? (smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
                : SecureSocketOptions.None;

            await client.ConnectAsync(smtpServer, smtpPort, options);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private void LogToFile(StatusChangeLogEntry entry)
    {
        if (!_options.LogToFile) return;
        try { OnLogEntry?.Invoke(entry); } catch { }
        WriteLogEntry(entry);
    }

    private void WriteLogEntry(StatusChangeLogEntry entry)
    {
        try
        {
            Directory.CreateDirectory(_options.LogDirectory);
            var logFile = Path.Combine(_options.LogDirectory,
                $"ipscope_{DateTime.Now:yyyy-MM-dd}.log");
            var line = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] {entry.Hostname} ({entry.Alias}): {entry.OldStatus} -> {entry.NewStatus}";
            File.AppendAllText(logFile, line + Environment.NewLine);
        }
        catch { }
    }
}
