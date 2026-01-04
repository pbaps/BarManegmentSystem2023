using BarManegment.Helpers;
using BarManegment.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "SystemSettings")] // أو FinancialReports
    public class FinancialSetupController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // === 1. إدارة دليل الحسابات (الأطراف المدينة والدائنة) ===

        public ActionResult AccountsIndex()
        {
            // عرض الحسابات بشكل مسطح مع ترتيب بالكود
            var accounts = db.Accounts.OrderBy(a => a.Code).ToList();
            return View(accounts);
        }

        public ActionResult CreateAccount()
        {
            // نمرر قائمة الحسابات الرئيسية فقط لتكون أباً للحساب الجديد
            ViewBag.ParentId = new SelectList(db.Accounts.Where(a => !a.IsTransactional), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateAccount(Account model)
        {
            if (ModelState.IsValid)
            {
                // تحديد المستوى تلقائياً بناءً على الأب
                if (model.ParentId.HasValue)
                {
                    var parent = db.Accounts.Find(model.ParentId);
                    model.Level = parent.Level + 1;
                }
                else
                {
                    model.Level = 1;
                }

                // الحسابات الجديدة غالباً تكون حركية (Transactional)
                model.IsTransactional = true;

                db.Accounts.Add(model);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم إضافة الحساب بنجاح.";
                return RedirectToAction("AccountsIndex");
            }

            ViewBag.ParentId = new SelectList(db.Accounts.Where(a => !a.IsTransactional), "Id", "Name", model.ParentId);
            return View(model);
        }

        public ActionResult EditAccount(int id)
        {
            var account = db.Accounts.Find(id);
            if (account == null) return HttpNotFound();

            ViewBag.ParentId = new SelectList(db.Accounts.Where(a => !a.IsTransactional && a.Id != id), "Id", "Name", account.ParentId);
            return View(account);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditAccount(Account model)
        {
            if (ModelState.IsValid)
            {
                var account = db.Accounts.Find(model.Id);
                account.Name = model.Name;
                account.Code = model.Code;
                account.ParentId = model.ParentId;
                // لا نغير الرصيد الافتتاحي أو النوع بسهولة للحفاظ على التكامل

                db.Entry(account).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل الحساب بنجاح.";
                return RedirectToAction("AccountsIndex");
            }
            ViewBag.ParentId = new SelectList(db.Accounts.Where(a => !a.IsTransactional && a.Id != model.Id), "Id", "Name", model.ParentId);
            return View(model);
        }


        // === 2. إدارة مراكز التكلفة ===

        public ActionResult CostCentersIndex()
        {
            return View(db.CostCenters.OrderBy(c => c.Code).ToList());
        }

        public ActionResult CreateCostCenter()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCostCenter(CostCenter model)
        {
            if (ModelState.IsValid)
            {
                db.CostCenters.Add(model);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم إضافة مركز التكلفة.";
                return RedirectToAction("CostCentersIndex");
            }
            return View(model);
        }

        public ActionResult EditCostCenter(int id)
        {
            var cc = db.CostCenters.Find(id);
            if (cc == null) return HttpNotFound();
            return View(cc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCostCenter(CostCenter model)
        {
            if (ModelState.IsValid)
            {
                db.Entry(model).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم التعديل بنجاح.";
                return RedirectToAction("CostCentersIndex");
            }
            return View(model);
        }
    }
}