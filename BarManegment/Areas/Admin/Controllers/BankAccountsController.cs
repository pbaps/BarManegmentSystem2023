using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class BankAccountsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            var bankAccounts = db.BankAccounts.Include(b => b.Currency).ToList();
            return View(bankAccounts);
        }
        // صلاحية "الإضافة" مطلوبة لعرض صفحة الإنشاء
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            var viewModel = new BankAccountViewModel
            {
                Currencies = new SelectList(db.Currencies, "Id", "Name")
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // صلاحية "الإضافة" مطلوبة لعرض صفحة الإنشاء
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(BankAccountViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var bankAccount = new BankAccount
                {
                    BankName = viewModel.BankName,
                    AccountName = viewModel.AccountName,
                    AccountNumber = viewModel.AccountNumber,
                    Iban = viewModel.Iban,
                    CurrencyId = viewModel.CurrencyId,
                    IsActive = viewModel.IsActive
                };
                db.BankAccounts.Add(bankAccount);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تمت إضافة حساب البنك بنجاح.";
                return RedirectToAction("Index");
            }

            viewModel.Currencies = new SelectList(db.Currencies, "Id", "Name", viewModel.CurrencyId);
            return View(viewModel);
        }
        // صلاحية "التعديل" مطلوبة لعرض صفحة التعديل
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BankAccount bankAccount = db.BankAccounts.Find(id);
            if (bankAccount == null)
            {
                return HttpNotFound();
            }

            var viewModel = new BankAccountViewModel
            {
                Id = bankAccount.Id,
                BankName = bankAccount.BankName,
                AccountName = bankAccount.AccountName,
                AccountNumber = bankAccount.AccountNumber,
                Iban = bankAccount.Iban,
                CurrencyId = bankAccount.CurrencyId,
                IsActive = bankAccount.IsActive,
                Currencies = new SelectList(db.Currencies, "Id", "Name", bankAccount.CurrencyId)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // صلاحية "التعديل" مطلوبة لعرض صفحة التعديل
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(BankAccountViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var bankAccount = new BankAccount
                {
                    Id = viewModel.Id,
                    BankName = viewModel.BankName,
                    AccountName = viewModel.AccountName,
                    AccountNumber = viewModel.AccountNumber,
                    Iban = viewModel.Iban,
                    CurrencyId = viewModel.CurrencyId,
                    IsActive = viewModel.IsActive
                };
                db.Entry(bankAccount).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل حساب البنك بنجاح.";
                return RedirectToAction("Index");
            }
            viewModel.Currencies = new SelectList(db.Currencies, "Id", "Name", viewModel.CurrencyId);
            return View(viewModel);
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
