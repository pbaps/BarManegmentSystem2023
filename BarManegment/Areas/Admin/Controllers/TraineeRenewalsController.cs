using BarManegment.Helpers;
using BarManegment.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class TraineeRenewalsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/TraineeRenewals
        // GET: Admin/TraineeRenewals
        // (الكود الذي زودتني به لعرض حالة تجديد جميع المتدربين)
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult Index(string searchTerm) // إضافة searchTerm
        {
            int currentYear = DateTime.Now.Year;

            // 1. جلب ID حالة "متدرب مقيد"
            var registeredStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "متدرب مقيد");
            if (registeredStatus == null)
            {
                // يمكنك إضافة معالجة خطأ هنا، مثلاً عرض رسالة خطأ
                TempData["ErrorMessage"] = "خطأ: لم يتم العثور على حالة 'متدرب مقيد'.";
                return View(new List<GraduateApplication>());
            }

            // 2. الاستعلام عن المتدربين المقيدين الذين بدأوا التدريب في سنة سابقة
            var traineesQuery = db.GraduateApplications
                .Include(a => a.Supervisor) // جلب المشرف
                .Include(a => a.ApplicationStatus) // جلب الحالة (احتياطي)
                .Where(a => a.ApplicationStatusId == registeredStatus.Id)
                // --- شرط استبعاد السنة الأولى ---
                // تأكد من وجود حقل TrainingStartDate وتعبئته عند تغيير الحالة لـ "متدرب مقيد"
                .Where(a => a.TrainingStartDate.HasValue && a.TrainingStartDate.Value.Year < currentYear);

            // 3. تطبيق البحث (إذا وجد)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // حاول البحث بالرقم أولاً
                int searchId;
                bool isNumeric = int.TryParse(searchTerm, out searchId);

                traineesQuery = traineesQuery.Where(a =>
                    a.ArabicName.Contains(searchTerm) ||
                    (isNumeric && a.Id == searchId) // افترض أن Id هو رقم المتدرب
                );
            }

            // تنفيذ الاستعلام وترتيب النتائج
            var traineesRequiringRenewal = traineesQuery.OrderBy(a => a.ArabicName).ToList();


            // 4. جلب ID المتدربين الذين جددوا بالفعل لهذا العام
            var renewedTraineeIds = db.TraineeRenewals
                .Where(r => r.RenewalYear == currentYear)
                .Select(r => r.TraineeId)
                .ToList();

            // 5. تمرير البيانات إلى الواجهة
            ViewBag.RenewedTraineeIds = renewedTraineeIds;
            ViewBag.CurrentYear = currentYear;
            ViewBag.SearchTerm = searchTerm; // تمرير قيمة البحث للواجهة

            return View(traineesRequiringRenewal);
        }

        // ==========================================================
        // === بداية الإضافة: Action لإنشاء قسيمة تجديد سنوي ===
        // ==========================================================

        // GET: Admin/TraineeRenewals/CreateRenewal/5
        // (هذا الأكشن يتم استدعاؤه من زر "تسجيل تجديد سنوي" في ملف المتدرب)
        [CustomAuthorize(Permission = "CanAdd")] // أو الصلاحية المناسبة
        public ActionResult CreateRenewal(int id) // id هو TraineeId (أو GraduateApplicationId)
        {
            // === بداية التعديل: جلب تاريخ بدء التدريب أيضاً ===
            var trainee = db.GraduateApplications
                            .Include(t => t.ApplicationStatus) // نحتاج الحالة
                            .FirstOrDefault(t => t.Id == id);
            // === نهاية التعديل ===

            if (trainee == null)
            {
                return HttpNotFound();
            }

            // تحقق من أن المتدرب "مقيد" وليس "موقوف" أو أي حالة أخرى
            if (trainee.ApplicationStatus.Name != "متدرب مقيد")
            {
                TempData["ErrorMessage"] = "لا يمكن تجديد الاشتراك لمتدرب غير مقيد. يجب أن يكون المتدرب في حالة 'متدرب مقيد'.";
                // العودة إلى ملف المتدرب الذي كنا فيه
                return RedirectToAction("Details", "TraineeProfile", new { id = id });
            }
            // === نهاية التعديل ===
            // === بداية الإضافة: التحقق من تاريخ بدء التدريب ===
            if (!trainee.TrainingStartDate.HasValue)
            {
                // هذا لا يجب أن يحدث لمتدرب مقيد، لكنه تحقق احتياطي
                TempData["ErrorMessage"] = "خطأ: تاريخ بدء التدريب غير محدد لهذا المتدرب.";
                return RedirectToAction("Details", "TraineeProfile", new { id = id });
            }
            // === نهاية الإضافة ===
            int currentYear = DateTime.Now.Year;
            int registrationYear = trainee.TrainingStartDate.Value.Year; // سنة بدء التدريب
                                                                         // === بداية الإضافة: تطبيق القاعدة الجديدة (السنة الأولى لا يوجد تجديد) ===
            if (currentYear <= registrationYear)
            {
                TempData["InfoMessage"] = $"المتدرب بدأ التدريب في سنة {registrationYear}. لا توجد رسوم تجديد مستحقة للسنة الأولى.";
                // يمكنك توجيهه لملف المتدرب أو البقاء في نفس الصفحة حسب تصميمك
                return RedirectToAction("Details", "TraineeProfile", new { id = id });
            }
            // === نهاية الإضافة ===
            // 1. التحقق إذا كان المتدرب قد دفع تجديد هذه السنة بالفعل
            bool alreadyRenewed = db.TraineeRenewals
                                    .Any(r => r.TraineeId == id &&
                                              r.RenewalYear == currentYear);

            if (alreadyRenewed)
            {
                TempData["InfoMessage"] = "تم تجديد اشتراك هذا المتدرب لهذه السنة (" + currentYear + ") بالفعل.";
                return RedirectToAction("Details", "TraineeProfile", new { id = id });
            }

            // 2. البحث عن "نوع الرسم" الخاص بالتجديد
            var renewalFeeType = db.FeeTypes.FirstOrDefault(f => f.IsActive && f.Name.Contains("تجديد سنوي"));
            if (renewalFeeType == null)
            {
                TempData["ErrorMessage"] = "خطأ: لم يتم العثور على 'نوع رسم' باسم 'تجديد سنوي' نشط في إعدادات الرسوم. يرجى إضافته أولاً.";
                return RedirectToAction("Details", "TraineeProfile", new { id = id });
            }

            // 3. تحويل الموظف إلى صفحة إنشاء القسائم المالية
            TempData["InfoMessage"] = $"سيتم تحويلك لإنشاء قسيمة دفع خاصة برسوم التجديد السنوي ({currentYear}).";

            // التوجيه لإنشاء القسيمة مع تحديد نوع الرسم مسبقاً
            return RedirectToAction("Create", "PaymentVouchers", new { area = "Admin", id = id, feeTypeId = renewalFeeType.Id });
        }
        // ==========================================================
        // === نهاية الإضافة =======================================
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        // (تأكد من الصلاحية المناسبة)
        public ActionResult DeferRenewal(int traineeId)
        {
            var trainee = db.GraduateApplications.Find(traineeId);
            int currentYear = DateTime.Now.Year;

            // (التحقق من عدم وجود تجديد أو تأجيل مسبق لهذه السنة)
            bool alreadyDone = db.TraineeRenewals.Any(r => r.TraineeId == traineeId && r.RenewalYear == currentYear) ||
                               db.DeferredFees.Any(d => d.GraduateApplicationId == traineeId &&
                                                      d.FeeType.Name.Contains("تجديد سنوي") &&
                                                      d.DateDeferred.Year == currentYear);
            if (alreadyDone)
            {
                TempData["ErrorMessage"] = "تم تجديد أو تأجيل رسوم هذا المتدرب لهذه السنة بالفعل.";
                return RedirectToAction("Details", "TraineeProfile", new { id = traineeId });
            }

            // جلب نوع رسم التجديد
            var renewalFeeType = db.FeeTypes.FirstOrDefault(f => f.IsActive && f.Name.Contains("تجديد سنوي"));
            if (renewalFeeType == null)
            {
                TempData["ErrorMessage"] = "خطأ: لم يتم العثور على 'رسوم تجديد سنوي' نشطة.";
                return RedirectToAction("Details", "TraineeProfile", new { id = traineeId });
            }

            // 1. إنشاء سجل الدين المؤجل
            var deferredFee = new DeferredFee
            {
                GraduateApplicationId = trainee.Id,
                FeeTypeId = renewalFeeType.Id,
                Amount = renewalFeeType.DefaultAmount,
                Reason = $"رسوم تجديد سنوي مؤجلة لعام {currentYear}",
                DateDeferred = DateTime.Now,
                IsCharged = false
            };
            db.DeferredFees.Add(deferredFee);

            // 2. (مهم) إنشاء سجل تجديد (بدون إيصال)
            // هذا السجل يثبت أن المتدرب "جدد" إدارياً لهذه السنة
            var renewalRecord = new TraineeRenewal
            {
                TraineeId = trainee.Id,
                RenewalYear = currentYear,
                RenewalDate = DateTime.Now,
               ReceiptId = null // (الأهم: لا يوجد إيصال)
            };
            db.TraineeRenewals.Add(renewalRecord);

            db.SaveChanges();

            TempData["SuccessMessage"] = $"تم تأجيل رسوم التجديد السنوي لعام {currentYear} بنجاح وإضافتها لسجل الديون.";
            return RedirectToAction("Details", "TraineeProfile", new { id = traineeId });
        }


    }
}
