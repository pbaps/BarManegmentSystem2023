using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services; // ✅ ضروري للتدقيق
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    // ✅ 1. الصلاحية الخاصة
    [CustomAuthorize(Permission = "LawyerFinancialData")]
    public class LawyerFinancialDataController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // 1. صفحة البحث الرئيسية
        public ActionResult Index()
        {
            return View();
        }

        // 2. البحث عن محامي (AJAX)
        [HttpGet]
        public ActionResult SearchLawyer(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return PartialView("_LawyerSearchResult", null);

            var lawyers = db.GraduateApplications
                .Where(g => g.ArabicName.Contains(term) ||
                            g.NationalIdNumber.Contains(term) ||
                            g.MembershipId.Contains(term))
                .Select(g => new {
                    g.Id,
                    g.ArabicName,
                    g.NationalIdNumber,
                    g.MembershipId,
                    Status = g.ApplicationStatus.Name
                })
                .Take(20)
                .ToList();

            return PartialView("_LawyerSearchResult", lawyers);
        }

        // 3. صفحة تعديل البيانات (GET)
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var lawyer = db.GraduateApplications.Find(id);
            if (lawyer == null) return HttpNotFound();

            var model = new LawyerFinancialDataViewModel
            {
                LawyerId = lawyer.Id,
                LawyerName = lawyer.ArabicName,
                NationalId = lawyer.NationalIdNumber,
                BankName = lawyer.BankName,
                BankBranch = lawyer.BankBranch,
                AccountNumber = lawyer.AccountNumber,
                Iban = lawyer.Iban,
                WalletNumber = lawyer.WalletNumber,
                WalletProviderId = lawyer.WalletProviderId
            };

            ViewBag.WalletProviderId = new SelectList(db.SystemLookups.Where(x => x.Category == "WalletProvider"), "Id", "Name", model.WalletProviderId);

            return View(model);
        }

        // 4. حفظ البيانات (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(LawyerFinancialDataViewModel model)
        {
            if (ModelState.IsValid)
            {
                var lawyer = db.GraduateApplications.Find(model.LawyerId);
                if (lawyer == null) return HttpNotFound();

                // حفظ القيم القديمة للمقارنة في التدقيق (اختياري، لكن مفيد)
                string oldBankInfo = $"Bank: {lawyer.BankName}, IBAN: {lawyer.Iban}";

                // تحديث البيانات
                lawyer.BankName = model.BankName;
                lawyer.BankBranch = model.BankBranch;
                lawyer.AccountNumber = model.AccountNumber;
                lawyer.Iban = model.Iban;
                lawyer.WalletNumber = model.WalletNumber;
                lawyer.WalletProviderId = model.WalletProviderId;

                db.Entry(lawyer).State = EntityState.Modified;
                db.SaveChanges();

                // ✅ 2. تسجيل التدقيق (Audit Log)
                AuditService.LogAction(
                    "UpdateLawyerFinance",
                    "LawyerFinancialData",
                    $"تم تعديل البيانات المالية للمحامي: {lawyer.ArabicName} (ID: {lawyer.Id}). البيانات القديمة: {oldBankInfo}"
                );

                TempData["SuccessMessage"] = "تم تحديث البيانات المالية للمحامي بنجاح.";
                return RedirectToAction("Index");
            }

            ViewBag.WalletProviderId = new SelectList(db.SystemLookups.Where(x => x.Category == "WalletProvider"), "Id", "Name", model.WalletProviderId);
            return View(model);
        }
    }
}