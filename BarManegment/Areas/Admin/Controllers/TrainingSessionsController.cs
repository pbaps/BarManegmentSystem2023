using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Services; // 💡 ضروري لخدمة التدقيق
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class TrainingSessionsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. العرض (Index)
        // ============================================================
        public ActionResult Index(int? courseId)
        {
            if (courseId == null)
            {
                // إذا دخل المستخدم مباشرة، نوجهه لقائمة الدورات ليختار دورة
                return RedirectToAction("Index", "TrainingCourses");
            }

            var course = db.TrainingCourses
                .Include(c => c.Sessions)
                .FirstOrDefault(c => c.Id == courseId);

            if (course == null) return HttpNotFound();

            // تمرير اسم الدورة للعرض في العنوان
            ViewBag.CourseName = course.CourseName;
            ViewBag.CourseId = course.Id;

            return View(course.Sessions.OrderBy(s => s.SessionDate).ToList());
        }

        // ============================================================
        // 2. الإنشاء (Create)
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(int? courseId)
        {
            if (courseId == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var course = db.TrainingCourses.Find(courseId);
            if (course == null) return HttpNotFound();

            var session = new TrainingSession
            {
                TrainingCourseId = course.Id,
                SessionDate = DateTime.Now
            };

            ViewBag.CourseName = course.CourseName;
            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create([Bind(Include = "TrainingCourseId,SessionTitle,InstructorName,SessionDate,CreditHours")] TrainingSession trainingSession)
        {
            if (ModelState.IsValid)
            {
                db.TrainingSessions.Add(trainingSession);
                db.SaveChanges();

                // ✅ تسجيل التدقيق
                AuditService.LogAction("Create Session", "TrainingSessions", $"Created session '{trainingSession.SessionTitle}' (Date: {trainingSession.SessionDate:yyyy-MM-dd}) for Course ID {trainingSession.TrainingCourseId}");

                TempData["SuccessMessage"] = "تمت إضافة الجلسة بنجاح.";
                return RedirectToAction("Index", new { courseId = trainingSession.TrainingCourseId });
            }

            var course = db.TrainingCourses.Find(trainingSession.TrainingCourseId);
            ViewBag.CourseName = course?.CourseName;

            return View(trainingSession);
        }

        // ============================================================
        // 3. التعديل (Edit)
        // ============================================================
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var session = db.TrainingSessions.Include(s => s.TrainingCourse).FirstOrDefault(s => s.Id == id);
            if (session == null) return HttpNotFound();

            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit([Bind(Include = "Id,TrainingCourseId,SessionTitle,InstructorName,SessionDate,CreditHours,TeamsMeetingUrl,TeamsMeetingId")] TrainingSession trainingSession)
        {
            if (ModelState.IsValid)
            {
                db.Entry(trainingSession).State = EntityState.Modified;
                db.SaveChanges();

                // ✅ تسجيل التدقيق
                AuditService.LogAction("Edit Session", "TrainingSessions", $"Updated session ID: {trainingSession.Id}");

                TempData["SuccessMessage"] = "تم حفظ التعديلات.";
                return RedirectToAction("Index", new { courseId = trainingSession.TrainingCourseId });
            }
            return View(trainingSession);
        }

        // ============================================================
        // 4. الحذف (Delete)
        // ============================================================
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var session = db.TrainingSessions.Include(s => s.TrainingCourse).FirstOrDefault(s => s.Id == id);
            if (session == null) return HttpNotFound();
            return View(session);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var session = db.TrainingSessions.Find(id);
            if (session != null)
            {
                int courseId = session.TrainingCourseId;
                string sessionTitle = session.SessionTitle;

                // حذف سجلات الحضور المرتبطة أولاً لتجنب أخطاء المفاتيح الأجنبية
                var attendances = db.TraineeAttendances.Where(a => a.SessionId == id).ToList();
                if (attendances.Any())
                {
                    db.TraineeAttendances.RemoveRange(attendances);
                }

                db.TrainingSessions.Remove(session);
                db.SaveChanges();

                // ✅ تسجيل التدقيق
                AuditService.LogAction("Delete Session", "TrainingSessions", $"Deleted session '{sessionTitle}' (ID: {id}) and its attendance records.");

                TempData["SuccessMessage"] = "تم حذف الجلسة وسجلات الحضور المرتبطة بها.";
                return RedirectToAction("Index", new { courseId = courseId });
            }
            return RedirectToAction("Index", "TrainingCourses");
        }

        // ============================================================
        // 5. إدارة الحضور (Attendance) - التفاصيل
        // ============================================================
        [CustomAuthorize(Permission = "CanEdit")] // صلاحية رصد الدرجات/الحضور
        public ActionResult Details(int? id) // SessionId
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var session = db.TrainingSessions
                .Include(s => s.TrainingCourse)
                .FirstOrDefault(s => s.Id == id);

            if (session == null) return HttpNotFound();

            // 1. جلب المتدربين المقيدين فقط (الذين يحق لهم الحضور)
            var registeredTrainees = db.GraduateApplications
                .Where(a => a.ApplicationStatus.Name == "متدرب مقيد")
                .OrderBy(a => a.TraineeSerialNo)
                .ToList();

            // 2. جلب الحضور المسجل مسبقاً لهذه الجلسة
            var currentAttendance = db.TraineeAttendances
                .Where(a => a.SessionId == id)
                .Select(a => a.TraineeId)
                .ToList();

            var viewModel = new CourseDetailsViewModel
            {
                CourseId = session.TrainingCourseId,
                CourseName = session.TrainingCourse.CourseName,
                SessionId = session.Id,
                SessionTitle = session.SessionTitle,
                SessionDate = session.SessionDate,
                // تعبئة القائمة مع تحديد الحالة الحالية
                Trainees = registeredTrainees.Select(t => new TraineeAttendanceViewModel
                {
                    TraineeId = t.Id,
                    TraineeSerialNo = t.TraineeSerialNo,
                    TraineeName = t.ArabicName,
                    IsAttended = currentAttendance.Contains(t.Id)
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Details(CourseDetailsViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // 1. جلب السجلات الحالية لهذه الجلسة لتحديثها
                var existingAttendances = db.TraineeAttendances
                    .Where(a => a.SessionId == viewModel.SessionId)
                    .ToList();

                int addedCount = 0;
                int removedCount = 0;

                foreach (var traineeVM in viewModel.Trainees)
                {
                    var existingRecord = existingAttendances.FirstOrDefault(a => a.TraineeId == traineeVM.TraineeId);

                    if (traineeVM.IsAttended)
                    {
                        // إذا تم تحديد "حاضر" ولم يكن مسجلاً -> إضافة
                        if (existingRecord == null)
                        {
                            var newAttendance = new TraineeAttendance
                            {
                                SessionId = viewModel.SessionId,
                                TraineeId = traineeVM.TraineeId,
                                Status = "حاضر",
                                AttendanceTime = DateTime.Now
                            };
                            db.TraineeAttendances.Add(newAttendance);
                            addedCount++;
                        }
                    }
                    else
                    {
                        // إذا تم إزالة التحديد وكان مسجلاً -> حذف (اعتباره غياب)
                        if (existingRecord != null)
                        {
                            db.TraineeAttendances.Remove(existingRecord);
                            removedCount++;
                        }
                    }
                }

                db.SaveChanges();

                // ✅ تسجيل التدقيق
                AuditService.LogAction("Record Attendance", "TrainingSessions", $"Updated attendance for session ID {viewModel.SessionId}. Added: {addedCount}, Removed: {removedCount}.");

                TempData["SuccessMessage"] = "تم حفظ كشف الحضور بنجاح.";
                return RedirectToAction("Index", new { courseId = viewModel.CourseId });
            }

            return View(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}