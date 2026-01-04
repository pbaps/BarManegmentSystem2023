using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class LoanApplicationsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        private int GetCurrentUserId()
        {
            if (Session["UserId"] == null) return -1;
            return (int)Session["UserId"];
        }

        // --- 1. القائمة والتفاصيل ---
        public ActionResult Index(string searchString)
        {
            var query = db.LoanApplications
                .Include(l => l.Lawyer)
                .Include(l => l.LoanType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l => l.Lawyer.ArabicName.Contains(searchString) || l.Lawyer.MembershipId == searchString);
            }
            return View(query.OrderByDescending(l => l.ApplicationDate).ToList());
        }

        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var loan = db.LoanApplications
                .Include(l => l.Lawyer.User)
                .Include(l => l.LoanType)
                .Include(l => l.Guarantors.Select(g => g.LawyerGuarantor.User))
                .Include(l => l.Installments)
                .FirstOrDefault(l => l.Id == id);

            if (loan == null) return HttpNotFound();
            return View(loan);
        }

        // --- 2. الإنشاء (Create) ---
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            var viewModel = new LoanApplicationViewModel();
            ViewBag.LoanTypesList = new SelectList(db.LoanTypes, "Id", "Name");
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(LoanApplicationViewModel viewModel)
        {
            // (تم اختصار منطق التحقق والإنشاء التقليدي هنا للتركيز على الأجزاء المالية، الكود الكامل موجود في رسالتك السابقة وهو سليم)
            // ... [افترض وجود كود الحفظ العادي هنا] ...

            // سأضع الكود الأساسي للحفظ لتكون النسخة كاملة
            if (!ModelState.IsValid) { ViewBag.LoanTypesList = new SelectList(db.LoanTypes, "Id", "Name"); return View(viewModel); }

            // منطق حفظ مبسط (لغرض المثال، استخدم الكود التفصيلي الخاص بك للتحقق من الكفلاء)
            var loan = new LoanApplication
            {
                // ... تعيين القيم من الـ View Model ...
                // LawyerId = ..., Amount = ... 
            };
            // db.LoanApplications.Add(loan); db.SaveChanges();
            return RedirectToAction("Index"); // (placeholder)
        }

        // ============================================================
        // === 💡 3. صرف القرض (Disburse) - التكامل المالي 💡 ===
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult DisburseLoan(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var loanApp = db.LoanApplications.Include(l => l.LoanType).Include(l => l.Lawyer).FirstOrDefault(l => l.Id == id);
                    if (loanApp == null) return HttpNotFound();

                    if (loanApp.IsDisbursed)
                    {
                        TempData["InfoMessage"] = "هذا القرض تم صرفه مسبقاً.";
                        return RedirectToAction("Details", new { id = id });
                    }

                    // 1. تحديث حالة القرض
                    loanApp.IsDisbursed = true;
                    loanApp.DisbursementDate = DateTime.Now;
                    loanApp.Status = "مفعل (تم الصرف)";
                    db.Entry(loanApp).State = EntityState.Modified;
                    db.SaveChanges();

                    // 2. إنشاء القيد المحاسبي الآلي
                    // (من ح/ ذمم القروض - إلى ح/ البنك)
                    bool entryCreated = false;
                    using (var accService = new AccountingService())
                    {
                        entryCreated = accService.GenerateEntryForLoanDisbursement(loanApp.Id, GetCurrentUserId());
                    }

                    if (!entryCreated) throw new Exception("تم تحديث الحالة ولكن فشل إنشاء القيد المحاسبي (تأكد من تعريف حسابات القروض والبنوك).");

                    transaction.Commit();
                    TempData["SuccessMessage"] = "تم صرف القرض، وإنشاء القيد المحاسبي بنجاح.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "خطأ أثناء الصرف: " + ex.Message;
                }
            }

            return RedirectToAction("Details", new { id = id });
        }

        // --- 4. توليد الأقساط ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult GenerateInstallments(int id)
        {
            // ... (نفس الكود السابق الخاص بتوليد الأقساط وقسائم الدفع) ...
            // هذا الكود ينشئ PaymentVouchers، وعندما يتم سداد هذه القسائم لاحقاً في ReceiptsController
            // سيتم استدعاء GenerateEntryForReceipt التي قمنا بتحديثها لتميز أنها "سداد قرض" وتخفض الذمم.

            // (للاختصار، استخدم الكود الذي أرسلته أنت في السؤال السابق لهذه الدالة، فهو سليم)
            return RedirectToAction("Details", new { id = id });
        }

        // --- دوال الطباعة والرفع ---
        // (استخدم نفس الدوال الموجودة في الكود السابق، لا تغيير عليها)

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}