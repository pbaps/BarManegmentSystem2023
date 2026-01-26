using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class FeeTypesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/FeeTypes
        public ActionResult Index()
        {
            var feeTypes = db.FeeTypes
                .Include(f => f.BankAccount)
                .Include(f => f.Currency)
                .Include(f => f.RevenueAccount); // ✅ تضمين حساب الإيراد
            return View(feeTypes.ToList());
        }

        // GET: Admin/FeeTypes/Create
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            ViewBag.BankAccountId = new SelectList(db.BankAccounts, "Id", "BankName");
            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name");

            // ✅ جلب حسابات الإيرادات فقط (التي تبدأ بـ 4 وهي حركية)
            var revenueAccounts = db.Accounts
                .Where(a => a.Code.StartsWith("4") && a.IsTransactional)
                .Select(a => new { a.Id, Name = a.Code + " - " + a.Name })
                .ToList();
            ViewBag.RevenueAccountId = new SelectList(revenueAccounts, "Id", "Name");

            return View();
        }

        // POST: Admin/FeeTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(FeeType feeType)
        {
            if (ModelState.IsValid)
            {
                db.FeeTypes.Add(feeType);
                db.SaveChanges();
                AuditService.LogAction("Create FeeType", "FeeTypes", $"Added {feeType.Name}");
                return RedirectToAction("Index");
            }

            ViewBag.BankAccountId = new SelectList(db.BankAccounts, "Id", "BankName", feeType.BankAccountId);
            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name", feeType.CurrencyId);

            var revenueAccounts = db.Accounts
                .Where(a => a.Code.StartsWith("4") && a.IsTransactional)
                .Select(a => new { a.Id, Name = a.Code + " - " + a.Name })
                .ToList();
            ViewBag.RevenueAccountId = new SelectList(revenueAccounts, "Id", "Name", feeType.RevenueAccountId);

            return View(feeType);
        }

        // GET: Admin/FeeTypes/Edit/5
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var feeType = db.FeeTypes.Find(id);
            if (feeType == null) return HttpNotFound();

            ViewBag.BankAccountId = new SelectList(db.BankAccounts, "Id", "BankName", feeType.BankAccountId);
            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name", feeType.CurrencyId);

            // ✅ جلب حسابات الإيرادات
            var revenueAccounts = db.Accounts
                .Where(a => a.Code.StartsWith("4") && a.IsTransactional)
                .Select(a => new { a.Id, Name = a.Code + " - " + a.Name })
                .ToList();
            ViewBag.RevenueAccountId = new SelectList(revenueAccounts, "Id", "Name", feeType.RevenueAccountId);

            return View(feeType);
        }

        // POST: Admin/FeeTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(FeeType feeType)
        {
            if (ModelState.IsValid)
            {
                db.Entry(feeType).State = EntityState.Modified;
                db.SaveChanges();
                AuditService.LogAction("Edit FeeType", "FeeTypes", $"Updated {feeType.Name}");
                return RedirectToAction("Index");
            }
            ViewBag.BankAccountId = new SelectList(db.BankAccounts, "Id", "BankName", feeType.BankAccountId);
            ViewBag.CurrencyId = new SelectList(db.Currencies, "Id", "Name", feeType.CurrencyId);

            var revenueAccounts = db.Accounts
                .Where(a => a.Code.StartsWith("4") && a.IsTransactional)
                .Select(a => new { a.Id, Name = a.Code + " - " + a.Name })
                .ToList();
            ViewBag.RevenueAccountId = new SelectList(revenueAccounts, "Id", "Name", feeType.RevenueAccountId);

            return View(feeType);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}