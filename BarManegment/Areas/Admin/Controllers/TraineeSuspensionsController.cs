using BarManegment.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System;

namespace BarManegment.Areas.Admin.Controllers
{
    // (افترض أن لديك فلتر صلاحيات)
    // [CustomAuthorize(Permission = "...")] 
    public class TraineeSuspensionsController : Controller // (أو BaseController)
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // === 1. صفحة اللجنة (لعرض الطلبات قيد المراجعة) ===
        // GET: Admin/TraineeSuspensions
        public ActionResult Index()
        {
            // جلب الطلبات التي تحتاج قراراً
            var pendingSuspensions = db.TraineeSuspensions
                .Include(t => t.Trainee)
                .Include(t => t.CreatedByUser)
                .Where(t => t.Status == "بانتظار الموافقة")
                .OrderBy(t => t.SuspensionStartDate)
                .ToList();

            return View(pendingSuspensions);
        }

        // === 2. صفحة الموظف (لإضافة طلب إيقاف جديد) ===
        // GET: Admin/TraineeSuspensions/Create?traineeId=5
        public ActionResult Create(int traineeId)
        {// ===
            if (Session["UserId"] == null)
            {
                // إذا انتهت الجلسة، أعده لصفحة دخول "الأدمن"
                return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });
            }
            // === نهاية الحل ===
            var trainee = db.GraduateApplications.Find(traineeId);
            if (trainee == null) return HttpNotFound();

            var model = new TraineeSuspension
            {
                GraduateApplicationId = traineeId,
                Trainee = trainee,
                SuspensionStartDate = DateTime.Now // تاريخ افتراضي
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(TraineeSuspension model)
        {// === 
            // ===
            if (Session["UserId"] == null)
            {
                // إذا انتهت الجلسة، أعده لصفحة دخول "الأدمن"
                return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });
            }
            // === نهاية الحل ===
            // === هذا هو السطر الذي يحل المشكلة ===
            // (أخبر المدقق أن يتجاهل هذا الحقل لأننا سنقوم بتعيينه يدوياً)
            ModelState.Remove("Status");
            // ===
            // ===
            if (ModelState.IsValid)
            {
                // الموظف فقط "يقترح" الطلب
                model.Status = "بانتظار الموافقة"; // (الآن سيتم تنفيذ هذا السطر)
                model.CreatedByUserId = (int)Session["UserId"]; // (أو ما يعادله)
                model.DecisionDate = DateTime.Now;

                db.TraineeSuspensions.Add(model);
                db.SaveChanges();

                TempData["SuccessMessage"] = "تم إرسال مقترح الإيقاف للجنة للموافقة.";
                // (العودة لملف المتدرب)
                return RedirectToAction("Details", "GraduateApplications", new { id = model.GraduateApplicationId });
            }

            // إذا فشل، أعد تحميل بيانات المتدرب
            model.Trainee = db.GraduateApplications.Find(model.GraduateApplicationId);
            return View(model);
        }

        // === 3. أكشن الموافقة (الذي تستدعيه اللجنة) ===
        // POST: Admin/TraineeSuspensions/Approve/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            var suspension = db.TraineeSuspensions
                                .Include(s => s.Trainee)
                                .FirstOrDefault(s => s.Id == id);

            if (suspension == null) return HttpNotFound();

            // 1. اعتماد سجل الإيقاف
            suspension.Status = "معتمد"; // أو "ساري"
            db.Entry(suspension).State = EntityState.Modified;

            // 2. (الأهم) تغيير حالة المتدرب نفسه
            var traineeStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "متدرب موقوف");
            if (traineeStatus != null)
            {
                suspension.Trainee.ApplicationStatusId = traineeStatus.Id;
                db.Entry(suspension.Trainee).State = EntityState.Modified;
            }

            db.SaveChanges();
            TempData["SuccessMessage"] = "تم اعتماد قرار الإيقاف وتغيير حالة المتدرب.";
            return RedirectToAction("Index");
        }

        // (أضف هذه الدالة داخل الكلاس)

        // POST: Admin/SupervisorChangeRequests/ApproveResumeRequest/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // (تأكد من اسم الصلاحية الصحيح، ربما صلاحية "التعديل")
        // [CustomAuthorize(Permission = "CanEdit")] 
        public ActionResult ApproveResumeRequest(int id)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });
            }

            // 1. جلب "طلب الاستئناف"
            var resumeRequest = db.SupervisorChangeRequests.Find(id);
            if (resumeRequest == null || resumeRequest.RequestType != "استئناف تدريب")
            {
                return HttpNotFound("الطلب غير موجود أو ليس طلب استئناف.");
            }

            // 2. جلب "المتدرب"
            var trainee = db.GraduateApplications
                            .Include(g => g.ApplicationStatus)
                            .FirstOrDefault(g => g.Id == resumeRequest.TraineeId);

            if (trainee == null || trainee.ApplicationStatus.Name != "متدرب موقوف")
            {
                TempData["ErrorMessage"] = "لا يمكن استئناف تدريب هذا المتدرب لأنه ليس في حالة (متدرب موقوف).";
                return RedirectToAction("CommitteeReview"); // (افترض أن هذه هي صفحة المراجعة)
            }

            // 3. جلب "فترة الإيقاف" المفتوحة حالياً
            // (نبحث عن آخر فترة إيقاف معتمدة ليس لها تاريخ انتهاء)
            var activeSuspension = db.TraineeSuspensions
                .Where(s => s.GraduateApplicationId == trainee.Id &&
                            s.Status == "معتمد" &&
                            s.SuspensionEndDate == null)
                .OrderByDescending(s => s.SuspensionStartDate)
                .FirstOrDefault();

            // 4. (الأهم) تنفيذ الإجراءات
            try
            {
                // أ. إغلاق فترة الإيقاف
                if (activeSuspension != null)
                {
                    activeSuspension.SuspensionEndDate = DateTime.Now; // إغلاقها بتاريخ اليوم
                    activeSuspension.Status = "منتهية"; // تغيير حالتها
                    db.Entry(activeSuspension).State = EntityState.Modified;
                }

                // ب. تغيير حالة المتدرب
                var activeStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "متدرب مقيد");
                trainee.ApplicationStatusId = activeStatus.Id;
                db.Entry(trainee).State = EntityState.Modified;

                // ج. تغيير حالة "طلب الاستئناف" نفسه
                resumeRequest.Status = "معتمد";
                resumeRequest.DecisionDate = DateTime.Now;
                db.Entry(resumeRequest).State = EntityState.Modified;

                db.SaveChanges();
                TempData["SuccessMessage"] = "تم اعتماد طلب الاستئناف وتغيير حالة المتدرب إلى (مقيد).";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ: " + ex.Message;
            }

            return RedirectToAction("CommitteeReview"); // (العودة لصفحة المراجعة)
        }

        // POST: Admin/TraineeSuspensions/EndSuspension
        [HttpPost]
        [ValidateAntiForgeryToken]
        // (تأكد من الصلاحية المناسبة، ربما "CanEdit")
        public ActionResult EndSuspension(int traineeId)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });
            }

            // 1. جلب المتدرب
            var trainee = db.GraduateApplications
                            .Include(g => g.ApplicationStatus)
                            .FirstOrDefault(g => g.Id == traineeId);

            // 2. التحقق من الحالة
            if (trainee == null || trainee.ApplicationStatus.Name != "متدرب موقوف")
            {
                TempData["ErrorMessage"] = "لا يمكن استئناف تدريب هذا المتدرب لأنه ليس في حالة (متدرب موقوف).";
                return RedirectToAction("Details", "TraineeProfile", new { id = traineeId });
            }

            // 3. جلب فترة الإيقاف "المفتوحة" حالياً (إن وجدت)
            var activeSuspension = db.TraineeSuspensions
                .Where(s => s.GraduateApplicationId == trainee.Id &&
                            s.Status == "معتمد" &&
                            s.SuspensionEndDate == null) // الإيقاف المفتوح
                .OrderByDescending(s => s.SuspensionStartDate)
                .FirstOrDefault();

            try
            {
                // 4أ. إغلاق فترة الإيقاف
                if (activeSuspension != null)
                {
                    activeSuspension.SuspensionEndDate = DateTime.Now; // إغلاقها بتاريخ اليوم
                    activeSuspension.Status = "منتهية (استئناف إداري)";
                    db.Entry(activeSuspension).State = EntityState.Modified;
                }

                // 4ب. تفعيل المتدرب
                var activeStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "متدرب مقيد");
                if (activeStatus == null)
                {
                    TempData["ErrorMessage"] = "خطأ فادح: لم يتم العثور على حالة (متدرب مقيد).";
                    return RedirectToAction("Details", "TraineeProfile", new { id = traineeId });
                }

                trainee.ApplicationStatusId = activeStatus.Id;
                db.Entry(trainee).State = EntityState.Modified;

                db.SaveChanges();
                TempData["SuccessMessage"] = "تم استئناف التدريب وتغيير حالة المتدرب إلى (مقيد).";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ: " + ex.Message;
            }

            return RedirectToAction("Details", "TraineeProfile", new { id = traineeId });
        }

        // === نهاية الإضافة ===
    }
}