using BarManegment.Models;
using System;
using System.Linq;

namespace BarManegment.Helpers
{
    public static class LawyerStatusHelper
    {
        /// <summary>
        /// التحقق مما إذا كان المحامي يعتبر "نشطاً" ويستحق الخدمات
        /// </summary>
        public static bool IsActiveLawyer(GraduateApplication lawyer)
        {
            if (lawyer == null || lawyer.ApplicationStatus == null) return false;

            string status = lawyer.ApplicationStatus.Name;

            // 1. إذا كان مزاولاً دافعاً للرسوم -> فعال دائماً
            if (status == "محامي مزاول") return true;

            // 2. إذا كان بانتظار التجديد -> نتحقق من فترة السماح الديناميكية
            if (status == "بانتظار تجديد المزاولة")
            {
                DateTime gracePeriodEnd;

                // فتح اتصال مؤقت لقراءة الإعدادات لضمان الحصول على أحدث قيمة
                using (var db = new ApplicationDbContext())
                {
                    var setting = db.SystemSettings.Find("RenewalGracePeriodEndDate");

                    if (setting != null && DateTime.TryParse(setting.SettingValue, out DateTime parsedDate))
                    {
                        gracePeriodEnd = parsedDate;
                    }
                    else
                    {
                        // القيمة الافتراضية في حال لم يتم ضبط الإعدادات: 31 مارس من العام الحالي
                        gracePeriodEnd = new DateTime(DateTime.Now.Year, 3, 31);
                    }
                }

                // إذا كنا قبل التاريخ المحدد -> فعال (يستفيد من فترة السماح)
                if (DateTime.Now.Date <= gracePeriodEnd.Date) return true;

                // إذا تجاوزنا التاريخ -> غير فعال (يجب عليه الدفع)
                return false;
            }

            // أي حالة أخرى (غير مزاول، مشطوب، الخ) -> غير فعال
            return false;
        }
    }
}