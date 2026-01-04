// File Path: BarManegment/Services/AuditService.cs
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
        /// <param name="controller">اسم المتحكم (e.g., "Users", "Account").</param>
        /// <param name="details">تفاصيل إضافية عن الحدث.</param>
        public static void LogAction(string action, string controller, string details)
        {
            try
            {
                using (var db = new ApplicationDbContext())
                {
                    var userId = (int?)HttpContext.Current.Session["UserId"];
                    var ipAddress = HttpContext.Current.Request.UserHostAddress;

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
                // في مشروع حقيقي، يمكنك تسجيل هذا الخطأ في ملف نصي أو نظام مراقبة آخر
                Console.WriteLine(ex.Message);
            }
        }
    }
}