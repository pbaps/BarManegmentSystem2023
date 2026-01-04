using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize]
    public class FeeTypesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // (Index: يبقى كما هو، التعديل في الواجهة)
        public ActionResult Index()
        {
            var feeTypes = db.FeeTypes.Include(f => f.Currency).Include(f => f.BankAccount).ToList();
            return View(feeTypes);
        }

        // (Create GET: يبقى كما هو)
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            var viewModel = new FeeTypeViewModel
            {
                Currencies = new SelectList(db.Currencies, "Id", "Name"),
                BankAccounts = new SelectList(db.BankAccounts.Where(b => b.IsActive), "Id", "AccountName"),
                // (قيم افتراضية للنسب)
                LawyerPercentage = 0.0m,
                BarSharePercentage = 1.0m // (100% للنقابة افتراضياً)
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(FeeTypeViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var feeType = new FeeType
                {
                    Name = viewModel.Name,
                    DefaultAmount = viewModel.DefaultAmount,
                    CurrencyId = viewModel.CurrencyId,
                    BankAccountId = viewModel.BankAccountId,
                    IsActive = viewModel.IsActive,

                    // --- ⬇️ ⬇️ بداية الإضافة ⬇️ ⬇️ ---
                    LawyerPercentage = viewModel.LawyerPercentage,
                    BarSharePercentage = viewModel.BarSharePercentage
                    // --- ⬆️ ⬆️ نهاية الإضافة ⬆️ ⬆️ ---
                };
                db.FeeTypes.Add(feeType);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تمت إضافة نوع الرسم بنجاح.";
                return RedirectToAction("Index");
            }

            viewModel.Currencies = new SelectList(db.Currencies, "Id", "Name", viewModel.CurrencyId);
            viewModel.BankAccounts = new SelectList(db.BankAccounts.Where(b => b.IsActive), "Id", "AccountName", viewModel.BankAccountId);
            return View(viewModel);
        }

        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            FeeType feeType = db.FeeTypes.Find(id);
            if (feeType == null)
            {
                return HttpNotFound();
            }

            var viewModel = new FeeTypeViewModel
            {
                Id = feeType.Id,
                Name = feeType.Name,
                DefaultAmount = feeType.DefaultAmount,
                CurrencyId = feeType.CurrencyId,
                BankAccountId = feeType.BankAccountId,
                IsActive = feeType.IsActive,

                // --- ⬇️ ⬇️ بداية الإضافة ⬇️ ⬇️ ---
                LawyerPercentage = feeType.LawyerPercentage,
                BarSharePercentage = feeType.BarSharePercentage,
                // --- ⬆️ ⬆️ نهاية الإضافة ⬆️ ⬆️ ---

                Currencies = new SelectList(db.Currencies, "Id", "Name", feeType.CurrencyId),
                BankAccounts = new SelectList(db.BankAccounts.Where(b => b.IsActive), "Id", "AccountName", feeType.BankAccountId)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(FeeTypeViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var feeType = new FeeType
                {
                    Id = viewModel.Id,
                    Name = viewModel.Name,
                    DefaultAmount = viewModel.DefaultAmount,
                    CurrencyId = viewModel.CurrencyId,
                    BankAccountId = viewModel.BankAccountId,
                    IsActive = viewModel.IsActive,

                    // --- ⬇️ ⬇️ بداية الإضافة ⬇️ ⬇️ ---
                    LawyerPercentage = viewModel.LawyerPercentage,
                    BarSharePercentage = viewModel.BarSharePercentage
                    // --- ⬆️ ⬆️ نهاية الإضافة ⬆️ ⬆️ ---
                };
                db.Entry(feeType).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل نوع الرسم بنجاح.";
                return RedirectToAction("Index");
            }
            viewModel.Currencies = new SelectList(db.Currencies, "Id", "Name", viewModel.CurrencyId);
            viewModel.BankAccounts = new SelectList(db.BankAccounts.Where(b => b.IsActive), "Id", "AccountName", viewModel.BankAccountId);
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