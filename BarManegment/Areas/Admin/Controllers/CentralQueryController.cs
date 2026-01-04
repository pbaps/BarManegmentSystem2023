using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class CentralQueryController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/CentralQuery/Index
        public ActionResult Index(string status, string governorate, string hasDebt, string searchTerm)
        {
            // 1. الاستعلام الأساسي (يشمل جميع العلاقات المحتملة للفلترة)
            var query = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Include(g => g.ContactInfo)
                .Include(g => g.LegalResearches)
                .Include(g => g.LoanApplications.Select(l => l.Installments))
                .AsQueryable();

            // 2. تطبيق فلتر البحث العام
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(g => g.ArabicName.Contains(searchTerm) ||
                                         g.NationalIdNumber == searchTerm ||
                                         g.MembershipId == searchTerm);
            }

            // 3. تطبيق فلتر الحالة (Status)
            if (!string.IsNullOrWhiteSpace(status) && status != "الكل")
            {
                query = query.Where(g => g.ApplicationStatus.Name == status);
            }

            // 4. تطبيق فلتر المحافظة
            if (!string.IsNullOrWhiteSpace(governorate) && governorate != "الكل")
            {
                query = query.Where(g => g.ContactInfo.Governorate == governorate);
            }

            // 5. تطبيق فلتر المديونية
            if (hasDebt == "true")
            {
                // جلب IDs المحامين الذين لديهم أقساط مستحقة
                var debtorIds = db.LoanInstallments
                    // ✅ الإصلاح 1: إزالة .Value والاعتماد على أن LawyerId هو int (أو int?)
                    .Where(i => i.LoanApplication.LawyerId > 0 && (i.Status == "مستحق" || i.Status == "متأخر"))
                    .Select(i => i.LoanApplication.LawyerId) // ✅ تم إزالة .Value
                    .Distinct()
                    .ToList();

                query = query.Where(g => debtorIds.Contains(g.Id));
            }


            // 6. تنفيذ الاستعلام وجلب البيانات
            var applications = query.OrderByDescending(g => g.SubmissionDate).ToList();

            // 7. بناء البيانات الإحصائية (Stats)
            var stats = new CentralQueryStats
            {
                TotalRecords = applications.Count,
                PracticingCount = applications.Count(g => g.ApplicationStatus.Name == "محامي مزاول"),
                TraineeCount = applications.Count(g => g.ApplicationStatus.Name == "متدرب مقيد"),

                // ✅ إصلاح: حل مشكلة CS0428 (Count)
                DebtCount = applications.Count(g => g.LoanApplications.Any(l => l.Installments.Any(i => i.Status == "مستحق" || i.Status == "متأخر"))),

                ResearchAcceptedCount = applications.Count(g => g.LegalResearches.Any(r => r.Status == "مقبول"))
            };

            // 8. تعبئة ViewModel
            var viewModel = new CentralQueryViewModel
            {
                Applications = applications,
                Stats = stats,
                // قوائم الإسقاط (Drop-downs)
                Statuses = new SelectList(db.ApplicationStatuses.Where(s => s.Name.Contains("محامي") || s.Name.Contains("متدرب")), "Name", "Name", status),
                Governorates = new SelectList(GetPalestinianGovernorates(), "Value", "Text", governorate)
            };

            ViewBag.CurrentStatus = status;
            ViewBag.CurrentGov = governorate;
            ViewBag.CurrentDebt = hasDebt;
            ViewBag.SearchTerm = searchTerm;

            return View(viewModel);
        }

        // POST: Admin/CentralQuery/ExportToExcel
        [HttpPost]
        [CustomAuthorize(Permission = "CanExport")]
        public ActionResult ExportToExcel(string status, string governorate, string hasDebt, string searchTerm)
        {
            // (نستخدم نفس منطق Index لضمان تصدير البيانات المفلترة حالياً)
            var query = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Include(g => g.ContactInfo)
                .Include(g => g.LegalResearches)
                .Include(g => g.LoanApplications.Select(l => l.Installments))
                .AsQueryable();

            // (تطبيق الفلاتر كما في Index)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(g => g.ArabicName.Contains(searchTerm) ||
                                        g.NationalIdNumber == searchTerm ||
                                        g.MembershipId == searchTerm);
            }
            if (!string.IsNullOrWhiteSpace(status) && status != "الكل")
            {
                query = query.Where(g => g.ApplicationStatus.Name == status);
            }
            if (!string.IsNullOrWhiteSpace(governorate) && governorate != "الكل")
            {
                query = query.Where(g => g.ContactInfo.Governorate == governorate);
            }

            if (hasDebt == "true")
            {
                var debtorIds = db.LoanInstallments
                    // ✅ الإصلاح 2: إزالة .Value والاعتماد على GraduateApplicationId الذي هو int
                    .Where(i => i.LoanApplication.LawyerId > 0 && (i.Status == "مستحق" || i.Status == "متأخر"))
                    .Select(i => i.LoanApplication.LawyerId)
                    .Distinct()
                    .ToList();
                query = query.Where(g => debtorIds.Contains(g.Id));
            }


            var data = query.OrderByDescending(g => g.SubmissionDate).ToList();

            // 4. إنشاء ملف Excel
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("كشف_الاستعلامات_المركزي");
                worksheet.View.RightToLeft = true;

                // العناوين
                worksheet.Cells[1, 1].Value = "رقم الملف";
                worksheet.Cells[1, 2].Value = "الاسم";
                worksheet.Cells[1, 3].Value = "الحالة";
                worksheet.Cells[1, 4].Value = "المحافظة";
                worksheet.Cells[1, 5].Value = "رقم العضوية";
                worksheet.Cells[1, 6].Value = "تاريخ البدء";
                worksheet.Cells[1, 7].Value = "البحث مجاز؟";
                worksheet.Cells[1, 8].Value = "مديونية قروض؟";

                // تعبئة البيانات
                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cells[row, 1].Value = item.Id;
                    worksheet.Cells[row, 2].Value = item.ArabicName;
                    worksheet.Cells[row, 3].Value = item.ApplicationStatus.Name;
                    worksheet.Cells[row, 4].Value = item.ContactInfo?.Governorate ?? "N/A";
                    worksheet.Cells[row, 5].Value = item.MembershipId ?? "-";
                    worksheet.Cells[row, 6].Value = item.TrainingStartDate?.ToString("yyyy-MM-dd") ?? "-";
                    worksheet.Cells[row, 7].Value = item.LegalResearches.Any(r => r.Status == "مقبول") ? "نعم" : "لا";
                    worksheet.Cells[row, 8].Value = item.LoanApplications.Any(l => l.Installments.Any(i => i.Status == "مستحق" || i.Status == "متأخر")) ? "نعم" : "لا";
                    row++;
                }

                worksheet.Cells.AutoFitColumns();
                var fileBytes = package.GetAsByteArray();
                string fileName = $"Central_Query_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // --- 4. الدوال المساعدة (View Models) ---

        private static List<SelectListItem> GetPalestinianGovernorates()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = "محافظة غزة", Value = "محافظة غزة" },
                new SelectListItem { Text = "محافظة خان يونس", Value = "محافظة خان يونس" },
                new SelectListItem { Text = "محافظة رفح", Value = "محافظة رفح" },
                new SelectListItem { Text = "محافظة شمال غزة", Value = "محافظة شمال غزة" },
                new SelectListItem { Text = "محافظة دير البلح", Value = "محافظة دير البلح" },
                new SelectListItem { Text = "محافظة جنين", Value = "محافظة جنين" },
                new SelectListItem { Text = "محافظة طوباس", Value = "محافظة طوباس" },
                new SelectListItem { Text = "محافظة طولكرم", Value = "محافظة طولكرم" },
                new SelectListItem { Text = "محافظة نابلس", Value = "محافظة نابلس" },
                new SelectListItem { Text = "محافظة قلقيلية", Value = "محافظة قلقيلية" },
                new SelectListItem { Text = "محافظة سلفيت", Value = "محافظة سلفيت" },
                new SelectListItem { Text = "محافظة رام الله والبيرة", Value = "محافظة رام الله والبيرة" },
                new SelectListItem { Text = "محافظة أريحا", Value = "محافظة أريحا" },
                new SelectListItem { Text = "محافظة القدس", Value = "محافظة القدس" },
                new SelectListItem { Text = "محافظة بيت لحم", Value = "محافظة بيت لحم" },
                new SelectListItem { Text = "محافظة الخليل", Value = "محافظة الخليل" }
            };
        }
    }

    // (ViewModels - يجب أن تنشأ في ملف منفصل)
 


}