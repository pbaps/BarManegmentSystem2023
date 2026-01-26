using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class OfficialReportsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // ✅ دالة مساعدة: لجلب قائمة الموقعين (أعضاء المجلس)
        // ============================================================
        private void PopulateSigners()
        {
            var members = db.CouncilMembers
                .Where(m => m.IsActive)
                .Select(m => new
                {
                    // ندمج الاسم والصفة بفاصل | ليفهمها كود الجافاسكريبت في الواجهة
                    Value = m.Name + "|" + m.Title,
                    Text = m.Title + " (" + m.Name + ")"
                })
                .ToList();

            ViewBag.SignersList = new SelectList(members, "Value", "Text");
        }

        // ============================================================
        // 1. الصفحة الرئيسية (جدول المحامين)
        // ============================================================
        public ActionResult Index()
        {
            var reportData = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Include(g => g.ContactInfo)
                .OrderByDescending(g => g.Id)
                .ToList();

            return View(reportData);
        }

        // ============================================================
        // 2. طباعة الإفادات (مزاولة / تدريب)
        // ============================================================
        public ActionResult PrintStatement(int? id, string type)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var application = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Include(g => g.Supervisor)
                .FirstOrDefault(g => g.Id == id);

            if (application == null) return HttpNotFound();

            // ✅ استدعاء قائمة الموقعين
            PopulateSigners();

            if (type == "Practicing")
            {
                if (!application.ApplicationStatus.Name.Contains("مزاول"))
                    return Content("عذراً، هذا العضو ليس محامياً مزاولاً.");

                return View("PrintStatement_Practicing", application);
            }
            else if (type == "Trainee")
            {
                if (!application.ApplicationStatus.Name.Contains("متدرب"))
                    return Content("عذراً، هذا العضو ليس متدرباً.");

                return View("PrintStatement_Trainee", application);
            }

            return HttpNotFound();
        }

        // ============================================================
        // 3. طباعة كتاب البنك
        // ============================================================
        public ActionResult PrintBankLetter(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var application = db.GraduateApplications.Find(id);
            if (application == null) return HttpNotFound();

            // ✅ استدعاء قائمة الموقعين
            PopulateSigners();

            return View("PrintBankLetter", application);
        }

        // ============================================================
        // 4. طباعة التقرير الشامل (بروفايل)
        // ============================================================
        public ActionResult PrintComprehensiveProfile(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var application = db.GraduateApplications
                .Include(g => g.ContactInfo)
                .Include(g => g.Qualifications.Select(q => q.QualificationType))
                .Include(g => g.Supervisor)
                .Include(g => g.ApplicationStatus)
                .FirstOrDefault(g => g.Id == id);

            if (application == null) return HttpNotFound();

            // جلب آخر دفعة
            ViewBag.LastPaymentDate = db.Receipts
                .Where(r => r.PaymentVoucher.GraduateApplicationId == id)
                .OrderByDescending(r => r.BankPaymentDate)
                .Select(r => r.BankPaymentDate)
                .FirstOrDefault();

            // ✅ استدعاء قائمة الموقعين
            PopulateSigners();

            return View("PrintComprehensiveProfile", application);
        }

        // ============================================================
        // 5. تقرير مالي سريع (إضافي للزر الموجود في الجدول)
        // ============================================================
        public ActionResult ExportFinancialStatement(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // هنا يمكنك توجيه المستخدم لتقرير مالي مفصل أو طباعته
            // سأقوم بتحويله لصفحة طباعة كشف حساب مبسط كمثال
            var application = db.GraduateApplications.Find(id);
            if (application == null) return HttpNotFound();

            // يمكنك إنشاء View خاص لهذا الغرض لاحقاً
            return RedirectToAction("PrintComprehensiveProfile", new { id = id });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}