using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class StampFinancialsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // (دالة مساعدة لجلب الاستعلام الأساسي غير المرسل وغير المحجوز)
        private IQueryable<StampSale> GetPendingSharesQuery()
        {
            return db.StampSales
                .Include(d => d.Lawyer) // (جلب المحامي)
                .Where(d => d.GraduateApplicationId.HasValue && // <-- ✅ إصلاح: استخدام GraduateApplicationId
                            d.IsPaidToLawyer == false &&
                            d.IsOnHold == false);
        }

        // --- 1. التقرير الرئيسي (الجاهز للدفع) ---
        public ActionResult Index(DateTime? from, DateTime? to)
        {
            var query = GetPendingSharesQuery();

            // (تصفية التاريخ بناءً على تاريخ البيع)
            if (from.HasValue)
            {
                query = query.Where(d => d.SaleDate >= from.Value);
            }
            if (to.HasValue)
            {
                var toDate = to.Value.AddDays(1);
                query = query.Where(d => d.SaleDate < toDate);
            }

            var pendingShares = query.ToList();

            var groupedShares = pendingShares
                .GroupBy(d => d.Lawyer)
                .Select(group => new LawyerStampShareViewModel
                {
                    LawyerId = group.Key.Id,
                    LawyerName = group.Key.ArabicName,
                    IdentificationNumber = group.Key.NationalIdNumber, // <-- ✅ إصلاح: جلب الرقم الوطني مباشرة
                    BankName = group.Key.BankName,
                    BankBranch = group.Key.BankBranch,
                    AccountNumber = group.Key.AccountNumber,
                    Iban = group.Key.Iban,
                    TotalAmount = group.Sum(d => d.AmountToLawyer),
                    TransactionCount = group.Count()
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            ViewBag.FromDate = from?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to?.ToString("yyyy-MM-dd");
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            ViewBag.ErrorMessage = TempData["ErrorMessage"];
            ViewBag.ReportUrl = TempData["ReportUrl"];

            return View(groupedShares);
        }

        // --- 2. دالة تأكيد الإرسال للبنك (معدلة) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult ConfirmTransfer(List<int> selectedLawyerIds, DateTime? from, DateTime? to)
        {
            var fromDateStr = from?.ToString("yyyy-MM-dd");
            var toDateStr = to?.ToString("yyyy-MM-dd");

            if (selectedLawyerIds == null || !selectedLawyerIds.Any())
            {
                TempData["ErrorMessage"] = "الرجاء تحديد محامي واحد على الأقل.";
                return RedirectToAction("Index", new { from = fromDateStr, to = toDateStr });
            }

            // 1. جلب كل الحصص الجاهزة
            var query = GetPendingSharesQuery()
                            // <-- ✅ إصلاح: استخدام GraduateApplicationId
                            .Where(d => selectedLawyerIds.Contains(d.GraduateApplicationId.Value));

            if (from.HasValue) query = query.Where(d => d.SaleDate >= from.Value);
            if (to.HasValue)
            {
                var toDate = to.Value.AddDays(1);
                query = query.Where(d => d.SaleDate < toDate);
            }

            var sharesToUpdate = query.ToList();

            if (!sharesToUpdate.Any())
            {
                TempData["ErrorMessage"] = "لم يتم العثور على حصص لتحديثها.";
                return RedirectToAction("Index", new { from = fromDateStr, to = toDateStr });
            }

            // (تجميع البيانات للتقرير)
            var groupedSharesForReport = sharesToUpdate
                .GroupBy(d => d.Lawyer)
                .Select(group => new LawyerStampShareViewModel
                {
                    LawyerName = group.Key.ArabicName,
                    IdentificationNumber = group.Key.NationalIdNumber, // <-- ✅ إصلاح
                    BankName = group.Key.BankName,
                    BankBranch = group.Key.BankBranch,
                    AccountNumber = group.Key.AccountNumber,
                    Iban = group.Key.Iban,
                    TotalAmount = group.Sum(d => d.AmountToLawyer),
                    TransactionCount = group.Count()
                })
                .OrderBy(x => x.LawyerName)
                .ToList();

            // (إنشاء ملف Excel)
            byte[] fileBytes;
            string fileName = $"StampTransferSheet_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            // ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // (تأكد أن هذا مفعل في Global.asax)

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("حصص طوابع المحامين");
                worksheet.View.RightToLeft = true;
                worksheet.Cells[1, 1].Value = "اسم المحامي";
                worksheet.Cells[1, 2].Value = "الرقم الوطني";
                worksheet.Cells[1, 3].Value = "اسم البنك";
                worksheet.Cells[1, 4].Value = "فرع البنك";
                worksheet.Cells[1, 5].Value = "رقم الحساب";
                worksheet.Cells[1, 6].Value = "رقم IBAN";
                worksheet.Cells[1, 7].Value = "المبلغ المستحق";
                worksheet.Cells[1, 8].Value = "العملة";
                worksheet.Cells[1, 9].Value = "عدد الطوابع";
                using (var range = worksheet.Cells["A1:I1"]) { /* ... (تنسيق) ... */ }

                int row = 2;
                foreach (var item in groupedSharesForReport)
                {
                    worksheet.Cells[row, 1].Value = item.LawyerName;
                    worksheet.Cells[row, 2].Value = item.IdentificationNumber;
                    worksheet.Cells[row, 3].Value = item.BankName;
                    worksheet.Cells[row, 4].Value = item.BankBranch;
                    worksheet.Cells[row, 5].Value = item.AccountNumber;
                    worksheet.Cells[row, 6].Value = item.Iban;
                    worksheet.Cells[row, 7].Value = item.TotalAmount;
                    worksheet.Cells[row, 8].Value = "₪";
                    worksheet.Cells[row, 9].Value = item.TransactionCount; // <-- (هذا السطر 188، وهو سليم)
                    worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0.00";
                    row++;
                }
                worksheet.Cells[row, 6].Value = "الإجمالي";
                worksheet.Cells[row, 7].Formula = $"SUM(G2:G{row - 1})";
                worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0.00";
                worksheet.Cells[$"A{row}:I{row}"].Style.Font.Bold = true;
                worksheet.Cells.AutoFitColumns();
                fileBytes = package.GetAsByteArray();
            }

            // (حفظ الملف مؤقتاً)
            string tempPath = Server.MapPath("~/Uploads/TempReports/");
            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
            string filePath = Path.Combine(tempPath, fileName);
            System.IO.File.WriteAllBytes(filePath, fileBytes);

            // 2. تحديث الحالات
            foreach (var share in sharesToUpdate)
            {
                share.IsPaidToLawyer = true;
                share.BankSendDate = DateTime.Now;
                db.Entry(share).State = EntityState.Modified;
            }
            db.SaveChanges();

            TempData["SuccessMessage"] = $"تم تأكيد إرسال مستحقات لـ ({sharesToUpdate.Count}) طابع بنجاح.";
            TempData["ReportUrl"] = Url.Action("DownloadReport", new { fileName = fileName });

            return RedirectToAction("Index", new { from = fromDateStr, to = toDateStr });
        }

        // (GET: ExportToExcel)
        public ActionResult ExportToExcel(DateTime? from, DateTime? to)
        {
            var query = GetPendingSharesQuery();
            if (from.HasValue) query = query.Where(d => d.SaleDate >= from.Value);
            if (to.HasValue)
            {
                var toDate = to.Value.AddDays(1);
                query = query.Where(d => d.SaleDate < toDate);
            }
            var pendingShares = query.ToList();

            var groupedShares = pendingShares
                .GroupBy(d => d.Lawyer)
                .Select(group => new LawyerStampShareViewModel
                {
                    LawyerName = group.Key.ArabicName,
                    IdentificationNumber = group.Key.NationalIdNumber, // <-- ✅ إصلاح
                    BankName = group.Key.BankName,
                    BankBranch = group.Key.BankBranch,
                    AccountNumber = group.Key.AccountNumber,
                    Iban = group.Key.Iban,
                    TotalAmount = group.Sum(d => d.AmountToLawyer),
                    TransactionCount = group.Count()
                })
                .OrderBy(x => x.LawyerName)
                .ToList();

            // (إنشاء ملف Excel)
            using (var package = new ExcelPackage())
            {
                // ... (نفس كود Excel الموجود في ConfirmTransfer) ...
                var worksheet = package.Workbook.Worksheets.Add("حصص طوابع المحامين");
                worksheet.View.RightToLeft = true;
                worksheet.Cells[1, 1].Value = "اسم المحامي";
                worksheet.Cells[1, 2].Value = "الرقم الوطني";
                worksheet.Cells[1, 3].Value = "اسم البنك";
                worksheet.Cells[1, 4].Value = "فرع البنك";
                worksheet.Cells[1, 5].Value = "رقم الحساب";
                worksheet.Cells[1, 6].Value = "رقم IBAN";
                worksheet.Cells[1, 7].Value = "المبلغ المستحق";
                worksheet.Cells[1, 8].Value = "العملة";
                worksheet.Cells[1, 9].Value = "عدد الطوابع";
                using (var range = worksheet.Cells["A1:I1"]) { range.Style.Font.Bold = true; /* ... إلخ ... */ }

                int row = 2;
                foreach (var item in groupedShares)
                {
                    worksheet.Cells[row, 1].Value = item.LawyerName;
                    worksheet.Cells[row, 2].Value = item.IdentificationNumber;
                    worksheet.Cells[row, 3].Value = item.BankName;
                    worksheet.Cells[row, 4].Value = item.BankBranch;
                    worksheet.Cells[row, 5].Value = item.AccountNumber;
                    worksheet.Cells[row, 6].Value = item.Iban;
                    worksheet.Cells[row, 7].Value = item.TotalAmount;
                    worksheet.Cells[row, 8].Value = "₪";
                    worksheet.Cells[row, 9].Value = item.TransactionCount;
                    worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0.00";
                    row++;
                }
                worksheet.Cells.AutoFitColumns();
                // ... (نهاية كود Excel) ...

                var fileBytes = package.GetAsByteArray();
                string fileName = $"StampTransferSheet_Review_{DateTime.Now:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // (GET: DownloadReport)
        [HttpGet]
        public ActionResult DownloadReport(string fileName)
        {
            // ... (هذا الكود سليم) ...
            string tempPath = Server.MapPath("~/Uploads/TempReports/");
            if (string.IsNullOrEmpty(fileName) || !fileName.EndsWith(".xlsx") || fileName.Contains(".."))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid file name.");
            }
            string filePath = Path.Combine(tempPath, fileName);
            if (System.IO.File.Exists(filePath))
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            return RedirectToAction("History");
        }

        // --- 3. التقارير الأرشيفية (المرسلة والمحجوزة) ---
        // (GET: History)
        public ActionResult History(DateTime? from, DateTime? to)
        {
            var query = db.StampSales
                .Include(d => d.Lawyer)
                // <-- ✅ إصلاح: حذف ShareType، واستخدام GraduateApplicationId
                .Where(d => d.GraduateApplicationId.HasValue && d.IsPaidToLawyer == true);

            if (from.HasValue) query = query.Where(d => d.BankSendDate >= from.Value);
            if (to.HasValue)
            {
                var toDate = to.Value.AddDays(1);
                query = query.Where(d => d.BankSendDate < toDate);
            }

            ViewBag.FromDate = from?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to?.ToString("yyyy-MM-dd");
            return View(query.OrderByDescending(d => d.BankSendDate).ToList());
        }

        // (GET: HeldShares)
        // (GET: HeldShares)
        // (GET: HeldShares) - عرض الحصص المحجوزة مع البحث
        public ActionResult HeldShares(string searchString)
        {
            var query = db.StampSales
                .Include(d => d.Lawyer)
                .Include(d => d.Stamp)
                .Where(d => d.GraduateApplicationId.HasValue &&
                            d.IsOnHold == true &&
                            d.IsPaidToLawyer == false);

            // --- إضافة منطق البحث ---
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(d => d.Lawyer.ArabicName.Contains(searchString) ||
                                         d.Lawyer.NationalIdNumber.Contains(searchString) ||
                                         d.Stamp.SerialNumber.ToString().Contains(searchString));
            }

            var heldShares = query.OrderBy(d => d.Lawyer.ArabicName).ToList();

            ViewBag.CurrentSearch = searchString; // لإعادة عرض نص البحث في الواجهة

            return View(heldShares);
        }

        // (GET: ExportHeldSharesToExcel) - تصدير الحصص المحجوزة
        public ActionResult ExportHeldSharesToExcel(string searchString)
        {
            // 1. نفس استعلام البحث
            var query = db.StampSales
                .Include(d => d.Lawyer)
                .Include(d => d.Stamp)
                .Where(d => d.GraduateApplicationId.HasValue &&
                            d.IsOnHold == true &&
                            d.IsPaidToLawyer == false);

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(d => d.Lawyer.ArabicName.Contains(searchString) ||
                                         d.Lawyer.NationalIdNumber.Contains(searchString) ||
                                         d.Stamp.SerialNumber.ToString().Contains(searchString));
            }

            var data = query.OrderBy(d => d.Lawyer.ArabicName).ToList();

            // 2. إنشاء ملف Excel
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("الحصص المحجوزة");
                worksheet.View.RightToLeft = true; // اتجاه الورقة من اليمين لليسار

                // العناوين
                worksheet.Cells[1, 1].Value = "تاريخ البيع";
                worksheet.Cells[1, 2].Value = "اسم المحامي";
                worksheet.Cells[1, 3].Value = "قيمة الطابع";
                worksheet.Cells[1, 4].Value = "الحصة المحجوزة";
                worksheet.Cells[1, 5].Value = "رقم الطابع";
                worksheet.Cells[1, 6].Value = "سبب الحجز";

                // تنسيق العناوين
                using (var range = worksheet.Cells["A1:F1"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // تعبئة البيانات
                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cells[row, 1].Value = item.SaleDate.ToString("yyyy/MM/dd");
                    worksheet.Cells[row, 2].Value = item.Lawyer.ArabicName;
                    worksheet.Cells[row, 3].Value = item.StampValue;
                    worksheet.Cells[row, 4].Value = item.AmountToLawyer;
                    worksheet.Cells[row, 5].Value = item.Stamp?.SerialNumber ?? 0;
                    worksheet.Cells[row, 6].Value = item.HoldReason;

                    // تنسيق الأرقام المالية
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "#,##0.00";
                    worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";

                    row++;
                }

                // صف الإجمالي
                worksheet.Cells[row, 3].Value = "الإجمالي:";
                worksheet.Cells[row, 3].Style.Font.Bold = true;
                worksheet.Cells[row, 4].Formula = $"SUM(D2:D{row - 1})";
                worksheet.Cells[row, 4].Style.Font.Bold = true;
                worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0.00";

                worksheet.Cells.AutoFitColumns();

                var fileBytes = package.GetAsByteArray();
                string fileName = $"Held_Stamps_Report_{DateTime.Now:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}