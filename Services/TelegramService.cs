using System.Net.Http;
using System.Net.Http.Json;

namespace IpScopePro.Services;

public class TelegramService
{
    private readonly HttpClient _http = new();

    public async Task<bool> SendMessage(string botToken, string chatId, string message)
    {
        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
            return false;

        try
        {
            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var payload = new { chat_id = chatId, text = message, parse_mode = "HTML" };
            var response = await _http.PostAsJsonAsync(url, payload);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool success, string error)> SendTestMessage(string botToken, string chatId)
    {
        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
            return (false, "Bot Token and Chat ID are required.");

        try
        {
            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var message = $"<b>IpScope Pro</b>\n{LocalizationService.Instance["TestTelegramMessage"]}";
            var payload = new { chat_id = chatId, text = message, parse_mode = "HTML" };
            var response = await _http.PostAsJsonAsync(url, payload);

            if (response.IsSuccessStatusCode)
                return (true, string.Empty);

            var errorBody = await response.Content.ReadAsStringAsync();
            return (false, $"Telegram API error: {response.StatusCode} - {errorBody}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
