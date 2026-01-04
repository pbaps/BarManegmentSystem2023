using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using OfficeOpenXml; // تأكد من تثبيت EPPlus
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "FinancialAid")]
    public class FinancialAidController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. عرض القائمة (Index)
        // ============================================================
        public ActionResult Index()
        {
            var aids = db.LawyerFinancialAids
                .Include(a => a.Lawyer)
                .Include(a => a.AidType) // يربط مع SystemLookup
                .Include(a => a.Currency)
                .OrderByDescending(a => a.DecisionDate)
                .ToList();
            return View(aids);
        }

        // ============================================================
        // 2. المساعدة الفردية (Create & Edit)
        // ============================================================

        // إنشاء - GET
        public ActionResult Create()
        {
            // تعبئة قائمة المحامين
            ViewBag.LawyerId = new SelectList(db.GraduateApplications
                .Where(x => x.ApplicationStatus.Name.Contains("محامي") || x.ApplicationStatus.Name.Contains("متدرب")),
                "Id", "ArabicName");

            // تعبئة أنواع المساعدة من SystemLookups
            ViewBag.AidTypeId = new SelectList(db.SystemLookups.Where(x => x.Category == "FinancialAidType"), "Id", "Name");
            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name");

            return View();
        }

        // إنشاء - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(LawyerFinancialAid model)
        {
            if (ModelState.IsValid)
            {
                model.DecisionDate = DateTime.Now;
                model.IsPaid = false;

                // تعبئة البيانات تلقائياً من ملف المحامي إذا لم يدخلها المستخدم
                var lawyer = db.GraduateApplications.Find(model.LawyerId);
                if (lawyer != null)
                {
                    if (model.DisbursementMethod == "BankTransfer")
                    {
                        if (string.IsNullOrEmpty(model.TargetBankName)) model.TargetBankName = lawyer.BankName;
                        if (string.IsNullOrEmpty(model.TargetBankBranch)) model.TargetBankBranch = lawyer.BankBranch;
                        if (string.IsNullOrEmpty(model.TargetIban)) model.TargetIban = lawyer.Iban;
                    }
                    else if (model.DisbursementMethod == "Wallet")
                    {
                        if (string.IsNullOrEmpty(model.TargetWalletNumber)) model.TargetWalletNumber = lawyer.WalletNumber;
                    }
                }

                db.LawyerFinancialAids.Add(model);
                db.SaveChanges();

                AuditService.LogAction("CreateFinancialAid", "FinancialAid", $"تم تسجيل مساعدة بقيمة {model.Amount} للمحامي ID: {model.LawyerId}");
                TempData["SuccessMessage"] = "تم تسجيل قرار المساعدة بنجاح.";
                return RedirectToAction("Index");
            }

            // إعادة تعبئة القوائم عند حدوث خطأ
            ViewBag.LawyerId = new SelectList(db.GraduateApplications
                .Where(x => x.ApplicationStatus.Name.Contains("محامي") || x.ApplicationStatus.Name.Contains("متدرب")),
                "Id", "ArabicName", model.LawyerId);

            ViewBag.AidTypeId = new SelectList(db.SystemLookups.Where(x => x.Category == "FinancialAidType"), "Id", "Name", model.AidTypeId);
            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name", model.CurrencyId);

            return View(model);
        }

        // تعديل - GET (جديد)
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var aid = db.LawyerFinancialAids.Include(a => a.Lawyer).FirstOrDefault(a => a.Id == id);
            if (aid == null) return HttpNotFound();

            // منع التعديل إذا تم الصرف
            if (aid.IsPaid)
            {
                TempData["ErrorMessage"] = "لا يمكن تعديل بيانات المساعدة بعد إتمام عملية الصرف.";
                return RedirectToAction("Index");
            }

            // نرسل اسم المحامي للعرض فقط (لا نغير المحامي في التعديل)
            ViewBag.LawyerName = aid.Lawyer.ArabicName;

            ViewBag.AidTypeId = new SelectList(db.SystemLookups.Where(x => x.Category == "FinancialAidType"), "Id", "Name", aid.AidTypeId);
            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name", aid.CurrencyId);

            return View(aid);
        }

        // تعديل - POST (جديد)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(LawyerFinancialAid model)
        {
            if (ModelState.IsValid)
            {
                var existingAid = db.LawyerFinancialAids.Find(model.Id);
                if (existingAid == null) return HttpNotFound();

                // حماية إضافية
                if (existingAid.IsPaid)
                {
                    TempData["ErrorMessage"] = "لا يمكن تعديل المساعدة لأنها مدفوعة.";
                    return RedirectToAction("Index");
                }

                // تحديث الحقول المسموح بها
                existingAid.AidTypeId = model.AidTypeId;
                existingAid.Amount = model.Amount;
                existingAid.CurrencyId = model.CurrencyId;
                existingAid.Notes = model.Notes;

                // تحديث بيانات الصرف
                existingAid.DisbursementMethod = model.DisbursementMethod;
                existingAid.TargetBankName = model.TargetBankName;
                existingAid.TargetBankBranch = model.TargetBankBranch;
                existingAid.TargetIban = model.TargetIban;
                existingAid.TargetWalletNumber = model.TargetWalletNumber;

                db.Entry(existingAid).State = EntityState.Modified;
                db.SaveChanges();

                AuditService.LogAction("EditFinancialAid", "FinancialAid", $"تم تعديل بيانات المساعدة رقم {model.Id}");
                TempData["SuccessMessage"] = "تم حفظ التعديلات بنجاح.";
                return RedirectToAction("Index");
            }

            // إعادة التعبئة في حال الخطأ
            var currentLawyer = db.GraduateApplications.Find(model.LawyerId); // لجلب الاسم في حال الخطأ
            ViewBag.LawyerName = currentLawyer?.ArabicName;

            ViewBag.AidTypeId = new SelectList(db.SystemLookups.Where(x => x.Category == "FinancialAidType"), "Id", "Name", model.AidTypeId);
            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name", model.CurrencyId);

            return View(model);
        }

        // ============================================================
        // 3. الصرف والقيود (Payments)
        // ============================================================

        // صفحة الصرف الفردي - GET
        [HttpGet]
        public ActionResult PayAid(int id)
        {
            var aid = db.LawyerFinancialAids
                .Include(a => a.Lawyer)
                .Include(a => a.AidType)
                .Include(a => a.Currency)
                .FirstOrDefault(a => a.Id == id);

            if (aid == null) return HttpNotFound();

            ViewBag.SourceBankAccountId = new SelectList(db.BankAccounts.Where(b => b.CurrencyId == aid.CurrencyId && b.IsActive), "Id", "BankName");
            return View(aid);
        }

        // تنفيذ الصرف الفردي - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmPayment(int id, int SourceBankAccountId)
        {
            var aid = db.LawyerFinancialAids.Include(a => a.Lawyer).Include(a => a.AidType).FirstOrDefault(a => a.Id == id);
            if (aid == null || aid.IsPaid) return HttpNotFound();

            int userId = (int)Session["UserId"];

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. تسجيل المصروف الإداري
                    var expense = new BarExpense
                    {
                        ExpenseDate = DateTime.Now,
                        Amount = aid.Amount,
                        BankAccountId = SourceBankAccountId,
                        ExpenseCategory = "مساعدات مالية",
                        Description = $"صرف مساعدة ({aid.AidType?.Name}) للمحامي {aid.Lawyer.ArabicName}"
                    };
                    db.BarExpenses.Add(expense);
                    db.SaveChanges();

                    // 2. تحديث حالة المساعدة
                    aid.IsPaid = true;
                    aid.PaymentDate = DateTime.Now;
                    aid.ExpenseId = expense.Id;
                    db.Entry(aid).State = EntityState.Modified;
                    db.SaveChanges();

                    // 3. إنشاء القيد المحاسبي
                    var bankAccountObj = db.BankAccounts.Find(SourceBankAccountId);
                    // البحث عن حساب الأصل (Asset) المرتبط بالبنك
                    var accountingBankId = db.Accounts.FirstOrDefault(a => a.Name.Contains(bankAccountObj.BankName) && a.AccountType == AccountType.Asset)?.Id ?? 0;

                    if (accountingBankId == 0) throw new Exception("لم يتم العثور على حساب محاسبي لهذا البنك في شجرة الحسابات.");

                    var accountingService = new AccountingService();
                    bool entryCreated = accountingService.GenerateEntryForFinancialAid(aid.Id, accountingBankId, userId);

                    if (!entryCreated) throw new Exception("فشل إنشاء القيد المحاسبي. يرجى مراجعة إعدادات الحسابات.");

                    AuditService.LogAction("ConfirmAidPayment", "FinancialAid", $"تم صرف {aid.Amount} للمحامي {aid.Lawyer.ArabicName}");

                    transaction.Commit();
                    TempData["SuccessMessage"] = "تم الصرف وتسجيل القيود بنجاح.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "حدث خطأ: " + ex.Message;
                }
            }

            return RedirectToAction("Index");
        }

        // ============================================================
        // 4. الكشف الجماعي (Batch Create & Approval)
        // ============================================================

        // إنشاء جماعي - GET
        [HttpGet]
        public ActionResult BatchCreate()
        {
            ViewBag.AidTypeId = new SelectList(db.SystemLookups.Where(x => x.Category == "FinancialAidType"), "Id", "Name");
            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name");
            ViewBag.StatusList = new SelectList(db.ApplicationStatuses, "Id", "Name");
            return View(new BatchAidViewModel());
        }

        // إنشاء جماعي - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BatchCreate(BatchAidViewModel model)
        {
            if (ModelState.IsValid && model.SelectedLawyerIds != null && model.SelectedLawyerIds.Any())
            {
                string batchReference = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                foreach (var lawyerId in model.SelectedLawyerIds)
                {
                    var lawyer = db.GraduateApplications.Find(lawyerId);

                    var aid = new LawyerFinancialAid
                    {
                        LawyerId = lawyerId,
                        AidTypeId = model.AidTypeId,
                        Amount = model.AmountPerLawyer,
                        CurrencyId = model.CurrencyId,
                        DecisionDate = DateTime.Now,
                        IsPaid = false,
                        Notes = model.Notes + $" (كشف رقم {batchReference})",
                        BatchReference = batchReference,

                        // تعبئة البيانات
                        TargetBankName = lawyer.BankName,
                        TargetBankBranch = lawyer.BankBranch,
                        TargetIban = lawyer.Iban,
                        TargetWalletNumber = lawyer.WalletNumber
                    };

                    // تحديد الطريقة
                    if (!string.IsNullOrEmpty(lawyer.Iban)) aid.DisbursementMethod = "BankTransfer";
                    else if (!string.IsNullOrEmpty(lawyer.WalletNumber)) aid.DisbursementMethod = "Wallet";
                    else aid.DisbursementMethod = "Cash";

                    db.LawyerFinancialAids.Add(aid);
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = $"تم حفظ الكشف المبدئي برقم مرجعي ({batchReference}). يرجى مراجعته واعتماده.";
                return RedirectToAction("BatchDetails", new { batchRef = batchReference });
            }

            // إعادة التعبئة عند الخطأ
            ViewBag.AidTypeId = new SelectList(db.SystemLookups.Where(x => x.Category == "FinancialAidType"), "Id", "Name", model.AidTypeId);
            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name", model.CurrencyId);
            ViewBag.StatusList = new SelectList(db.ApplicationStatuses, "Id", "Name");
            return View(model);
        }

        // تفاصيل الكشف الجماعي
        public ActionResult BatchDetails(string batchRef)
        {
            var aids = db.LawyerFinancialAids
                .Include(a => a.Lawyer)
                .Include(a => a.AidType)
                .Include(a => a.Currency)
                .Where(a => a.BatchReference == batchRef)
                .ToList();

            ViewBag.BatchRef = batchRef;
            ViewBag.SourceBankAccounts = new SelectList(db.BankAccounts.Where(b => b.IsActive), "Id", "BankName");

            return View(aids);
        }

        // اعتماد الكشف الجماعي (تنفيذ القيود)
        [HttpPost]
        public ActionResult ApproveBatch(string batchRef, int sourceBankAccountId)
        {
            var aids = db.LawyerFinancialAids.Include(a => a.Lawyer).Include(a => a.AidType).Include(a => a.Currency)
                         .Where(a => a.BatchReference == batchRef && !a.IsPaid).ToList();

            if (!aids.Any()) return Json(new { success = false, message = "لا يوجد قيود للاعتماد أو تم اعتمادها مسبقاً." });

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var accountingService = new AccountingService();
                    var userId = (int)Session["UserId"];

                    var bankAccountObj = db.BankAccounts.Find(sourceBankAccountId);
                    var accountingBankId = db.Accounts.FirstOrDefault(a => a.Name.Contains(bankAccountObj.BankName) && a.AccountType == AccountType.Asset)?.Id ?? 0;

                    if (accountingBankId == 0) throw new Exception("لم يتم العثور على حساب محاسبي مطابق للبنك.");

                    foreach (var aid in aids)
                    {
                        var expense = new BarExpense
                        {
                            ExpenseDate = DateTime.Now,
                            Amount = aid.Amount,
                            BankAccountId = sourceBankAccountId,
                            ExpenseCategory = "مساعدات مالية",
                            Description = $"صرف مساعدة جماعية ({batchRef}) - {aid.Lawyer.ArabicName}"
                        };
                        db.BarExpenses.Add(expense);
                        db.SaveChanges();

                        aid.IsPaid = true;
                        aid.PaymentDate = DateTime.Now;
                        aid.ExpenseId = expense.Id;
                        db.Entry(aid).State = EntityState.Modified;

                        // القيد المحاسبي
                        accountingService.GenerateEntryForFinancialAid(aid.Id, accountingBankId, userId);
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "تم اعتماد الكشف وإنشاء القيود المالية بنجاح." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
                }
            }
        }

        // ============================================================
        // 5. التقارير (Reports & Export)
        // ============================================================
        public ActionResult PrintBankTransfer(string batchRef, int sourceBankAccountId)
        {
            var aids = db.LawyerFinancialAids
                .Include(a => a.Lawyer).Include(a => a.Currency)
                .Where(a => a.BatchReference == batchRef && a.DisbursementMethod == "BankTransfer")
                .ToList();

            var sourceAccount = db.BankAccounts.Find(sourceBankAccountId);

            var model = new BankTransferReportViewModel
            {
                Date = DateTime.Now,
                BatchReference = batchRef,
                SourceBankName = sourceAccount.BankName,
                SourceAccountNumber = sourceAccount.AccountNumber,
                SourceIBAN = sourceAccount.Iban,
                TotalAmount = aids.Sum(x => x.Amount),
                CurrencySymbol = aids.FirstOrDefault()?.Currency?.Symbol ?? "",
                Beneficiaries = aids
            };

            return View(model);
        }

        public ActionResult ExportBankExcel(string batchRef)
        {
            var aids = db.LawyerFinancialAids
                .Include(a => a.Lawyer).Include(a => a.Currency)
                .Where(a => a.BatchReference == batchRef && a.DisbursementMethod == "BankTransfer")
                .ToList();

         ///   ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add("Bank Transfer");

                // ترويسة
                ws.Cells[1, 1].Value = "اسم المستفيد";
                ws.Cells[1, 2].Value = "رقم الهوية";
                ws.Cells[1, 3].Value = "البنك";
                ws.Cells[1, 4].Value = "الفرع";
                ws.Cells[1, 5].Value = "رقم الحساب / IBAN";
                ws.Cells[1, 6].Value = "المبلغ";
                ws.Cells[1, 7].Value = "العملة";

                int row = 2;
                foreach (var item in aids)
                {
                    ws.Cells[row, 1].Value = item.Lawyer.ArabicName;
                    ws.Cells[row, 2].Value = item.Lawyer.NationalIdNumber;
                    ws.Cells[row, 3].Value = item.TargetBankName;
                    ws.Cells[row, 4].Value = item.TargetBankBranch;
                    ws.Cells[row, 5].Value = !string.IsNullOrEmpty(item.TargetIban) ? item.TargetIban : "---";
                    ws.Cells[row, 6].Value = item.Amount;
                    ws.Cells[row, 7].Value = item.Currency?.Symbol;
                    row++;
                }
                ws.Cells.AutoFitColumns();
                var stream = new MemoryStream();
                package.SaveAs(stream);
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Transfer_{batchRef}.xlsx");
            }
        }

        // ============================================================
        // 6. AJAX Helpers
        // ============================================================
        [HttpGet]
        public JsonResult GetLawyersByStatus(int statusId)
        {
            var lawyers = db.GraduateApplications
                            .Where(g => g.ApplicationStatusId == statusId)
                            .Select(g => new {
                                id = g.Id,
                                name = g.ArabicName,
                                nationalId = g.NationalIdNumber,
                                bank = g.BankName ?? "لا يوجد",
                                iban = g.Iban,
                                wallet = g.WalletNumber
                            })
                            .ToList();
            return Json(lawyers, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult SearchLawyerByName(string term)
        {
            var lawyers = db.GraduateApplications
                            .Where(g => g.ArabicName.Contains(term) || g.NationalIdNumber.Contains(term))
                            .Take(10)
                            .Select(g => new {
                                id = g.Id,
                                text = g.ArabicName + " (" + g.NationalIdNumber + ")",
                                bank = g.BankName ?? "لا يوجد",
                                iban = g.Iban,
                                wallet = g.WalletNumber
                            })
                            .ToList();
            return Json(lawyers, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetLawyerDetails(int id)
        {
            var lawyer = db.GraduateApplications.Find(id);
            if (lawyer == null) return Json(null, JsonRequestBehavior.AllowGet);
            return Json(new
            {
                bankName = lawyer.BankName,
                branch = lawyer.BankBranch,
                iban = lawyer.Iban,
                wallet = lawyer.WalletNumber
            }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}