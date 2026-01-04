using BarManegment.Models;
using BarManegment.Services;
using BarManegment.Helpers;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class GeneralExpensesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. عرض المصروفات
        public ActionResult Index()
        {
            var expenses = db.GeneralExpenses
                .Include(g => g.ExpenseAccount)
                .Include(g => g.TreasuryAccount)
                .OrderByDescending(e => e.ExpenseDate)
                .ThenByDescending(e => e.Id)
                .ToList();
            return View(expenses);
        }

        // 2. إنشاء سند صرف جديد (GET)
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            // تصفية الحسابات لتسهيل الاختيار
            ViewBag.ExpenseAccountId = new SelectList(db.Accounts.Where(a => a.Code.StartsWith("5")), "Id", "Name"); // المصروفات
            ViewBag.TreasuryAccountId = new SelectList(db.Accounts.Where(a => a.Code.StartsWith("1101") || a.Code.StartsWith("1102")), "Id", "Name"); // النقدية والبنوك
            ViewBag.CostCenterId = new SelectList(db.CostCenters, "Id", "Name");
            return View();
        }

        // 3. حفظ سند الصرف وإنشاء القيد (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(GeneralExpense generalExpense)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // إعداد البيانات الافتراضية
                        generalExpense.CreatedAt = DateTime.Now;
                        generalExpense.CreatedByUserId = (int)Session["UserId"];
                        generalExpense.IsPosted = false; // سيتم تحويلها لـ true بعد إنشاء القيد

                        db.GeneralExpenses.Add(generalExpense);
                        db.SaveChanges();

                        // ============================================================
                        // === 💡 التكامل المالي: إنشاء القيد الآلي 💡 ===
                        // ============================================================
                        bool entryCreated = false;
                        using (var accService = new AccountingService())
                        {
                            entryCreated = accService.GenerateEntryForExpense(generalExpense.Id, (int)Session["UserId"]);
                        }

                        if (!entryCreated)
                        {
                            throw new Exception("تم حفظ السند ولكن فشل إنشاء القيد المحاسبي. يرجى مراجعة الدليل المحاسبي.");
                        }

                        transaction.Commit();
                        TempData["SuccessMessage"] = $"تم حفظ سند الصرف رقم {generalExpense.VoucherNumber} وإنشاء القيد المحاسبي بنجاح.";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "خطأ أثناء الحفظ: " + ex.Message);
                    }
                }
            }

            // إعادة تعبئة القوائم عند الخطأ
            ViewBag.ExpenseAccountId = new SelectList(db.Accounts.Where(a => a.Code.StartsWith("5")), "Id", "Name", generalExpense.ExpenseAccountId);
            ViewBag.TreasuryAccountId = new SelectList(db.Accounts.Where(a => a.Code.StartsWith("1101") || a.Code.StartsWith("1102")), "Id", "Name", generalExpense.TreasuryAccountId);
            ViewBag.CostCenterId = new SelectList(db.CostCenters, "Id", "Name", generalExpense.CostCenterId);
            return View(generalExpense);
        }

        // 4. التفاصيل
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var generalExpense = db.GeneralExpenses.Include(g => g.ExpenseAccount).Include(g => g.TreasuryAccount).FirstOrDefault(e => e.Id == id);
            if (generalExpense == null) return HttpNotFound();
            return View(generalExpense);
        }

        // 5. الحذف (اختياري - يجب الحذر عند حذف سند مرحل)
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var generalExpense = db.GeneralExpenses.Find(id);
            if (generalExpense == null) return HttpNotFound();

            if (generalExpense.IsPosted)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف سند صرف تم ترحيله محاسبياً.";
                return RedirectToAction("Index");
            }
            return View(generalExpense);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var generalExpense = db.GeneralExpenses.Find(id);
            if (!generalExpense.IsPosted)
            {
                db.GeneralExpenses.Remove(generalExpense);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم الحذف بنجاح.";
            }
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}