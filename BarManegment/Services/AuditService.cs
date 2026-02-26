using BarManegment.Models;
using System;
using System.Web;

namespace BarManegment.Services
{
    public static class AuditService
    {
        /// <summary>
        /// يسجل إجراء مهماً في قاعدة البيانات.
        /// </summary>
        /// <param name="action">اسم الإجراء (e.g., "Create", "Login").</param>
        /// <param name="controller">اسم المتحكم أو المصدر (e.g., "Accounting").</param>
        /// <param name="details">تفاصيل إضافية عن الحدث.</param>
        /// <param name="explicitUserId">معرف المستخدم (اختياري، إذا لم يتم تمريره سيؤخذ من الجلسة).</param>
        public static void LogAction(string action, string controller, string details, int? explicitUserId = null)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    // الأولوية للمعرف الممرر، ثم للجلسة الحالية
                    var userId = explicitUserId;

                    if (userId == null && HttpContext.Current != null && HttpContext.Current.Session != null)
                    {
                        userId = (int?)HttpContext.Current.Session["UserId"];
                    }

                    var ipAddress = HttpContext.Current?.Request?.UserHostAddress ?? "::1";

                    var auditLog = new AuditLogModel
                    {
                        UserId = userId,
                        Timestamp = DateTime.Now,
                        Action = action,
                        Controller = controller,
                        Details = details,
                        IpAddress = ipAddress
                    };

                    db.AuditLogs.Add(auditLog);
                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // في حال فشل التسجيل، نكتب في الكونسول حتى لا نوقف النظام
                System.Diagnostics.Debug.WriteLine("Audit Log Failed: " + ex.Message);
            }
        }
    }
}