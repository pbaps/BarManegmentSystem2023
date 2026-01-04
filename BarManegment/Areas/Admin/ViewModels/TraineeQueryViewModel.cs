using BarManegment.Models;
using System.Collections.Generic;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using System;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class TraineeQueryViewModel
    {
        // --- 1. حقول البحث ---
        [Display(Name = "بحث بالاسم/الرقم")]
        public string SearchTerm { get; set; }

        [Display(Name = "الحالة")]
        public int? StatusId { get; set; }

        [Display(Name = "المشرف")]
        public int? SupervisorId { get; set; }

        [Display(Name = "من تاريخ بدء تدريب")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "إلى تاريخ بدء تدريب")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        // --- 2. قوائم منسدلة للبحث ---
        public SelectList Statuses { get; set; }
        public SelectList Supervisors { get; set; }
        public SelectList Governorates { get; set; }
        // === نهاية الإضافة ===
        [Display(Name = "المحافظة")]
        public string Governorate { get; set; }
        // === نهاية الإضافة ===
        // --- 3. اختيار الأعمدة ---
        [Display(Name = "اختر الأعمدة للعرض والتصدير")]
        public List<string> SelectedColumns { get; set; }
        public MultiSelectList AvailableColumns { get; set; }

        // --- 4. النتائج ---
        public List<GraduateApplication> Results { get; set; }

        // --- Constructor ---
        public TraineeQueryViewModel()
        {
            Results = new List<GraduateApplication>();
            SelectedColumns = new List<string>();
        }

        // دالة مساعدة لإنشاء قائمة الأعمدة المتاحة
        public static MultiSelectList GetAvailableColumns()
        {
            var columns = new Dictionary<string, string>
            {
                { "TraineeSerialNo", "رقم المتدرب" },
                { "ArabicName", "الاسم بالعربية" },
                { "NationalIdNumber", "الرقم الوطني" },
                { "Status", "الحالة" },
                { "Supervisor", "المشرف" },
                { "TrainingStartDate", "تاريخ بدء التدريب" },
                { "TrainingEndDate", "تاريخ انتهاء التدريب" },
                { "Gender", "الجنس" },
                { "BirthDate", "تاريخ الميلاد" },
                { "MobileNumber", "رقم الجوال" },
                { "Email", "البريد الإلكتروني" },
                { "Governorate", "المحافظة" },
                { "Address", "العنوان" }
                // يمكنك إضافة المزيد من الحقول هنا
            };

            return new MultiSelectList(columns, "Key", "Value");
        }
    }
}