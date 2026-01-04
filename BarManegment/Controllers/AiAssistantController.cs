using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BarManegment.Controllers
{
    public class AiAssistantController : Controller
    {
        private static readonly HttpClient client = new HttpClient();

        [HttpPost]
        public async Task<ActionResult> SendMessage(string userMessage)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string apiKey = ConfigurationManager.AppSettings["GeminiApiKey"];

            if (string.IsNullOrEmpty(apiKey))
            {
                return Json(new { success = false, response = "مفتاح API غير موجود." });
            }

            // هنا نضع "عقل" النظام ومعلوماته عن الإجراءات
            // يمكنك إضافة أو تعديل أي معلومة هنا ليعرفها الذكاء الاصطناعي
            string systemInstruction = @"
                أنت المساعد الذكي الرسمي لنظام إدارة نقابة المحامين.
                مهمتك: توجيه المستخدمين، شرح الخطوات، وتوضيح إجراءات النظام (Workflow) فقط.
                
                معلومات النظام الأساسية التي يجب أن تعتمد عليها في إجاباتك:

                1. للخريجين الجدد (Graduates):
                   - الخطوة الأولى: إنشاء حساب خريج جديد من الشاشة الرئيسية.
                   - الخطوة الثانية: تعبئة طلب الانتساب وإرفاق المستندات (صورة الهوية، الشهادة الجامعية، شهادة عدم محكومية).
                   - الخطوة الثالثة: انتظار تدقيق الموظف للطلب (حالة الطلب تكون 'قيد المراجعة').
                   - عند قبول الطلب، يظهر للخريج إشعار لدفع رسوم امتحان المزاولة.
                   - بعد الدفع ورفع الوصل، يمكنه حجز مقعد في الامتحان القادم.

                2. للمتدربين (Trainees):
                   - يصبح الخريج 'متدرباً' تلقائياً بعد اجتياز امتحان المزاولة بنجاح.
                   - يجب على المتدرب اختيار 'محامي مدرب' مزاول من القائمة المعتمدة في النظام.
                   - مدة التدريب هي سنتان، يجب خلالها تقديم تقرير تدريب كل 6 أشهر عبر البوابة.
                   - في حال تأخر التقرير، يرسل النظام تنبيهاً ويتم إيقاف الاحتساب مؤقتاً.

                3. الأمور المالية (Financials):
                   - جميع الدفعات تتم عبر 'سندات قبض' (Vouchers) يتم توليدها من النظام.
                   - يجب على المستخدم طباعة السند والذهاب للبنك للدفع، ثم تصوير الوصل ورفعه على النظام لتفعيل الخدمة.
                
                4. الدعم الفني:
                   - في حال واجه المستخدم مشكلة تقنية (مثل خطأ 404 أو تعليق)، انصحه بتحديث الصفحة أو الاتصال بمسؤول النظام.

                تعليمات الإجابة:
                - أجب دائماً باللغة العربية.
                - كن مباشراً واستخدم نقاطاً مرقمة عند شرح الخطوات.
                - لا تحاول تأليف إجراءات غير موجودة في النص أعلاه.
                - إذا سأل المستخدم عن بيانات شخصية (مثل: هل اسمي موجود؟)، اعتذر بلطف وقل أنك تجيب عن الإجراءات فقط.
            ";

            // استخدام الموديل السريع والذكي 2.0
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

            string finalPrompt = $"{systemInstruction}\n\nسؤال المستخدم: {userMessage}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = finalPrompt } } }
                }
            };

            try
            {
                string jsonBody = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                string responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = JObject.Parse(responseString);
                    string aiResponse = jsonResponse["candidates"][0]["content"]["parts"][0]["text"].ToString();

                    // تنسيق بسيط للنص
                    aiResponse = aiResponse.Replace("\n", "<br>").Replace("**", "<b>").Replace("*", "<li>");

                    return Json(new { success = true, response = aiResponse });
                }
                else
                {
                    return Json(new { success = false, response = "عذراً، حدث ضغط على الخادم. حاول مرة أخرى." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, response = "حدث خطأ: " + ex.Message });
            }
        }
    }
}