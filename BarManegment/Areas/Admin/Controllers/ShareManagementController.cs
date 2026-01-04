using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Collections.Generic; // (مطلوب لـ List)
using System; // (مطلوب لـ Exception)

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanEdit")]
    public class ShareManagementController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // --- 1. البحث عن المحامي ---
        // GET: Admin/ShareManagement
        public ActionResult Index(string searchString)
        {
            IQueryable<GraduateApplication> lawyersQuery = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Include(g => g.User) // (يفضل إضافة Include هنا أيضاً للبحث)
                .Where(g => g.ApplicationStatus.Name == "محامي مزاول");

            if (!string.IsNullOrEmpty(searchString))
            {
                lawyersQuery = lawyersQuery.Where(g => g.ArabicName.Contains(searchString) ||
                                                       g.MembershipId.Contains(searchString) ||
                                                       (g.User != null && g.User.IdentificationNumber == searchString));
            }

            var lawyers = lawyersQuery.OrderBy(g => g.ArabicName).Take(20).ToList();
            ViewBag.SearchString = searchString;

            return View(lawyers);
        }

        // --- 2. عرض شاشة إدارة الحصص للمحامي ---
        // GET: Admin/ShareManagement/Manage/5
        public ActionResult Manage(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // ✅ === بداية التعديل === ✅
            // (يجب استخدام Include لجلب بيانات المستخدم المرتبطة)
            var lawyer = db.GraduateApplications
                .Include(g => g.User) // 💡 (الإضافة الأهم لحل الخطأ)
                .FirstOrDefault(g => g.Id == id);
            // ✅ === نهاية التعديل === ✅

            if (lawyer == null)
            {
                return HttpNotFound();
            }

            // (جلب الحصص - الكود سليم)
            var shares = db.FeeDistributions
                .Include(f => f.Receipt)
                .Include(f => f.ContractTransaction.ContractType)
                .Where(f => f.LawyerId == id && f.IsSentToBank == false)
                .OrderByDescending(f => f.Receipt.BankPaymentDate)
                .ToList();

            var viewModel = new LawyerShareDetailsViewModel
            {
                Lawyer = lawyer,
                Shares = shares,
                HoldReason = "مستحقات قرض / ذمم مالية" // (سبب افتراضي)
            };

            return View(viewModel);
        }

        // --- 3. تنفيذ الحجز (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HoldShare(int shareId, string holdReason)
        {
            var share = db.FeeDistributions.Find(shareId);
            if (share == null || share.IsSentToBank)
            {
                TempData["ErrorMessage"] = "لا يمكن حجز هذه الحصة (إما غير موجودة أو أرسلت للبنك).";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(holdReason))
            {
                TempData["ErrorMessage"] = "الرجاء تحديد سبب الحجز.";
                return RedirectToAction("Manage", new { id = share.LawyerId });
            }

            share.IsOnHold = true;
            share.HoldReason = holdReason;
            db.Entry(share).State = EntityState.Modified;
            db.SaveChanges();

            TempData["SuccessMessage"] = "تم حجز المعاملة بنجاح.";
            return RedirectToAction("Manage", new { id = share.LawyerId });
        }

        // --- 4. إلغاء الحجز (POST) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReleaseShare(int shareId)
        {
            var share = db.FeeDistributions.Find(shareId);
            if (share == null)
            {
                return HttpNotFound();
            }

            share.IsOnHold = false;
            share.HoldReason = null;
            db.Entry(share).State = EntityState.Modified;
            db.SaveChanges();

            TempData["SuccessMessage"] = "تم إلغاء الحجز عن المعاملة بنجاح.";
            return RedirectToAction("Manage", new { id = share.LawyerId });
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