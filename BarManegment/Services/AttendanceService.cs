using BarManegment.Models;
using System;
using System.Linq;

namespace BarManegment.Services
{
    public class AttendanceService
    {
        private ApplicationDbContext db;

        public AttendanceService()
        {
            db = new ApplicationDbContext();
        }

        // 1. هل هذا التاريخ يوم عمل رسمي؟
        public bool IsWorkingDay(DateTime date)
        {
            // أ) التحقق من العطل الرسمية (أعياد، مناسبات)
            bool isHoliday = db.OfficialHolidays.Any(h => date >= h.FromDate && date <= h.ToDate);
            if (isHoliday) return false;

            // ب) التحقق من الإجازة الأسبوعية (الجمعة/السبت)
            // نجلب الدوام الافتراضي
            var shift = db.WorkShifts.FirstOrDefault(s => s.IsDefault);
            if (shift != null)
            {
                var day = date.DayOfWeek;
                if (day == DayOfWeek.Friday && shift.IsFridayOff) return false;
                if (day == DayOfWeek.Saturday && shift.IsSaturdayOff) return false;
                if (day == DayOfWeek.Sunday && shift.IsSundayOff) return false;
                if (day == DayOfWeek.Monday && shift.IsMondayOff) return false;
                if (day == DayOfWeek.Tuesday && shift.IsTuesdayOff) return false;
                if (day == DayOfWeek.Wednesday && shift.IsWednesdayOff) return false;
                if (day == DayOfWeek.Thursday && shift.IsThursdayOff) return false;
            }
            else
            {
                // افتراضي إذا لم يتم إعداد النظام: الجمعة إجازة
                if (date.DayOfWeek == DayOfWeek.Friday) return false;
            }

            return true;
        }

        // 2. حساب حالة الحضور (حاضر أم متأخر)
        public string CalculateStatus(TimeSpan checkInTime)
        {
            var shift = db.WorkShifts.FirstOrDefault(s => s.IsDefault);
            if (shift == null) return "حاضر"; // لا يوجد إعدادات

            // وقت الحضور المسموح = وقت البدء + فترة السماح
            var lateLimit = shift.StartTime.Add(TimeSpan.FromMinutes(shift.GracePeriodMinutes));

            if (checkInTime > lateLimit)
            {
                // حساب دقائق التأخير
                var lateMinutes = (checkInTime - shift.StartTime).TotalMinutes;
                return $"متأخر ({Math.Ceiling(lateMinutes)} دقيقة)";
            }

            return "حاضر";
        }
    }
}