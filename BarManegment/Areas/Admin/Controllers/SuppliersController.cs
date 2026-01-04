using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using BarManegment.Models;
using BarManegment.Helpers;
using System.Net;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class SuppliersController : BaseController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View(db.Suppliers.Include(s => s.Account).ToList());
        }

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            // نرسل قائمة بحسابات الموردين (الخصوم المتداولة - 2101)
            ViewBag.AccountId = new SelectList(db.Accounts.Where(a => a.Code.StartsWith("2101") && a.IsTransactional), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                db.Suppliers.Add(supplier);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AccountId = new SelectList(db.Accounts.Where(a => a.Code.StartsWith("2101") && a.IsTransactional), "Id", "Name", supplier.AccountId);
            return View(supplier);
        }

        // GET: Edit
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var supplier = db.Suppliers.Find(id);
            if (supplier == null) return HttpNotFound();

            ViewBag.AccountId = new SelectList(db.Accounts.Where(a => a.Code.StartsWith("2101") && a.IsTransactional), "Id", "Name", supplier.AccountId);
            return View(supplier);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                db.Entry(supplier).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AccountId = new SelectList(db.Accounts.Where(a => a.Code.StartsWith("2101") && a.IsTransactional), "Id", "Name", supplier.AccountId);
            return View(supplier);
        }

        // POST: Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int id)
        {
            var supplier = db.Suppliers.Find(id);
            if (supplier != null)
            {
                if (db.PurchaseInvoices.Any(p => p.SupplierId == id))
                {
                    TempData["ErrorMessage"] = "لا يمكن حذف المورد لوجود فواتير مرتبطة به.";
                }
                else
                {
                    db.Suppliers.Remove(supplier);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "تم الحذف بنجاح.";
                }
            }
            return RedirectToAction("Index");
        }
    }
}