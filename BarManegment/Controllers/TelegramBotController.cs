using BarManegment.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System;
using System.Configuration;

namespace BarManegment.Controllers
{
    public class TelegramBotController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        [HttpPost]
        public async Task<ActionResult> Update(Update update)
        {
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                var message = update.Message;
                var chatId = message.Chat.Id;
                var text = message.Text;

                if (text.StartsWith("/start "))
                {
                    var token = text.Split(' ')[1];

                    // === بداية التعديل: البحث باستخدام "رقم الطلب" (Id) بدلاً من "رقم المستخدم" (UserId) ===
                    if (int.TryParse(token, out int applicationId))
                    {
                        // 1. البحث في ملفات المتدربين
                        var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.Id == applicationId);
                        if (graduateApp != null)
                        {
                            graduateApp.TelegramChatId = chatId;
                            await db.SaveChangesAsync();
                            await Services.TelegramService.SendMessageAsync(chatId, "تم ربط حسابك في بوابة الأعضاء بنجاح! يمكنك الآن استقبال الإشعارات.");
                            return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
                        }

                        // 2. البحث في طلبات الامتحان
                        var examApp = db.ExamApplications.FirstOrDefault(e => e.Id == applicationId);
                        if (examApp != null)
                        {
                            examApp.TelegramChatId = chatId;
                            await db.SaveChangesAsync();
                            await Services.TelegramService.SendMessageAsync(chatId, "تم ربط حسابك في بوابة الامتحانات بنجاح!");
                            return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
                        }
                    }
                    // === نهاية التعديل ===

                    await Services.TelegramService.SendMessageAsync(chatId, "الرمز الذي أدخلته غير صالح.");
                }
                else
                {
                    var botName = ConfigurationManager.AppSettings["TelegramBotName"];
                    await Services.TelegramService.SendMessageAsync(chatId, $"أهلاً بك في {botName}. يرجى استخدام الرابط الموجود في 'ملف المتدرب' أو 'صفحة التسجيل' لربط حسابك.");
                }
            }
            return new HttpStatusCodeResult(System.Net.HttpStatusCode.OK);
        }
    }
}

