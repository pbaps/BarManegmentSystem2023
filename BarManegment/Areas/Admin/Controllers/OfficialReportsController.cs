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

        // 1. الصفحة الرئيسية (جدول المحامين)
        public ActionResult Index()
        {
            var reportData = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Include(g => g.ContactInfo)
                .OrderByDescending(g => g.Id) // أو حسب تاريخ التسجيل
                .ToList();

            return View(reportData);
        }

        // 2. طباعة الإفادات (مزاولة / تدريب)
        public ActionResult PrintStatement(int? id, string type)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var application = db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .Include(g => g.Supervisor) // نحتاج المشرف لإفادة التدريب
                .FirstOrDefault(g => g.Id == id);

            if (application == null) return HttpNotFound();

            // توجيه حسب النوع للصفحة المناسبة
            if (type == "Practicing")
            {
                if (!application.ApplicationStatus.Name.Contains("مزاول"))
                    return Content("عذراً، هذا العضو ليس محامياً مزاولاً.");

                return View("PrintStatement_Practicing", application); // النموذج أ (adv.jpg)
            }
            else if (type == "Trainee")
            {
                if (!application.ApplicationStatus.Name.Contains("متدرب"))
                    return Content("عذراً، هذا العضو ليس متدرباً.");

                return View("PrintStatement_Trainee", application); // النموذج ب (trainee.jpg)
            }

            return HttpNotFound();
        }

        // 3. طباعة كتاب البنك
        public ActionResult PrintBankLetter(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var application = db.GraduateApplications.Find(id);
            if (application == null) return HttpNotFound();

            return View("PrintBankLetter", application); // نموذج البنك (activate_account.jpg)
        }

        // 4. طباعة التقرير الشامل (بروفايل)
        public ActionResult PrintComprehensiveProfile(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // نحتاج جلب كل البيانات المرتبطة (مؤهلات، دفعات، الخ)
            var application = db.GraduateApplications
                .Include(g => g.ContactInfo)
                .Include(g => g.Qualifications.Select(q => q.QualificationType))
                .Include(g => g.Supervisor)
                .Include(g => g.ApplicationStatus)
                .FirstOrDefault(g => g.Id == id);

            if (application == null) return HttpNotFound();

            // جلب آخر دفعة وتاريخ المزاولة (منطق بسيط للعرض)
            ViewBag.LastPaymentDate = db.Receipts.Where(r => r.PaymentVoucher.GraduateApplicationId == id)
                                                .OrderByDescending(r => r.BankPaymentDate)
                                                .Select(r => r.BankPaymentDate)
                                                .FirstOrDefault();

            return View("PrintComprehensiveProfile", application); // نموذج (adv_rep.jpg)
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}