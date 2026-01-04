using System;
using System.Configuration;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace BarManegment.Services
{
    public static class TelegramService
    {
        private static readonly string botToken = ConfigurationManager.AppSettings["TelegramBotToken"];
        private static readonly TelegramBotClient botClient = new TelegramBotClient(botToken);

        public static async Task SendMessageAsync(long chatId, string message)
        {
            try
            {
                // === بداية التعديل: استخدام الصيغة الحديثة (بدون Methods) ===
                await botClient.SendTextMessageAsync(
                    chatId: new ChatId(chatId),
                    text: message
                );
                // === نهاية التعديل ===
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Telegram Send Error: {ex.Message}");
            }
        }

        public static async Task SetWebhookAsync(string webhookUrl)
        {
            try
            {
                // === بداية التعديل: استخدام الصيغة الحديثة (بدون Methods) ===
                await botClient.SetWebhookAsync(
                    url: webhookUrl
                );
                // === نهاية التعديل ===
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Telegram Webhook Error: {ex.Message}");
            }
        }
    }
}

