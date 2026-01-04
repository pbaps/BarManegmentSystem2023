using BarManegment.Models;
using BarManegment.Helpers;
using BarManegment.Areas.Admin.ViewModels;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Net;
using System.Collections.Generic;
using System;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class FinancialReportsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // (دالة مساعدة لجلب الاستعلام الأساسي غير المرسل وغير المحجوز)
        private IQueryable<FeeDistribution> GetPendingSharesQuery()
        {
            return db.FeeDistributions
                .Include(d => d.Lawyer.User)
                .Include(d => d.Receipt)
                .Include(d => d.ContractTransaction.ContractType.Currency)
                .Where(d => d.ShareType == "حصة محامي" &&
                            d.IsSentToBank == false &&
                            d.IsOnHold == false);
        }

        // --- 1. التقرير الرئيسي (الجاهز للدفع) ---
        public ActionResult Index(DateTime? from, DateTime? to)
        {
            var query = GetPendingSharesQuery();

            if (from.HasValue)
            {
                query = query.Where(d => d.Receipt.BankPaymentDate >= from.Value);
            }
            if (to.HasValue)
            {
                var toDate = to.Value.AddDays(1);
                query = query.Where(d => d.Receipt.BankPaymentDate < toDate);
            }

            var pendingShares = query.ToList();

            var groupedShares = pendingShares
                .GroupBy(d => d.Lawyer)
                .Select(group => new LawyerShareViewModel
                {
                    LawyerId = group.Key.Id,
                    LawyerName = group.Key.ArabicName,
                    IdentificationNumber = group.Key.User?.IdentificationNumber ?? "N/A",
                    BankName = group.Key.BankName,
                    BankBranch = group.Key.BankBranch,
                    AccountNumber = group.Key.AccountNumber,
                    Iban = group.Key.Iban,
                    TotalAmount = group.Sum(d => d.Amount),
                    TransactionCount = group.Count(),
                    CurrencySymbol = group.First().ContractTransaction.ContractType.Currency.Symbol ?? "?"
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

        // --- 2. دالة تأكيد الإرسال للبنك ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult ConfirmTransfer(List<int> selectedLawyerIds, DateTime? from, DateTime? to)
        {
            var fromDateStr = from?.ToString("yyyy-MM-dd");
            var toDateStr = to?.ToString("yyyy-MM-dd");

            if (selectedLawyerIds == null || !selectedLawyerIds.Any())
            {
                TempData["ErrorMessage"] = "الرجاء تحديد محامي واحد على الأقل لتأكيد إرسال مستحقاته.";
                return RedirectToAction("Index", new { from = fromDateStr, to = toDateStr });
            }

            var query = GetPendingSharesQuery().Where(d => selectedLawyerIds.Contains(d.LawyerId.Value));

            if (from.HasValue) query = query.Where(d => d.Receipt.BankPaymentDate >= from.Value);
            if (to.HasValue) { var t = to.Value.AddDays(1); query = query.Where(d => d.Receipt.BankPaymentDate < t); }

            var sharesToUpdate = query.ToList();

            if (!sharesToUpdate.Any())
            {
                TempData["ErrorMessage"] = "لم يتم العثور على حصص لتحديثها.";
                return RedirectToAction("Index", new { from = fromDateStr, to = toDateStr });
            }

            var groupedSharesForReport = sharesToUpdate
                .GroupBy(d => d.Lawyer)
                .Select(group => new LawyerShareViewModel
                {
                    LawyerId = group.Key.Id,
                    LawyerName = group.Key.ArabicName,
                    IdentificationNumber = group.Key.User?.IdentificationNumber ?? "N/A",
                    BankName = group.Key.BankName,
                    BankBranch = group.Key.BankBranch,
                    AccountNumber = group.Key.AccountNumber,
                    Iban = group.Key.Iban,
                    TotalAmount = group.Sum(d => d.Amount),
                    TransactionCount = group.Count(),
                    CurrencySymbol = group.First().ContractTransaction.ContractType.Currency.Symbol ?? "?"
                })
                .OrderBy(x => x.LawyerName)
                .ToList();

            byte[] fileBytes;
            string fileName = $"BankTransferSheet_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("حصص المحامين");
                worksheet.View.RightToLeft = true;

                worksheet.Cells[1, 1].Value = "اسم المحامي";
                worksheet.Cells[1, 2].Value = "الرقم الوطني";
                worksheet.Cells[1, 3].Value = "اسم البنك";
                worksheet.Cells[1, 4].Value = "فرع البنك";
                worksheet.Cells[1, 5].Value = "رقم الحساب";
                worksheet.Cells[1, 6].Value = "رقم IBAN";
                worksheet.Cells[1, 7].Value = "المبلغ المستحق";
                worksheet.Cells[1, 8].Value = "العملة";
                worksheet.Cells[1, 9].Value = "عدد المعاملات";

                using (var range = worksheet.Cells["A1:I1"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

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
                    worksheet.Cells[row, 8].Value = item.CurrencySymbol;
                    worksheet.Cells[row, 9].Value = item.TransactionCount;
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

            string tempPath = Server.MapPath("~/Uploads/TempReports/");
            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);
            string filePath = Path.Combine(tempPath, fileName);
            System.IO.File.WriteAllBytes(filePath, fileBytes);

            foreach (var share in sharesToUpdate)
            {
                share.IsSentToBank = true;
                share.BankSendDate = DateTime.Now;
                db.Entry(share).State = EntityState.Modified;
            }
            db.SaveChanges();

            TempData["SuccessMessage"] = $"تم تأكيد إرسال مستحقات لـ ({sharesToUpdate.Count}) معاملة بنجاح.";
            TempData["ReportUrl"] = Url.Action("DownloadReport", new { fileName = fileName });

            return RedirectToAction("Index", new { from = fromDateStr, to = toDateStr });
        }

        [HttpGet]
        public ActionResult DownloadReport(string fileName)
        {
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

            TempData["ErrorMessage"] = "لم يتم العثور على ملف التقرير المطلوب.";
            return RedirectToAction("History");
        }

        // --- 3. التقارير الأرشيفية ---
        public ActionResult History(DateTime? from, DateTime? to)
        {
            var query = db.FeeDistributions
                .Include(d => d.Lawyer)
                .Where(d => d.ShareType == "حصة محامي" && d.IsSentToBank == true);

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

        public ActionResult HeldShares()
        {
            var heldShares = db.FeeDistributions
                .Include(d => d.Lawyer)
                .Where(d => d.ShareType == "حصة محامي" && d.IsOnHold == true && d.IsSentToBank == false)
                .OrderBy(d => d.Lawyer.ArabicName)
                .ToList();

            return View(heldShares);
        }

        public ActionResult ExportToExcel(DateTime? from, DateTime? to)
        {
            var query = GetPendingSharesQuery();
            if (from.HasValue) query = query.Where(d => d.Receipt.BankPaymentDate >= from.Value);
            if (to.HasValue) { var t = to.Value.AddDays(1); query = query.Where(d => d.Receipt.BankPaymentDate < t); }
            var pendingShares = query.ToList();

            var groupedShares = pendingShares
                .GroupBy(d => d.Lawyer)
                .Select(group => new LawyerShareViewModel
                {
                    LawyerId = group.Key.Id,
                    LawyerName = group.Key.ArabicName,
                    IdentificationNumber = group.Key.User?.IdentificationNumber ?? "N/A",
                    BankName = group.Key.BankName,
                    BankBranch = group.Key.BankBranch,
                    AccountNumber = group.Key.AccountNumber,
                    Iban = group.Key.Iban,
                    TotalAmount = group.Sum(d => d.Amount),
                    TransactionCount = group.Count(),
                    CurrencySymbol = group.First().ContractTransaction.ContractType.Currency.Symbol ?? "?"
                })
                .OrderBy(x => x.LawyerName)
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("حصص المحامين");
                worksheet.View.RightToLeft = true;

                worksheet.Cells[1, 1].Value = "اسم المحامي";
                worksheet.Cells[1, 2].Value = "الرقم الوطني";
                worksheet.Cells[1, 3].Value = "اسم البنك";
                worksheet.Cells[1, 4].Value = "فرع البنك";
                worksheet.Cells[1, 5].Value = "رقم الحساب";
                worksheet.Cells[1, 6].Value = "رقم IBAN";
                worksheet.Cells[1, 7].Value = "المبلغ المستحق";
                worksheet.Cells[1, 8].Value = "العملة";
                worksheet.Cells[1, 9].Value = "عدد المعاملات";

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
                    worksheet.Cells[row, 8].Value = item.CurrencySymbol;
                    worksheet.Cells[row, 9].Value = item.TransactionCount;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();
                var fileBytes = package.GetAsByteArray();
                string fileName = $"BankTransferSheet_Review_{DateTime.Now:yyyy-MM-dd}.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        // تقرير كشف أرصدة البنوك والصناديق
        public ActionResult BankBalances()
        {
            // 1. جلب حسابات النقدية والبنوك
            var bankAccountsIds = db.Accounts
                .Where(a => a.IsTransactional && (a.Code.StartsWith("1101") || a.Code.StartsWith("1102")))
                .Select(a => a.Id)
                .ToList();

            // 2. جلب الحركات (تم التعديل لاستخدام JournalEntryDetails)
            var details = db.JournalEntryDetails
                .Include(l => l.Account)
                .Include(l => l.Currency)
                .Where(l => bankAccountsIds.Contains(l.AccountId) && l.JournalEntry.IsPosted)
                .ToList();

            // 3. التجميع حسب الحساب
            var reportData = details
                .GroupBy(l => l.Account)
                .Select(g => new BankBalanceViewModel
                {
                    AccountCode = g.Key.Code,
                    AccountName = g.Key.Name,

                    // الرصيد المحلي (الشيكل)
                    LocalBalance = g.Sum(x => x.Debit - x.Credit),

                    // العملة الأجنبية
                    CurrencySymbol = g.FirstOrDefault(x => x.CurrencyId != null)?.Currency?.Symbol ?? "₪",

                    // ✅ هنا الإصلاح: معالجة القيم الفارغة (Null Coalescing)
                    ForeignBalance = g.Sum(x => (x.ForeignDebit) - (x.ForeignCredit)) // تم إزالة الـ ? لأن الحقول في الموديل ليست Nullable الآن (decimal)
                })
                .ToList();

            // تصحيح الأرصدة للعرض
            foreach (var item in reportData)
            {
                if (item.CurrencySymbol == "₪" || item.ForeignBalance == 0)
                {
                    item.ForeignBalance = item.LocalBalance;
                    item.CurrencySymbol = "₪";
                }
            }

            return View(reportData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}