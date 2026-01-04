using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "FinancialReports")] // أو صلاحية جديدة "CheckManagement"
    public class CheckPortfolioController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // عرض الشيكات (مع فلاتر)
        public ActionResult Index(string status = "UnderCollection")
        {
            var query = db.ChecksPortfolio.Include(c => c.Currency).AsQueryable();

            if (status == "UnderCollection")
                query = query.Where(c => c.Status == CheckStatus.UnderCollection);
            else if (status == "Collected")
                query = query.Where(c => c.Status == CheckStatus.Collected);
            else if (status == "Bounced")
                query = query.Where(c => c.Status == CheckStatus.Bounced);

            ViewBag.CurrentStatus = status;
            return View(query.OrderBy(c => c.DueDate).ToList());
        }

        // نافذة التحصيل (Modal)
        public ActionResult Collect(int id)
        {
            var check = db.ChecksPortfolio.Find(id);
            if (check == null) return HttpNotFound();

            // قائمة حسابات البنوك للإيداع فيها (حسابات الأصول 1102)
            var bankAccounts = db.Accounts
                .Where(a => a.IsTransactional && a.Code.StartsWith("1102"))
                .Select(a => new { a.Id, Text = a.Name })
                .ToList();

            ViewBag.TargetBankAccountId = new SelectList(bankAccounts, "Id", "Text");
            ViewBag.CheckInfo = $"شيك رقم {check.CheckNumber} - {check.Amount} {(check.Currency?.Symbol ?? "₪")}";

            return PartialView("_CollectModal", new CheckActionViewModel { CheckId = id, ActionDate = DateTime.Now });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Collect(CheckActionViewModel model)
        {
            using (var service = new AccountingService())
            {
                try
                {
                    bool success = service.CollectCheck(model.CheckId, model.TargetBankAccountId, model.ActionDate, (int)Session["UserId"]);
                    if (success)
                        TempData["SuccessMessage"] = "تم تحصيل الشيك وإيداعه في البنك بنجاح.";
                    else
                        TempData["ErrorMessage"] = "فشلت العملية.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "خطأ: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        // عرض نافذة الارتجاع
        public ActionResult Bounce(int id)
        {
            var check = db.ChecksPortfolio.Find(id);
            if (check == null) return HttpNotFound();

            return PartialView("_BounceModal", check);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmBounce(int id, string reason)
        {
            using (var service = new AccountingService())
            {
                bool success = service.BounceCheck(id, reason, (int)Session["UserId"]);
                if (success)
                    TempData["SuccessMessage"] = "تم تسجيل الشيك كمرتجع بنجاح.";
                else
                    TempData["ErrorMessage"] = "حدث خطأ أثناء العملية.";
            }
            return RedirectToAction("Index");
        }




    }

    public class CheckActionViewModel
    {
        public int CheckId { get; set; }
        public DateTime ActionDate { get; set; }
        public int TargetBankAccountId { get; set; } // حساب البنك المالي (Accounts Table)
    }
}