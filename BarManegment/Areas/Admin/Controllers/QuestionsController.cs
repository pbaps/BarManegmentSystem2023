using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System;
using System.Web;
using OfficeOpenXml; // <-- إضافة مهمة

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class QuestionsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index(int? examId)
        {
            if (examId == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var exam = db.Exams.Include(e => e.Questions.Select(q => q.Answers)).FirstOrDefault(e => e.Id == examId);
            if (exam == null) return HttpNotFound();
            return View(exam);
        }

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(int? examId)
        {
            if (examId == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var exam = db.Exams.Find(examId);
            if (exam == null) return HttpNotFound();

            var viewModel = new QuestionViewModel
            {
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                QuestionTypes = new SelectList(db.QuestionTypes, "Id", "Name")
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(QuestionViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var questionType = db.QuestionTypes.Find(viewModel.QuestionTypeId)?.Name;
                if (questionType == null)
                {
                    ModelState.AddModelError("QuestionTypeId", "نوع السؤال غير صالح.");
                }
                else
                {
                    var question = new Question { ExamId = viewModel.ExamId, QuestionTypeId = viewModel.QuestionTypeId, QuestionText = viewModel.QuestionText, Points = viewModel.Points, Answers = new List<Answer>() };

                    if (questionType == "اختيار من متعدد")
                    {
                        if (viewModel.CorrectAnswerIndex == null || viewModel.Answers.All(a => string.IsNullOrWhiteSpace(a.AnswerText)))
                        {
                            ModelState.AddModelError("", "لأسئلة الاختيار من متعدد، يجب تعبئة الخيارات وتحديد الإجابة الصحيحة.");
                        }
                        else
                        {
                            for (int i = 0; i < viewModel.Answers.Count; i++)
                            {
                                if (!string.IsNullOrWhiteSpace(viewModel.Answers[i].AnswerText))
                                {
                                    question.Answers.Add(new Answer { AnswerText = viewModel.Answers[i].AnswerText, IsCorrect = (i == viewModel.CorrectAnswerIndex) });
                                }
                            }
                        }
                    }
                    else if (questionType == "صح / خطأ")
                    {
                        question.Answers.Add(new Answer { AnswerText = "صح", IsCorrect = viewModel.TrueFalseAnswer });
                        question.Answers.Add(new Answer { AnswerText = "خطأ", IsCorrect = !viewModel.TrueFalseAnswer });
                    }

                    if (ModelState.IsValid) // Check again after custom validation
                    {
                        db.Questions.Add(question);
                        db.SaveChanges();
                        return RedirectToAction("Index", new { examId = viewModel.ExamId });
                    }
                }
            }

            viewModel.QuestionTypes = new SelectList(db.QuestionTypes, "Id", "Name", viewModel.QuestionTypeId);
            viewModel.ExamTitle = db.Exams.Find(viewModel.ExamId)?.Title;
            return View(viewModel);
        }


        // === بداية الإضافة: دوال الاستيراد والتصدير ===

        public ActionResult DownloadTemplate()
        {
         //   ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("نموذج الأسئلة");
                worksheet.Cells["A1"].Value = "نوع السؤال (اختيار من متعدد / صح / خطأ / مقالي)";
                worksheet.Cells["B1"].Value = "نص السؤال";
                worksheet.Cells["C1"].Value = "الدرجة";
                worksheet.Cells["D1"].Value = "الإجابة 1";
                worksheet.Cells["E1"].Value = "الإجابة 2";
                worksheet.Cells["F1"].Value = "الإجابة 3";
                worksheet.Cells["G1"].Value = "الإجابة 4";
                worksheet.Cells["H1"].Value = "الإجابة الصحيحة (للصح/خطأ: اكتب 'صح' أو 'خطأ' | للاختيار من متعدد: اكتب رقم الإجابة الصحيحة 1-4)";

                worksheet.Cells["A1:H1"].Style.Font.Bold = true;

                var stream = new System.IO.MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "QuestionImportTemplate.xlsx");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanImport")]
        public ActionResult Import(int examId, HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
            {
                TempData["ErrorMessage"] = "الرجاء اختيار ملف إكسل.";
                return RedirectToAction("Index", new { examId });
            }

            try
            {
            ///    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage(file.InputStream))
                {
                    var worksheet = package.Workbook.Worksheets.First();
                    var rowCount = worksheet.Dimension.Rows;
                    var questionTypes = db.QuestionTypes.ToList();

                    for (int row = 2; row <= rowCount; row++)
                    {
                        var questionTypeName = worksheet.Cells[row, 1].Text.Trim();
                        var questionType = questionTypes.FirstOrDefault(qt => qt.Name == questionTypeName);

                        if (questionType == null) continue; // تجاهل الصف إذا كان نوع السؤال غير صالح

                        var question = new Question
                        {
                            ExamId = examId,
                            QuestionTypeId = questionType.Id,
                            QuestionText = worksheet.Cells[row, 2].Text,
                            Points = double.TryParse(worksheet.Cells[row, 3].Text, out double points) ? points : 1.0,
                            Answers = new List<Answer>()
                        };

                        if (questionType.Name == "اختيار من متعدد")
                        {
                            var correctAnswerIndex = int.Parse(worksheet.Cells[row, 8].Text) - 1;
                            for (int i = 0; i < 4; i++)
                            {
                                var answerText = worksheet.Cells[row, 4 + i].Text;
                                if (!string.IsNullOrWhiteSpace(answerText))
                                {
                                    question.Answers.Add(new Answer { AnswerText = answerText, IsCorrect = (i == correctAnswerIndex) });
                                }
                            }
                        }
                        else if (questionType.Name == "صح / خطأ")
                        {
                            var correctAnswer = worksheet.Cells[row, 8].Text.Trim();
                            question.Answers.Add(new Answer { AnswerText = "صح", IsCorrect = (correctAnswer == "صح") });
                            question.Answers.Add(new Answer { AnswerText = "خطأ", IsCorrect = (correctAnswer == "خطأ") });
                        }

                        db.Questions.Add(question);
                    }
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "تم استيراد الأسئلة بنجاح.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء استيراد الملف: " + ex.Message;
            }

            return RedirectToAction("Index", new { examId });
        }

        [CustomAuthorize(Permission = "CanExport")]
        public ActionResult Export(int examId)
        {
            var exam = db.Exams.Include(e => e.Questions.Select(q => q.Answers)).FirstOrDefault(e => e.Id == examId);
            if (exam == null) return HttpNotFound();

          ///  ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("أسئلة " + exam.Title);
                // ... (Add headers) ...

                int row = 2;
                foreach (var q in exam.Questions)
                {
                    // ... (Populate rows with question and answer data) ...
                    row++;
                }

                var stream = new System.IO.MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Questions_{examId}.xlsx");
            }
        }

        // === نهاية الإضافة ===
        // === بداية الإضافة: دوال التعديل والحذف ===

        // GET: Admin/Questions/Edit/5
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Question question = db.Questions.Include(q => q.Answers).Include(q => q.Exam).FirstOrDefault(q => q.Id == id);
            if (question == null) return HttpNotFound();

            var viewModel = new QuestionViewModel
            {
                Id = question.Id,
                ExamId = question.ExamId,
                ExamTitle = question.Exam.Title,
                QuestionTypeId = question.QuestionTypeId,
                QuestionText = question.QuestionText,
                Points = question.Points,
                QuestionTypes = new SelectList(db.QuestionTypes, "Id", "Name", question.QuestionTypeId)
            };

            var questionTypeName = db.QuestionTypes.Find(question.QuestionTypeId)?.Name;
            if (questionTypeName == "اختيار من متعدد")
            {
                for (int i = 0; i < question.Answers.Count; i++)
                {
                    if (i < viewModel.Answers.Count)
                    {
                        viewModel.Answers[i].Id = question.Answers.ElementAt(i).Id;
                        viewModel.Answers[i].AnswerText = question.Answers.ElementAt(i).AnswerText;
                        if (question.Answers.ElementAt(i).IsCorrect)
                        {
                            viewModel.CorrectAnswerIndex = i;
                        }
                    }
                }
            }
            else if (questionTypeName == "صح / خطأ")
            {
                viewModel.TrueFalseAnswer = question.Answers.First(a => a.AnswerText == "صح").IsCorrect;
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(QuestionViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var questionInDb = db.Questions.Include(q => q.Answers).FirstOrDefault(q => q.Id == viewModel.Id);
                if (questionInDb == null) return HttpNotFound();

                var questionTypeName = db.QuestionTypes.Find(viewModel.QuestionTypeId)?.Name;

                questionInDb.QuestionText = viewModel.QuestionText;
                questionInDb.Points = viewModel.Points;

                // Clear old answers and add new ones
                db.Answers.RemoveRange(questionInDb.Answers);

                if (questionTypeName == "اختيار من متعدد")
                {
                    for (int i = 0; i < viewModel.Answers.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(viewModel.Answers[i].AnswerText))
                        {
                            questionInDb.Answers.Add(new Answer { AnswerText = viewModel.Answers[i].AnswerText, IsCorrect = (i == viewModel.CorrectAnswerIndex) });
                        }
                    }
                }
                else if (questionTypeName == "صح / خطأ")
                {
                    questionInDb.Answers.Add(new Answer { AnswerText = "صح", IsCorrect = viewModel.TrueFalseAnswer });
                    questionInDb.Answers.Add(new Answer { AnswerText = "خطأ", IsCorrect = !viewModel.TrueFalseAnswer });
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "تم تعديل السؤال بنجاح.";
                return RedirectToAction("Index", new { examId = viewModel.ExamId });
            }

            viewModel.QuestionTypes = new SelectList(db.QuestionTypes, "Id", "Name", viewModel.QuestionTypeId);
            viewModel.ExamTitle = db.Exams.Find(viewModel.ExamId)?.Title;
            return View(viewModel);
        }

        // GET: Admin/Questions/Delete/5
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Question question = db.Questions.Find(id);
            if (question == null) return HttpNotFound();
            return View(question);
        }

        // POST: Admin/Questions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteConfirmed(int id)
        {
            Question question = db.Questions.Include(q => q.Answers).FirstOrDefault(q => q.Id == id);
            if (question != null)
            {
                db.Answers.RemoveRange(question.Answers);
                db.Questions.Remove(question);
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم حذف السؤال بنجاح.";
            }
            return RedirectToAction("Index", new { examId = question.ExamId });
        }

        // === نهاية الإضافة ===
    }
}