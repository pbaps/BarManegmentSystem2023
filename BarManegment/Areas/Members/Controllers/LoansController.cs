using BarManegment.Models;
using BarManegment.Areas.Members.ViewModels;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;

namespace BarManegment.Areas.Members.Controllers
{
    [Authorize]
    public class LoansController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. قائمة قروضي (Index)
        public ActionResult Index()
        {
            var userId = (int)Session["UserId"];
            var lawyer = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);
            if (lawyer == null) return RedirectToAction("Login", "Account");

            var myLoans = db.LoanApplications
                .Include(l => l.LoanType)
                .Where(l => l.LawyerId == lawyer.Id)
                .OrderByDescending(l => l.ApplicationDate)
                .ToList();

            return View(myLoans);
        }

        // 2. تفاصيل القرض والأقساط (Details)
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var userId = (int)Session["UserId"];
            var lawyer = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            var loan = db.LoanApplications
                .Include(l => l.LoanType)
                .Include(l => l.Installments)
                .FirstOrDefault(l => l.Id == id && l.LawyerId == lawyer.Id);

            if (loan == null) return HttpNotFound();

            return View(loan);
        }

        // 3. تقديم طلب جديد (Create - GET)
        public ActionResult Create()
        {
            var userId = (int)Session["UserId"];
            var lawyer = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            // التحقق: هل يوجد قرض قائم لم يتم سداده؟
            bool hasActiveLoan = db.LoanApplications.Any(l => l.LawyerId == lawyer.Id && l.IsDisbursed && l.Status != "مسدد بالكامل");
            if (hasActiveLoan)
            {
                TempData["ErrorMessage"] = "عذراً، لا يمكنك تقديم طلب جديد لوجود قرض قائم لم يتم سداده بالكامل.";
                return RedirectToAction("Index");
            }

            // التحقق: هل يوجد طلب قيد المراجعة؟
            bool hasPendingRequest = db.LoanApplications.Any(l => l.LawyerId == lawyer.Id && (l.Status == "جديد" || l.Status == "قيد المراجعة"));
            if (hasPendingRequest)
            {
                TempData["ErrorMessage"] = "لديك طلب قرض قيد المعالجة حالياً.";
                return RedirectToAction("Index");
            }

           
           
            ViewBag.LoanTypeId = new SelectList(db.LoanTypes.ToList(), "Id", "Name");

            return View(new LoanApplicationCreateViewModel());
        }

        // 4. تقديم طلب جديد (Create - POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(LoanApplicationCreateViewModel model)
        {
            var userId = (int)Session["UserId"];
            var lawyer = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            if (ModelState.IsValid)
            {
                // التحقق من حدود نوع القرض
                var loanType = db.LoanTypes.Find(model.LoanTypeId);
                if (loanType != null)
                {
                    if (model.Amount > loanType.MaxAmount)
                    {
                        ModelState.AddModelError("Amount", $"المبلغ المطلوب يتجاوز الحد الأقصى لهذا النوع ({loanType.MaxAmount})");
                    }
                    else if (model.InstallmentCount > loanType.MaxInstallments)
                    {
                        ModelState.AddModelError("InstallmentCount", $"عدد الأقساط يتجاوز الحد المسموح ({loanType.MaxInstallments})");
                    }
                    else
                    {
                        // الحفظ
                        var application = new LoanApplication
                        {
                            LawyerId = lawyer.Id,
                            LoanTypeId = model.LoanTypeId,
                            Amount = model.Amount,
                            InstallmentCount = model.InstallmentCount,
                            ApplicationDate = DateTime.Now,
                            Status = "جديد",
                            Notes = model.Notes,
                            IsDisbursed = false
                        };

                        db.LoanApplications.Add(application);
                        db.SaveChanges();

                        TempData["SuccessMessage"] = "تم تقديم طلب القرض بنجاح وسيتم عرضه على اللجنة.";
                        return RedirectToAction("Index");
                    }
                }
            }

 
            // نفس التعديل في دالة الـ POST عند إعادة عرض الصفحة في حال الخطأ
            ViewBag.LoanTypeId = new SelectList(db.LoanTypes.ToList(), "Id", "Name", model.LoanTypeId);
            return View(model);
        }
    }
}