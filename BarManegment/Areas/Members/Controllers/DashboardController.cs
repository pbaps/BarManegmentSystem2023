using BarManegment.Models;
using BarManegment.Areas.Members.ViewModels;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using BarManegment.Areas.Admin.ViewModels; // (مطلوب لـ PrintReceiptViewModel)
using System.Net;
using System.Web;
using System.IO;
using BarManegment.Helpers; // (مطلوب لـ TafqeetHelper)
using Tafqeet; // (مطلوب لـ TafqeetHelper)

namespace BarManegment.Areas.Members.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // الصفحة الرئيسية (لوحة التحكم)
        // ============================================================
        public ActionResult Index()
        {
            // 1. التحقق من الجلسة
            if (Session["UserId"] == null)
            {
                System.Web.Security.FormsAuthentication.SignOut();
                return RedirectToAction("Login", "Account", new { area = "Members" });
            }
            var userId = (int)Session["UserId"];

            var graduateApp = db.GraduateApplications
                                .Include(g => g.ApplicationStatus)
                                .FirstOrDefault(g => g.UserId == userId);

            if (graduateApp == null)
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "Members" });
            }

            var viewModel = new MemberDashboardViewModel
            {
                GraduateInfo = graduateApp,
                // 💡 تهيئة القوائم لتجنب NullReferenceException
                AvailableJobTests = new List<AvailableExamViewModel>(),
                EnrolledExams = new List<EnrolledExamViewModel>(),
                FinishedExams = new List<EnrolledExamViewModel>()
            };

            string status = graduateApp.ApplicationStatus.Name;
            var now = DateTime.Now;

            // --- جلب سجل الامتحانات ---
            var allMyExams = db.ExamEnrollments
                .Include(e => e.Exam)
                .Where(e => e.GraduateApplicationId == graduateApp.Id)
                .ToList();

            // 1. الامتحانات النشطة
            viewModel.EnrolledExams = allMyExams
                .Where(e => e.Exam.IsActive && e.Exam.StartTime <= now && e.Exam.EndTime >= now && string.IsNullOrEmpty(e.Result))
                .Select(e => new EnrolledExamViewModel
                {
                    Id = e.Exam.Id,
                    Title = e.Exam.Title,
                    EndTime = e.Exam.EndTime,
                    DurationInMinutes = e.Exam.DurationInMinutes
                }).ToList();

            // 2. الامتحانات المنتهية
            viewModel.FinishedExams = allMyExams
                .Where(e => !string.IsNullOrEmpty(e.Result) || e.Exam.EndTime < now)
                .OrderByDescending(e => e.Exam.StartTime)
                .Select(e => new EnrolledExamViewModel
                {
                    Id = e.Id,
                    Title = e.Exam.Title,
                    EndTime = e.Exam.EndTime,
                    Result = e.Result ?? "غائب/لم يقدم",
                    Score = e.Score
                }).ToList();


            // --- جلب باقي البيانات حسب نوع المستخدم ---

            if (status.Contains("متدرب"))
            {
                // محاضرات
                viewModel.UpcomingLectures = db.TraineeAttendances
                    .Include(a => a.Session)
                    .Where(a => a.TraineeId == graduateApp.Id && a.Session.SessionDate > now)
                    .OrderBy(a => a.Session.SessionDate)
                    .Take(5)
                    .Select(a => new UpcomingLectureViewModel { Id = a.Session.Id, Title = a.Session.SessionTitle, StartTime = a.Session.SessionDate, TeamsLink = a.Session.TeamsMeetingUrl })
                    .ToList();

                // أبحاث
                viewModel.ResearchTasks = db.LegalResearches
                    .Where(r => r.GraduateApplicationId == graduateApp.Id)
                    .Select(r => new ResearchTaskViewModel { Id = r.Id, Title = r.Title, Status = r.Status })
                    .ToList();

                // سجلات تدريب
                viewModel.MyTrainingLogs = db.TrainingLogs
                    .Where(l => l.GraduateApplicationId == graduateApp.Id)
                    .OrderByDescending(l => l.Year).ThenByDescending(l => l.Month)
                    .Take(5)
                    .ToList();

                // طلبات إدارية
                viewModel.MyServiceRequests = db.SupervisorChangeRequests
                    .Where(r => r.TraineeId == graduateApp.Id)
                    .OrderByDescending(r => r.RequestDate)
                    .Take(5)
                    .Select(r => new ServiceRequestViewModel { Id = r.Id, RequestType = r.RequestType, Status = r.Status, SubmissionDate = r.RequestDate })
                    .ToList();

                // إيصالات
                viewModel.MyReceipts = db.Receipts
                    .Include(r => r.PaymentVoucher)
                    .Where(r => r.PaymentVoucher.GraduateApplicationId == graduateApp.Id)
                    .OrderByDescending(r => r.BankPaymentDate)
                    .Take(5)
                    .Select(r => new MemberReceiptViewModel { Id = r.Id, ReceiptFullNumber = r.SequenceNumber.ToString(), TotalAmount = r.PaymentVoucher.TotalAmount, PaymentDate = r.BankPaymentDate })
                    .ToList();

                // إيقافات
                viewModel.MySuspensions = db.TraineeSuspensions
                    .Where(s => s.GraduateApplicationId == graduateApp.Id)
                    .Select(s => new MemberSuspensionViewModel { StartDate = s.SuspensionStartDate, EndDate = s.SuspensionEndDate, Reason = s.Reason, Status = (s.SuspensionEndDate.HasValue && s.SuspensionEndDate < now) ? "منتهية" : "سارية" })
                    .ToList();
            }
            else if (status.Contains("محامي") || status == "Advocate")
            {
                // 💡 1. منطق جلب الاختبارات الوظيفية المتاحة للتقدم

                // 💡 التصحيح الهام: التأكد من تهيئة القائمة قبل الاستخدام
                if (viewModel.AvailableJobTests == null)
                {
                    viewModel.AvailableJobTests = new List<AvailableExamViewModel>();
                }

                var availableTests = db.Exams
                    .Include(e => e.ExamType)
                    .Where(e => e.IsActive && e.EndTime > now && (e.ExamType.Name.Contains("وظيفي") || e.ExamType.Name.Contains("Job")))
                    .ToList();

                foreach (var exam in availableTests)
                {
                    // تخطي إذا كان مسجلاً بالفعل
                    if (allMyExams.Any(e => e.ExamId == exam.Id)) continue;

                    bool isEligible = true;
                    string reason = "";

                    // التحقق من الحالة المطلوبة
                    if (exam.RequiredApplicationStatusId.HasValue && exam.RequiredApplicationStatusId != graduateApp.ApplicationStatusId)
                    {
                        isEligible = false;
                        reason = "حالة العضوية غير مطابقة للشروط.";
                    }

                    // التحقق من سنوات المزاولة
                    if (isEligible && exam.MinPracticeYears.HasValue)
                    {
                        var practiceStartDate = graduateApp.PracticeStartDate ?? DateTime.Now;
                        var yearsOfPractice = (now - practiceStartDate).TotalDays / 365.25;
                        if (yearsOfPractice < exam.MinPracticeYears.Value)
                        {
                            isEligible = false;
                            reason = $"يتطلب {exam.MinPracticeYears} سنوات مزاولة على الأقل.";
                        }
                    }

                    viewModel.AvailableJobTests.Add(new AvailableExamViewModel
                    {
                        ExamId = exam.Id,
                        Title = exam.Title,
                        StartTime = exam.StartTime,
                        EndTime = exam.EndTime,
                        Duration = exam.DurationInMinutes,
                        RequirementsNote = exam.RequirementsNote,
                        IsEligible = isEligible,
                        IneligibilityReason = reason
                    });
                }

                // للمحامي المزاول (بيانات أخرى)
                viewModel.MyTrainees = db.GraduateApplications
                    .Where(t => t.SupervisorId == graduateApp.Id && t.ApplicationStatus.Name == "متدرب مقيد")
                    .Select(t => new MyTraineeViewModel { Id = t.Id, Name = t.ArabicName, SerialNo = t.TraineeSerialNo, StartDate = t.TrainingStartDate ?? DateTime.Now })
                    .ToList();

                viewModel.MyLoans = db.LoanApplications
                    .Include(l => l.LoanType)
                    .Where(l => l.LawyerId == graduateApp.Id)
                    .OrderByDescending(l => l.ApplicationDate)
                    .Take(5)
                    .Select(l => new MemberLoanViewModel { LoanId = l.Id, Amount = l.Amount, Status = l.Status, LoanTypeName = l.LoanType.Name, IsDisbursed = l.IsDisbursed })
                    .ToList();

                viewModel.MyContractShares = db.FeeDistributions
                    .Include(d => d.ContractTransaction.ContractType)
                    .Include(d => d.Receipt)
                    .Where(d => d.LawyerId == graduateApp.Id)
                    .OrderByDescending(d => d.Receipt.BankPaymentDate)
                    .Take(5)
                    .Select(d => new MemberShareViewModel
                    {
                        TransactionId = d.ContractTransactionId,
                        ContractTypeName = d.ContractTransaction.ContractType.Name,
                        PaymentDate = d.Receipt.BankPaymentDate,
                        LawyerShareAmount = d.Amount,
                        Status = d.IsSentToBank ? "مدفوع" : "معلق"
                    })
                    .ToList();

                viewModel.PracticingRenewals = db.PracticingLawyerRenewals
                    .Include(r => r.Receipt)
                    .Where(r => r.GraduateApplicationId == graduateApp.Id)
                    .OrderByDescending(r => r.RenewalYear)
                    .ToList();

                viewModel.PendingSupervisionRequests = db.SupervisorChangeRequests
                    .Include(r => r.Trainee)
                    .Where(r => r.NewSupervisorId == graduateApp.Id && r.Status == "بانتظار موافقة المشرف الجديد")
                    .Select(r => new PendingSupervisionRequestViewModel { Id = r.Id, Name = r.Trainee.ArabicName, SubmissionDate = r.RequestDate })
                    .ToList();

                viewModel.PendingTrainingLogs = db.TrainingLogs
                    .Include(l => l.Trainee)
                    .Where(l => l.SupervisorId == graduateApp.Id && l.Status == "بانتظار موافقة المشرف")
                    .Select(l => new PendingTrainingLogViewModel { LogId = l.Id, TraineeName = l.Trainee.ArabicName, Month = l.Month, Year = l.Year, SubmissionDate = l.SubmissionDate })
                    .ToList();
            }

            return View(viewModel);
        }

        // ============================================================
        // 💡 2. دالة التقدم للامتحان
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApplyForJobTest(int examId)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            var userId = (int)Session["UserId"];
            var lawyer = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            if (lawyer == null) return HttpNotFound();

            var exam = db.Exams.Find(examId);
            if (exam == null || !exam.IsActive || exam.EndTime < DateTime.Now)
            {
                TempData["ErrorMessage"] = "الامتحان غير متاح أو انتهى وقته.";
                return RedirectToAction("Index");
            }

            // التحقق من التسجيل المسبق
            if (db.ExamEnrollments.Any(e => e.ExamId == examId && e.GraduateApplicationId == lawyer.Id))
            {
                TempData["InfoMessage"] = "أنت مسجل بالفعل في هذا الامتحان.";
                return RedirectToAction("Index");
            }

            // تسجيل المحامي
            var enrollment = new ExamEnrollment
            {
                ExamId = examId,
                GraduateApplicationId = lawyer.Id,
                // ExamApplicationId يبقى null لأنه محامي موجود
            };

            db.ExamEnrollments.Add(enrollment);
            db.SaveChanges();

            TempData["SuccessMessage"] = "تم تسجيلك في الاختبار الوظيفي بنجاح. يمكنك الدخول إليه من قسم الامتحانات النشطة وقت البدء.";
            return RedirectToAction("Index");
        }

        public ActionResult Result(int? enrollmentId)
        {
            if (enrollmentId == null)
            {
                if (Session["EnrollmentId"] == null) return RedirectToAction("Index", "ExamLogin");
                enrollmentId = (int)Session["EnrollmentId"];
            }

            var enrollment = db.ExamEnrollments.Include(e => e.Exam.ExamType).FirstOrDefault(e => e.Id == enrollmentId);
            if (enrollment == null) return HttpNotFound();

            var applicantId = (int?)Session["ApplicantId"];
            if (applicantId.HasValue && enrollment.ExamApplicationId != applicantId && enrollment.GraduateApplicationId != applicantId)
            {
                // حماية بسيطة لمنع عرض نتائج الآخرين
                return RedirectToAction("Index", "Dashboard");
            }

            double totalPossibleScore = db.Questions.Where(q => q.ExamId == enrollment.ExamId).Sum(q => (double?)q.Points) ?? 0;
            ViewBag.TotalPossibleScore = totalPossibleScore;

            return View(enrollment);
        }













        // 💡💡 === بداية الإضافة: دوال القروض الجديدة === 💡💡

        // GET: Members/Dashboard/MyLoans
        // (صفحة عرض جميع القروض)
        public ActionResult MyLoans()
        {
            var graduateApp = GetCurrentLawyer();
            if (graduateApp == null) { return RedirectToAction("Login", "Account", new { area = "Members" }); }

            var lawyerId = graduateApp.Id;

            var model = db.LoanApplications
                .Include(l => l.LoanType)
                .Where(l => l.LawyerId == lawyerId)
                .OrderByDescending(l => l.ApplicationDate)
                .Select(l => new MemberLoanViewModel
                {
                    LoanId = l.Id,
                    LoanTypeName = l.LoanType.Name,
                    Amount = l.Amount,
                    InstallmentCount = l.InstallmentCount,
                    ApplicationDate = l.ApplicationDate,
                    Status = l.Status,
                    IsDisbursed = l.IsDisbursed
                })
                .ToList();

            return View(model);
        }

        // GET: Members/Dashboard/MyLoanDetails/5
        // (صفحة عرض تفاصيل القرض والأقساط)
        public ActionResult MyLoanDetails(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var graduateApp = GetCurrentLawyer();
            if (graduateApp == null) { return RedirectToAction("Login", "Account", new { area = "Members" }); }

            var lawyerId = graduateApp.Id;

            // (جلب القرض مع الأقساط)
            var loan = db.LoanApplications
                .Include(l => l.LoanType)
                .Include(l => l.Installments.Select(i => i.PaymentVoucher))
                .Include(l => l.Installments.Select(i => i.Receipt))
                .FirstOrDefault(l => l.Id == id && l.LawyerId == lawyerId); // (تأكيد الملكية)

            if (loan == null) return HttpNotFound();

            // (تحويله إلى ViewModel)
            var viewModel = new MemberLoanViewModel
            {
                LoanId = loan.Id,
                LoanTypeName = loan.LoanType.Name,
                Amount = loan.Amount,
                InstallmentCount = loan.InstallmentCount,
                ApplicationDate = loan.ApplicationDate,
                Status = loan.Status,
                IsDisbursed = loan.IsDisbursed,
                Installments = loan.Installments.OrderBy(i => i.InstallmentNumber)
                                .Select(i => new MemberInstallmentViewModel
                                {
                                    InstallmentNumber = i.InstallmentNumber,
                                    DueDate = i.DueDate,
                                    Amount = i.Amount,
                                    Status = i.Status,
                                    PaymentVoucherId = i.PaymentVoucherId,
                                    ReceiptId = i.ReceiptId
                                }).ToList()
            };

            return View(viewModel);
        }

        // 💡💡 === نهاية الإضافة === 💡💡

        // (دوال مراجعة سجل التدريب للمشرف - تبقى كما هي)
        // ... (ReviewLog, ApproveLog, RejectLog) ...


        // ============================================================
        // 4. إجراءات المشرف على السجلات (قبول / رفض)
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveLog(int logId)
        {
            var currentLawyer = GetCurrentLawyer();
            var log = db.TrainingLogs.Find(logId);

            if (log != null && log.SupervisorId == currentLawyer.Id)
            {
                log.Status = "معتمد"; // أو "Approved" حسب المسميات في قاعدة بياناتك
                // log.ApprovalDate = DateTime.Now; // إذا كان الحقل موجوداً
                db.SaveChanges();
                TempData["SuccessMessage"] = "تم اعتماد السجل الشهري بنجاح.";
            }
            else
            {
                TempData["ErrorMessage"] = "حدث خطأ أو ليس لديك صلاحية.";
            }

            return RedirectToAction("ViewTraineeProfile", new { id = log?.GraduateApplicationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectLog(int logId, string rejectionReason)
        {
            var currentLawyer = GetCurrentLawyer();
            var log = db.TrainingLogs.Find(logId);

            if (log != null && log.SupervisorId == currentLawyer.Id)
            {
                log.Status = "مرفوض"; // أو "Rejected"
                log.SupervisorNotes = rejectionReason; // تخزين سبب الرفض
                db.SaveChanges();
                TempData["InfoMessage"] = "تم رفض السجل وإعادته للمتدرب للتصحيح.";
            }
            else
            {
                TempData["ErrorMessage"] = "حدث خطأ أو ليس لديك صلاحية.";
            }

            return RedirectToAction("ViewTraineeProfile", new { id = log?.GraduateApplicationId });
        }

        // دالة لتحميل المرفق الخاص بالسجل
        // دالة لتحميل المرفق الخاص بالسجل
        public ActionResult GetTrainingLogAttachment(int logId)
        {
            var currentLawyer = GetCurrentLawyer();
            var log = db.TrainingLogs.Find(logId);

            // التحقق من الصلاحية
            if (log == null || (log.SupervisorId != currentLawyer.Id && log.GraduateApplicationId != currentLawyer.Id))
            {
                return HttpNotFound();
            }

            // ✅ التصحيح: استخدام FilePath بدلاً من AttachmentPath
            if (string.IsNullOrEmpty(log.FilePath))
            {
                return Content("لا يوجد مرفق.");
            }

            // ✅ التصحيح: استخدام FilePath
            string filePath = Server.MapPath(log.FilePath);
            if (!System.IO.File.Exists(filePath)) return HttpNotFound("الملف غير موجود على السيرفر.");

            string fileName = Path.GetFileName(filePath);
            return File(filePath, "application/pdf", fileName);
        }

        // (دالة بدء الامتحان - تبقى كما هي)
        public ActionResult StartTraineeExam(int? examId)
        {
            // ... (الكود سليم)
            return RedirectToAction("StartExam", "TakeExam", new { area = "ExamPortal" });
        }
        // (دالة مساعدة لجلب بيانات المحامي الحالي)
        private GraduateApplication GetCurrentLawyer()
        {
            if (Session["UserId"] == null) return null;
            var userId = (int)Session["UserId"];

            return db.GraduateApplications
                .Include(g => g.ApplicationStatus)
                .FirstOrDefault(g => g.UserId == userId);
        }
        // 💡💡 === بداية الإضافة: دالة "عرض كل الحصص" === 💡💡
        // GET: Members/Dashboard/MyContractShares
        public ActionResult MyContractShares(string searchString, DateTime? from, DateTime? to)
        {
            var graduateApp = GetCurrentLawyer();
            if (graduateApp == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Members" });
            }

            // (جلب الاستعلام الأساسي)
            var query = db.FeeDistributions
                .Include(d => d.ContractTransaction.ContractType)
                .Include(d => d.Receipt)
                .Where(d => d.LawyerId == graduateApp.Id);

            // (تطبيق التصفية)
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(d =>
                    d.ContractTransactionId.ToString().Contains(searchString) ||
                    d.ContractTransaction.ContractType.Name.Contains(searchString)
                );
            }
            if (from.HasValue)
            {
                query = query.Where(d => d.Receipt.BankPaymentDate >= from.Value);
            }
            if (to.HasValue)
            {
                var toDate = to.Value.AddDays(1);
                query = query.Where(d => d.Receipt.BankPaymentDate < toDate);
            }

            // (جلب النتائج)
            var shares = query
                .OrderByDescending(d => d.Receipt.BankPaymentDate)
                .Select(d => new MemberShareViewModel
                {
                    TransactionId = d.ContractTransactionId,
                    ContractTypeName = d.ContractTransaction.ContractType.Name,
                    PaymentDate = d.Receipt.BankPaymentDate,
                    LawyerShareAmount = d.Amount,
                    Status = d.IsOnHold ? "محجوزة" : (d.IsSentToBank ? "مرسلة للبنك" : "جاهزة للدفع")
                }).ToList();

            ViewBag.SearchString = searchString;
            ViewBag.FromDate = from?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = to?.ToString("yyyy-MM-dd");

            return View(shares);
        }
        // 💡💡 === نهاية الإضافة === 💡💡




        // (دالة عرض ملف المتدرب للمشرف - تبقى كما هي)
        // ============================================================
        // 3. عرض ملف المتدرب وسجلاته للمشرف
        // ============================================================
        public ActionResult ViewTraineeProfile(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var currentLawyer = GetCurrentLawyer();
            if (currentLawyer == null) return RedirectToAction("Login", "Account");

            // جلب بيانات المتدرب الأساسية
            var trainee = db.GraduateApplications
                .Include(t => t.ContactInfo)
                .Include(t => t.ApplicationStatus)
                .Include(t => t.NationalIdType)
                .FirstOrDefault(t => t.Id == id);

            if (trainee == null) return HttpNotFound();

            // التحقق الأمن: هل هذا المتدرب يتبع للمشرف الحالي؟
            if (trainee.SupervisorId != currentLawyer.Id)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "عذراً، هذا المتدرب ليس تحت إشرافك الحالي.");
            }

            // إعداد الموديل الشامل
            var viewModel = new TraineeReviewViewModel
            {
                Trainee = trainee,

                // 1. سجلات التدريب
                Logs = db.TrainingLogs
                    .Where(l => l.GraduateApplicationId == trainee.Id)
                    .OrderByDescending(l => l.Year).ThenByDescending(l => l.Month)
                    .ToList(),

                // 2. السجل المالي
                Receipts = db.Receipts
                    .Include(r => r.PaymentVoucher)
                    .Where(r => r.PaymentVoucher.GraduateApplicationId == trainee.Id)
                    .OrderByDescending(r => r.BankPaymentDate)
                    .ToList(),

                // 3. الأبحاث
                Researches = db.LegalResearches
                    .Where(r => r.GraduateApplicationId == trainee.Id)
                    .OrderByDescending(r => r.SubmissionDate)
                    .ToList(),

                // 4. الامتحانات
                Exams = db.ExamEnrollments
                    .Include(e => e.Exam)
                    .Where(e => e.GraduateApplicationId == trainee.Id)
                    .OrderByDescending(e => e.Exam.StartTime)
                    .ToList(),

                // 5. الطلبات الإدارية
                Requests = db.SupervisorChangeRequests
                    .Where(r => r.TraineeId == trainee.Id)
                    .OrderByDescending(r => r.RequestDate)
                    .ToList()
            };

            return View(viewModel);
        }

        // (دالة جلب مرفق سجل التدريب - تبقى كما هي)


        // أضف هذا الأكشن في DashboardController (كمسؤول) أو قم بتنفيذه عبر SQL
        // هذا الكود للتأكد من وجود الصلاحيات
        public ActionResult FixCommitteePermissions()
        {
            // 1. تحديد المودول الخاص ببوابة اللجان
            var committeeModule = db.Modules.FirstOrDefault(m => m.ControllerName == "CommitteePortal");

            // إذا لم يكن موجوداً، نقوم بإنشائه
            if (committeeModule == null)
            {
                committeeModule = new ModuleModel { NameArabic = "بوابة لجان المناقشة والاختبارات", ControllerName = "CommitteePortal" };
                db.Modules.Add(committeeModule);
                db.SaveChanges();
            }

            // 2. تحديد الأدوار التي تحتاج صلاحية (محامي + عضو لجنة)
            var roles = db.UserTypes.Where(u => u.NameEnglish == "Advocate" || u.NameEnglish == "CommitteeMember").ToList();

            foreach (var role in roles)
            {
                // التحقق هل الصلاحية موجودة؟
                var perm = db.Permissions.FirstOrDefault(p => p.UserTypeId == role.Id && p.ModuleId == committeeModule.Id);

                if (perm == null)
                {
                    // إنشاء الصلاحية
                    db.Permissions.Add(new PermissionModel
                    {
                        UserTypeId = role.Id,
                        ModuleId = committeeModule.Id,
                        CanView = true, // ضروري للدخول
                        CanAdd = true,  // لإضافة الدرجات
                        CanEdit = true, // لتعديل الدرجات
                        CanDelete = false
                    });
                }
                else
                {
                    // تحديث الصلاحية لضمان التفعيل
                    perm.CanView = true;
                    perm.CanAdd = true;
                    perm.CanEdit = true;
                    db.Entry(perm).State = EntityState.Modified;
                }
            }

            db.SaveChanges();
            return Content("تم تحديث صلاحيات بوابة اللجان بنجاح للمحامين وأعضاء اللجان.");
        }


        // ============================================================
        // طباعة الإيصال (تم التصحيح)
        // ============================================================
        public ActionResult PrintReceipt(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            if (Session["UserId"] == null) return RedirectToAction("Login", "Account", new { area = "Members" });

            var userId = (int)Session["UserId"];
            // التأكد من أن الإيصال يخص المستخدم الحالي
            var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            var receipt = db.Receipts
                .Include(r => r.PaymentVoucher.GraduateApplication.ApplicationStatus)
                .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType.Currency))
                .Include(r => r.PaymentVoucher.VoucherDetails.Select(d => d.FeeType))
                .FirstOrDefault(r => r.Id == id);

            if (receipt == null || (graduateApp != null && receipt.PaymentVoucher.GraduateApplicationId != graduateApp.Id))
            {
                return HttpNotFound("الإيصال غير موجود أو لا تملك صلاحية لعرضه.");
            }

            var currencySymbol = receipt.PaymentVoucher.VoucherDetails.FirstOrDefault()?.FeeType.Currency?.Symbol ?? "";

            // استخدام TafqeetHelper الموجود في مجلد Helpers
            var amountInWords = TafqeetHelper.ConvertToArabic(receipt.PaymentVoucher.TotalAmount, currencySymbol);

            var viewModel = new PrintReceiptViewModel
            {
                ReceiptFullNumber = $"{receipt.SequenceNumber}/{receipt.Year}",
                ReceiptId = receipt.Id,
                ApplicantName = receipt.PaymentVoucher.GraduateApplication?.ArabicName ?? "غير محدد",
                ApplicantStatus = receipt.PaymentVoucher.GraduateApplication?.ApplicationStatus?.Name ?? "-",
                BankPaymentDate = receipt.BankPaymentDate,
                BankReceiptNumber = receipt.BankReceiptNumber,
                CreationDate = receipt.CreationDate,
                IssuedByUserName = receipt.IssuedByUserName,
                TotalAmount = receipt.PaymentVoucher.TotalAmount,
                TotalAmountInWords = amountInWords, // تمرير التفقيد هنا
                CurrencySymbol = currencySymbol,
                PaymentMethod = receipt.PaymentVoucher.PaymentMethod,
                Details = receipt.PaymentVoucher.VoucherDetails.Select(d => new ReceiptDetailViewModel
                {
                    FeeTypeName = d.FeeType.Name,
                    Amount = d.Amount,
                    Description = d.Description
                }).ToList()
            };

            // 💡 التصحيح: إرجاع الفيو الافتراضي (PrintReceipt) الموجود في نفس مجلد الكونترولر
            return View(viewModel);
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