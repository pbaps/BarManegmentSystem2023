using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Collections.Generic;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.IO;
using BarManegment.Services; // تأكد من وجود هذا للتدقيق

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class ExamsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ==================================================================
        // 1. إدارة الامتحانات (القائمة، الإنشاء، التعديل)
        // ==================================================================

        // GET: Admin/Exams
        public ActionResult Index(string searchString)
        {
            // إلغاء تفعيل الامتحانات المنتهية تلقائيًا
            var examsToDeactivate = db.Exams.Where(e => e.IsActive && e.EndTime <= DateTime.Now).ToList();
            if (examsToDeactivate.Any())
            {
                foreach (var exam in examsToDeactivate) exam.IsActive = false;
                db.SaveChanges();
            }

            var examsQuery = db.Exams
                .Include(e => e.ExamType)
                .Include(e => e.Enrollments) // ضروري لعرض عدد المسجلين
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                examsQuery = examsQuery.Where(e => e.Title.Contains(searchString));
            }

            var allExams = examsQuery.OrderByDescending(e => e.StartTime).ToList();

            var viewModel = new ExamIndexViewModel
            {
                ActiveExams = allExams.Where(e => e.IsActive && e.EndTime > DateTime.Now).ToList(),
                FinishedExams = allExams.Where(e => !e.IsActive || e.EndTime <= DateTime.Now).ToList(),
                SearchString = searchString
            };

            return View(viewModel);
        }

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            ViewBag.ExamTypeId = new SelectList(db.ExamTypes, "Id", "Name");
            // 💡 إضافة قائمة الحالات (لتحديد شرط الحالة المهنية)
            ViewBag.RequiredApplicationStatusId = new SelectList(db.ApplicationStatuses, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        // 💡 تحديث الـ Bind ليشمل الحقول الجديدة
        public ActionResult Create([Bind(Include = "Id,ExamTypeId,Title,StartTime,EndTime,DurationInMinutes,IsActive,ShowResultInstantly,PassingPercentage,MinPracticeYears,RequiredApplicationStatusId,RequirementsNote")] Exam exam)
        {
            if (ModelState.IsValid)
            {
                db.Exams.Add(exam);
                db.SaveChanges();
                AuditService.LogAction("Create Exam", "Exams", $"Created exam: {exam.Title}");
                TempData["SuccessMessage"] = "تم إنشاء الامتحان بنجاح.";
                return RedirectToAction("Index");
            }

            ViewBag.ExamTypeId = new SelectList(db.ExamTypes, "Id", "Name", exam.ExamTypeId);
            ViewBag.RequiredApplicationStatusId = new SelectList(db.ApplicationStatuses, "Id", "Name", exam.RequiredApplicationStatusId);
            return View(exam);
        }

        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Exam exam = db.Exams.Find(id);
            if (exam == null) return HttpNotFound();

            ViewBag.ExamTypeId = new SelectList(db.ExamTypes, "Id", "Name", exam.ExamTypeId);
            // 💡 إضافة القائمة للتعديل
            ViewBag.RequiredApplicationStatusId = new SelectList(db.ApplicationStatuses, "Id", "Name", exam.RequiredApplicationStatusId);
            return View(exam);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        // 💡 تحديث الـ Bind
        public ActionResult Edit([Bind(Include = "Id,ExamTypeId,Title,StartTime,EndTime,DurationInMinutes,IsActive,ShowResultInstantly,PassingPercentage,MinPracticeYears,RequiredApplicationStatusId,RequirementsNote")] Exam exam)
        {
            if (ModelState.IsValid)
            {
                db.Entry(exam).State = EntityState.Modified;
                db.SaveChanges();
                AuditService.LogAction("Edit Exam", "Exams", $"Updated exam: {exam.Title}");
                TempData["SuccessMessage"] = "تم تعديل بيانات الامتحان بنجاح.";
                return RedirectToAction("Index");
            }
            ViewBag.ExamTypeId = new SelectList(db.ExamTypes, "Id", "Name", exam.ExamTypeId);
            ViewBag.RequiredApplicationStatusId = new SelectList(db.ApplicationStatuses, "Id", "Name", exam.RequiredApplicationStatusId);
            return View(exam);
        }
        // ==================================================================
        // 2. تعيين المصححين (للامتحانات الإلكترونية المقالية)
        // ==================================================================

        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AssignGraders(int examId)
        {
            var exam = db.Exams.Find(examId);
            if (exam == null) return HttpNotFound();

            // جلب المستخدمين الفعالين فقط من الأدوار المحددة (موظف، مدير، مصحح)
            var potentialGraders = db.Users
                .Where(u => u.IsActive && (u.UserType.NameEnglish == "Employee" || u.UserType.NameEnglish == "Administrator" || u.UserType.NameEnglish == "Grader"))
                .ToList();

            var assignedGraderIds = db.ManualGrades
                .Where(g => g.TraineeAnswer.Question.ExamId == examId)
                .Select(g => g.GraderId)
                .Distinct()
                .ToList();

            var viewModel = new AssignGradersViewModel
            {
                ExamId = examId,
                ExamTitle = exam.Title,
                Graders = potentialGraders.Select(g => new GraderAssignmentViewModel
                {
                    GraderId = g.Id,
                    GraderName = g.FullNameArabic,
                    IsAssigned = assignedGraderIds.Contains(g.Id)
                }).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult AssignGraders(AssignGradersViewModel viewModel)
        {
            var selectedGraderIds = viewModel.Graders.Where(g => g.IsAssigned).Select(g => g.GraderId).ToList();
            if (!selectedGraderIds.Any())
            {
                TempData["ErrorMessage"] = "يجب اختيار مصحح واحد على الأقل.";
                return RedirectToAction("AssignGraders", new { examId = viewModel.ExamId });
            }

            var essayAnswers = db.TraineeAnswers
                .Where(a => a.Question.ExamId == viewModel.ExamId && a.Question.QuestionType.Name == "مقالي")
                .ToList();

            // حذف التعيينات القديمة للمصححين الذين لم يتم اختيارهم
            var unselectedGraderIds = viewModel.Graders.Where(g => !g.IsAssigned).Select(g => g.GraderId);
            var assignmentsToRemove = db.ManualGrades
                .Where(g => g.TraineeAnswer.Question.ExamId == viewModel.ExamId && unselectedGraderIds.Contains(g.GraderId));
            db.ManualGrades.RemoveRange(assignmentsToRemove);

            // توزيع الإجابات على المصححين المختارين بالتساوي (Round Robin)
            for (int i = 0; i < essayAnswers.Count; i++)
            {
                var graderId = selectedGraderIds[i % selectedGraderIds.Count];
                var answerId = essayAnswers[i].Id;

                var existingAssignment = db.ManualGrades.FirstOrDefault(g => g.TraineeAnswerId == answerId);
                if (existingAssignment != null)
                {
                    existingAssignment.GraderId = graderId; // إعادة تعيين
                }
                else
                {
                    db.ManualGrades.Add(new ManualGrade { TraineeAnswerId = answerId, GraderId = graderId, Status = "معين" });
                }
            }

            db.SaveChanges();
            AuditService.LogAction("Assign Graders", "Exams", $"Assigned graders for Exam ID {viewModel.ExamId}.");
            TempData["SuccessMessage"] = "تم تعيين وتوزيع مهام التصحيح بنجاح.";
            return RedirectToAction("Index");
        }

        // ==================================================================
        // 3. رفع النتائج التحريرية (Excel Import)
        // ==================================================================

        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult UploadWrittenResults(int? examId)
        {
            if (examId == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var exam = db.Exams.Include(e => e.ExamType).FirstOrDefault(e => e.Id == examId);
            if (exam == null) return HttpNotFound();

            // يمكن تفعيل هذا الشرط لحصر الميزة بامتحانات معينة
            // if (exam.ExamType.Name != "امتحان إنهاء تدريب") { ... }

            var viewModel = new UploadExamResultsViewModel
            {
                ExamId = exam.Id,
                ExamTitle = exam.Title
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult UploadWrittenResults(UploadExamResultsViewModel viewModel)
        {
            if (!ModelState.IsValid) return View(viewModel);
            if (viewModel.UploadedFile == null || viewModel.UploadedFile.ContentLength == 0)
            {
                ModelState.AddModelError("UploadedFile", "الرجاء اختيار ملف.");
                return View(viewModel);
            }

            var exam = db.Exams.Find(viewModel.ExamId);
            if (exam == null) return HttpNotFound();

            // 1. جلب كل المسجلين (سواء خريجين جدد أو متدربين) لتقليل الاستعلامات داخل اللوب
            var allEnrollments = db.ExamEnrollments
                .Include(e => e.GraduateApplication) // للمتدربين
                .Include(e => e.ExamApplication)     // للخريجين الجدد
                .Where(e => e.ExamId == viewModel.ExamId)
                .ToList();

            int updatedCount = 0;
            var errorList = new List<string>();

            try
            {
                using (var package = new ExcelPackage(viewModel.UploadedFile.InputStream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        ModelState.AddModelError("", "ملف الإكسل فارغ.");
                        return View(viewModel);
                    }

                    // الافتراض: A=الرقم الوطني, C=النتيجة, D=الدرجة (بناءً على ملف التصدير)
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var nationalId = worksheet.Cells[row, 1].GetValue<string>()?.Trim();
                        var resultText = worksheet.Cells[row, 3].GetValue<string>()?.Trim();
                        var scoreVal = worksheet.Cells[row, 4].GetValue<double?>();

                        if (string.IsNullOrWhiteSpace(nationalId)) continue;

                        // البحث عن المتدرب/الخريج باستخدام الرقم الوطني
                        var enrollment = allEnrollments.FirstOrDefault(e =>
                            (e.GraduateApplication != null && e.GraduateApplication.NationalIdNumber == nationalId) ||
                            (e.ExamApplication != null && e.ExamApplication.NationalIdNumber == nationalId)
                        );

                        if (enrollment == null)
                        {
                            errorList.Add($"السطر {row}: الرقم الوطني {nationalId} غير مسجل في هذا الامتحان.");
                            continue;
                        }

                        // تحديث سجل الامتحان
                        enrollment.Result = resultText;
                        enrollment.Score = scoreVal;

                        // 🔴 تحديث حالة الملف الأصلي (انعكاس النتيجة) 🔴

                        // أ. الخريجين الجدد
                        if (enrollment.ExamApplication != null)
                        {
                            enrollment.ExamApplication.ExamScore = scoreVal;
                            enrollment.ExamApplication.ExamResult = resultText;

                            if (resultText == "ناجح")
                            {
                                enrollment.ExamApplication.Status = "ناجح (بانتظار استكمال النواقص)";
                            }
                            else if (resultText == "راسب")
                            {
                                enrollment.ExamApplication.Status = "راسب";
                            }
                        }
                        // ب. المتدربين الحاليين (يمكن إضافة منطق هنا إذا لزم الأمر)

                        db.Entry(enrollment).State = EntityState.Modified;
                        updatedCount++;
                    }
                }

                if (updatedCount > 0)
                {
                    db.SaveChanges();
                    AuditService.LogAction("Upload Results", "Exams", $"Uploaded results for Exam {exam.Id}. Updated {updatedCount} records.");
                    TempData["SuccessMessage"] = $"تم تحديث نتائج {updatedCount} متقدم بنجاح.";
                }
                else
                {
                    TempData["ErrorMessage"] = "لم يتم تحديث أي سجل. تأكد من مطابقة الأرقام الوطنية.";
                }

                if (errorList.Any())
                {
                    string errorHtml = "<ul>" + string.Join("", errorList.Take(10).Select(e => $"<li>{e}</li>")) + "</ul>";
                    if (errorList.Count > 10) errorHtml += $"<p>...و {errorList.Count - 10} أخطاء أخرى.</p>";
                    TempData["WarningMessage"] = "تمت العملية مع بعض الملاحظات:<br>" + errorHtml;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء قراءة الملف: " + ex.Message;
                return View(viewModel);
            }

            // العودة لصفحة النتائج لرؤية التغييرات
            return RedirectToAction("ManageResults", "ExamEnrollments", new { examId = viewModel.ExamId });
        }

        // ==================================================================
        // 4. تصدير النتائج (Excel Export)
        // ==================================================================

        [CustomAuthorize(Permission = "CanExport")]
        public ActionResult ExportResults(int examId)
        {
            var exam = db.Exams.Include(e => e.ExamType).FirstOrDefault(e => e.Id == examId);
            if (exam == null) return HttpNotFound();

            var enrollments = db.ExamEnrollments
                .Include(e => e.ExamApplication)
                .Include(e => e.GraduateApplication)
                .Where(e => e.ExamId == examId)
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("النتائج");
                worksheet.View.RightToLeft = true;

                worksheet.Cells["A1"].Value = "الرقم الوطني";
                worksheet.Cells["B1"].Value = "الاسم";
                worksheet.Cells["C1"].Value = "النتيجة";
                worksheet.Cells["D1"].Value = "الدرجة";

                using (var range = worksheet.Cells["A1:D1"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                int row = 2;
                foreach (var enrollment in enrollments)
                {
                    string nationalId = "";
                    string name = "";

                    if (enrollment.ExamApplication != null)
                    {
                        nationalId = enrollment.ExamApplication.NationalIdNumber;
                        name = enrollment.ExamApplication.FullName;
                    }
                    else if (enrollment.GraduateApplication != null)
                    {
                        nationalId = enrollment.GraduateApplication.NationalIdNumber;
                        name = enrollment.GraduateApplication.ArabicName;
                    }

                    worksheet.Cells[row, 1].Value = nationalId;
                    worksheet.Cells[row, 2].Value = name;
                    worksheet.Cells[row, 3].Value = enrollment.Result;
                    worksheet.Cells[row, 4].Value = enrollment.Score;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Results_{exam.Title}_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }

        // Helper: تحميل نموذج فارغ لإدخال النتائج
        public ActionResult DownloadWrittenExamTemplate()
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("نموذج إدخال النتائج");
                worksheet.View.RightToLeft = true;

                worksheet.Cells["A1"].Value = "الرقم الوطني (مطلوب)";
                worksheet.Cells["B1"].Value = "الاسم (للمرجع فقط)";
                worksheet.Cells["C1"].Value = "النتيجة (مطلوب: 'ناجح' أو 'راسب')";
                worksheet.Cells["D1"].Value = "الدرجة (اختياري - رقم فقط)";

                using (var range = worksheet.Cells["A1:D1"])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "WrittenExamResultsTemplate.xlsx");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}