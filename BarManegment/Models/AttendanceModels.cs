using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    // 1. سجل الحضور والانصراف اليومي
    public class AttendanceLog
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        [Display(Name = "التاريخ")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "وقت الحضور")]
        public TimeSpan? CheckInTime { get; set; }

        [Display(Name = "وقت الانصراف")]
        public TimeSpan? CheckOutTime { get; set; }

        [Display(Name = "الحالة")]
        public string Status { get; set; } // حاضر، متأخر، غائب، إجازة

        [Display(Name = "طريقة التسجيل")]
        public string Method { get; set; } // GPS, Manual, Biometric

        [Display(Name = "ملاحظات النظام")]
        public string SystemNotes { get; set; } // لتسجيل الإحداثيات أو الـ IP
    }

    // 2. طلبات الإجازات (أيام)
    public class LeaveRequest
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        [Display(Name = "نوع الإجازة")]
        public string LeaveType { get; set; } // سنوية، مرضية، طارئة

        [Display(Name = "من تاريخ")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "إلى تاريخ")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "عدد الأيام")]
        public int TotalDays { get; set; }

        [Display(Name = "السبب")]
        public string Reason { get; set; }

        [Display(Name = "حالة الطلب")]
        public string Status { get; set; } = "قيد الانتظار"; // قيد الانتظار، مقبول، مرفوض

        [Display(Name = "رد المدير")]
        public string ManagerComment { get; set; }
    }

    // 3. الأذونات (ساعات مغادرة أثناء الدوام)
    public class HourlyPermission
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }

        [Display(Name = "التاريخ")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "من الساعة")]
        public TimeSpan StartTime { get; set; }

        [Display(Name = "إلى الساعة")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "نوع الإذن")]
        public string Type { get; set; } // شخصي، عمل خارجي

        [Display(Name = "السبب")]
        public string Reason { get; set; }

        [Display(Name = "الحالة")]
        public string Status { get; set; } = "قيد الانتظار";
    }

    // 4. العطل الرسمية (لعدم احتسابها غياب)
    public class OfficialHoliday
    {
        public int Id { get; set; }

        [Required, Display(Name = "اسم العطلة")]
        public string Name { get; set; } // عيد الفطر، يوم العمال...

        [Display(Name = "من تاريخ")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }

        [Display(Name = "إلى تاريخ")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }
    }

    public class Branch
    {
        public int Id { get; set; }

        [Required, Display(Name = "اسم الفرع")]
        public string Name { get; set; } // مثال: المقر الرئيسي - غزة

        [Required, Display(Name = "خط العرض")]
        public string Latitude { get; set; }

        [Required, Display(Name = "خط الطول")]
        public string Longitude { get; set; }

        [Display(Name = "نطاق السماح (متر)")]
        public int AllowedRadius { get; set; } = 100; // يمكن تخصيص مسافة لكل فرع

        public bool IsActive { get; set; } = true;
    }

    // 1. إعدادات الدوام (Shift)
    public class WorkShift
    {
        public int Id { get; set; }

        [Display(Name = "اسم الوردية")]
        public string Name { get; set; } // مثال: الدوام الصباحي، دوام رمضان

        [Display(Name = "بداية الدوام")]
        public TimeSpan StartTime { get; set; } // 08:00

        [Display(Name = "نهاية الدوام")]
        public TimeSpan EndTime { get; set; }   // 15:00

        [Display(Name = "فترة السماح (دقائق)")]
        public int GracePeriodMinutes { get; set; } = 15; // مسموح التأخر 15 دقيقة

        // أيام الراحة الأسبوعية (Checkboxes)
        public bool IsSaturdayOff { get; set; }
        public bool IsSundayOff { get; set; }
        public bool IsMondayOff { get; set; }
        public bool IsTuesdayOff { get; set; }
        public bool IsWednesdayOff { get; set; }
        public bool IsThursdayOff { get; set; }
        public bool IsFridayOff { get; set; } = true; // الجمعة إجازة افتراضية

        public bool IsDefault { get; set; } // هل هذا هو الدوام الأساسي للنقابة؟
    }
}