using BarManegment.Models;
using BarManegment.Helpers;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    // (نفترض أننا سننشئ صلاحية "LoanTypes" لاحقاً)
    [CustomAuthorize(Permission = "CanView")]
    public class LoanTypesController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // (دالة مساعدة لملء قائمة البنوك)
        private void PopulateBankAccounts(object selectedAccount = null)
        {
            // (جلب البنوك مع العملات لعرضها بشكل واضح)
            var bankAccounts = db.BankAccounts.Include(b => b.Currency)
                                .Where(b => b.IsActive)
                                .ToList()
                                .Select(b => new {
                                    Id = b.Id,
                                    DisplayText = $"{b.BankName} - {b.AccountName} ({b.Currency.Symbol})"
                                });
            ViewBag.BankAccountForRepaymentId = new SelectList(bankAccounts, "Id", "DisplayText", selectedAccount);
        }

        // GET: Admin/LoanTypes
        public ActionResult Index()
        {
            // (جلب اسم البنك المرتبط)
            var loanTypes = db.LoanTypes.Include(l => l.BankAccount.Currency).ToList();
            return View(loanTypes);
        }

        // GET: Admin/LoanTypes/Create
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            PopulateBankAccounts(); // (ملء قائمة البنوك)
            return View();
        }

        // POST: Admin/LoanTypes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create([Bind(Include = "Id,Name,BankAccountForRepaymentId,MaxAmount,MaxInstallments")] LoanType loanType)
        {
            if (ModelState.IsValid)
            {
                db.LoanTypes.Add(loanType);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم إضافة نوع القرض بنجاح.";
                return RedirectToAction("Index");
            }

            PopulateBankAccounts(loanType.BankAccountForRepaymentId); // (إعادة ملء القائمة عند الفشل)
            return View(loanType);
        }

        // GET: Admin/LoanTypes/Edit/5
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LoanType loanType = db.LoanTypes.Find(id);
            if (loanType == null)
            {
                return HttpNotFound();
            }
            PopulateBankAccounts(loanType.BankAccountForRepaymentId); // (ملء القائمة مع تحديد الخيار الحالي)
            return View(loanType);
        }

        // POST: Admin/LoanTypes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit([Bind(Include = "Id,Name,BankAccountForRepaymentId,MaxAmount,MaxInstallments")] LoanType loanType)
        {
            if (ModelState.IsValid)
            {
                db.Entry(loanType).State = EntityState.Modified;
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل نوع القرض بنجاح.";
                return RedirectToAction("Index");
            }
            PopulateBankAccounts(loanType.BankAccountForRepaymentId);
            return View(loanType);
        }

        // (يمكن إضافة دالة الحذف (Delete) لاحقاً إذا احتجت إليها)

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