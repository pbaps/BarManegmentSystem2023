using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace BarManegment.Services
{
    public class TelegramService
    {
        private static readonly string BotToken = WebConfigurationManager.AppSettings["TelegramBotToken"];

        public static async Task<bool> SendMessageAsync(long chatId, string message)
        {
            if (string.IsNullOrEmpty(BotToken) || chatId == 0) return false;

            try
            {
                string url = $"https://api.telegram.org/bot{BotToken}/sendMessage?chat_id={chatId}&text={message}&parse_mode=Markdown";

                using (var client = new HttpClient())
                {
                    var response = await client.GetAsync(url);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}