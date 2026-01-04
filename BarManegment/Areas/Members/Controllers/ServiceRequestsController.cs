using BarManegment.Models;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System;
using System.Web;
using System.IO; // <-- (مطلوب لعمليات الملفات)
using BarManegment.Areas.Members.ViewModels;
using System.Collections.Generic;

namespace BarManegment.Areas.Members.Controllers
{
    [Authorize]
    public class ServiceRequestsController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Members/ServiceRequests/Create
        // GET: Members/ServiceRequests/Create
        public ActionResult Create()
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            var userId = (int)Session["UserId"];

            var graduateApp = db.GraduateApplications
                                .Include(g => g.Supervisor)
                                .FirstOrDefault(g => g.UserId == userId);

            if (graduateApp == null) return HttpNotFound();

            var viewModel = new CreateServiceRequestViewModel
            {
                CurrentSupervisorName = graduateApp.Supervisor?.ArabicName ?? "لا يوجد"
            };

            // (جلب قائمة المشرفين المتاحين للنقل)
            viewModel.SupervisorList = GetAvailableSupervisors();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateServiceRequestViewModel viewModel)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);
            if (graduateApp == null) return HttpNotFound();

            // --- 1. التحقق من صحة البيانات ---
            if (viewModel.RequestType == "نقل" && !viewModel.NewSupervisorId.HasValue)
            {
                ModelState.AddModelError("NewSupervisorId", "اختيار المشرف الجديد إلزامي لطلب النقل.");
            }
            if ((viewModel.RequestType == "نقل" || viewModel.RequestType == "استكمال") && viewModel.AttachmentFile == null)
            {
                ModelState.AddModelError("AttachmentFile", "إرفاق الملف إلزامي لطلبات النقل والاستكمال.");
            }

            bool hasOpenRequest = db.SupervisorChangeRequests.Any(r =>
                r.TraineeId == graduateApp.Id &&
                (r.Status.Contains("قيد المراجعة") || r.Status.Contains("بانتظار دفع")));

            if (hasOpenRequest)
            {
                ModelState.AddModelError("", "لديك طلب آخر قيد المعالجة. لا يمكنك تقديم طلب جديد الآن.");
            }

            if (ModelState.IsValid)
            {
                // --- 2. تحديد الحالة ونوع الرسم ---
                string finalStatus = "قيد المراجعة"; // (الافتراضي لطلب الوقف)
                FeeType requiredFeeType = null;
                string feeDescription = "";
               

                if (viewModel.RequestType == "نقل")
                {
                    requiredFeeType = db.FeeTypes.FirstOrDefault(f => f.IsActive && f.Name.Contains("نقل إشراف"));
                    finalStatus = "بانتظار دفع الرسوم";
                    if (requiredFeeType != null)
                        feeDescription = $"رسوم نقل إشراف للمتدرب {graduateApp.ArabicName}";
                }
                else if (viewModel.RequestType == "استكمال")
                {
                    requiredFeeType = db.FeeTypes.FirstOrDefault(f => f.IsActive && f.Name.Contains("استئناف تدريب"));
                    finalStatus = "بانتظار دفع الرسوم";
                    if (requiredFeeType != null)
                        feeDescription = $"رسوم استئناف تدريب للمتدرب {graduateApp.ArabicName}";
                }

                // --- 3. إنشاء الطلب ---
                var request = new SupervisorChangeRequest
                {
                    TraineeId = graduateApp.Id,
                    RequestType = viewModel.RequestType,
                    RequestDate = DateTime.Now,
                    OldSupervisorId = graduateApp.SupervisorId,
                    NewSupervisorId = (viewModel.RequestType == "نقل") ? viewModel.NewSupervisorId : null,
                    CommitteeNotes = viewModel.Reason,
                    Status = finalStatus
                };

                if (viewModel.AttachmentFile != null)
                {
                    request.NewSupervisorApprovalPath = SaveRequestAttachment(viewModel.AttachmentFile, graduateApp.Id, viewModel.RequestType);
                }

                db.SupervisorChangeRequests.Add(request);

                // --- 4. إنشاء القسيمة (إذا كان الطلب يتطلب رسوم) ---
                PaymentVoucher voucher = null;
                if (requiredFeeType != null)
                {
                    var user = db.Users.Find(userId); // (نحتاج الموظف الذي أنشأ الحساب)
                    voucher = CreatePaymentVoucher(graduateApp.Id, requiredFeeType.Id, feeDescription, user);
                    if (voucher != null)
                    {
                        db.PaymentVouchers.Add(voucher);
                        db.SaveChanges(); // حفظ للحصول على ID القسيمة
                        request.PaymentVoucherId = voucher.Id; // ربط القسيمة بالطلب
                    }
                    else
                    {
                        ModelState.AddModelError("", "خطأ أثناء إنشاء قسيمة الدفع. تأكد من تعريف الرسوم في النظام.");
                        viewModel.SupervisorList = GetAvailableSupervisors();
                        return View(viewModel);
                    }
                }

                db.SaveChanges(); // حفظ الطلب (مع ID القسيمة إذا وجد)

                TempData["SuccessMessage"] = "تم إرسال طلبك بنجاح. سيتم مراجعته من قبل اللجنة.";
                return RedirectToAction("Index", "Dashboard");
            }

            // (إعادة ملء القائمة المنسدلة في حال فشل الإرسال)
            viewModel.SupervisorList = GetAvailableSupervisors();
            return View(viewModel);
        }
        // GET: Members/ServiceRequests/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account", new { area = "Members" });

            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            var request = db.SupervisorChangeRequests
                .Include(r => r.OldSupervisor)
                .Include(r => r.NewSupervisor)
                .FirstOrDefault(r => r.Id == id);

            if (request == null || request.TraineeId != graduateApp.Id)
            {
                return HttpNotFound();
            }

            return View(request);
        }

        // === دوال مساعدة ===
        private SelectList GetAvailableSupervisors()
        {
            var practicingStatusId = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "محامي مزاول")?.Id;
            var fiveYearsAgo = DateTime.Now.AddYears(-5);

            var supervisors = db.GraduateApplications
                .Where(s => s.ApplicationStatusId == practicingStatusId && s.SubmissionDate <= fiveYearsAgo)
                .Select(s => new { s.Id, s.ArabicName })
                .ToList();

            return new SelectList(supervisors, "Id", "ArabicName");
        }

        private string SaveRequestAttachment(HttpPostedFileBase file, int traineeId, string requestType)
        {
            if (file == null || file.ContentLength == 0) return null;
            string safeRequestType = requestType.Replace(" ", "_");
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var directoryPath = Server.MapPath($"~/Uploads/ServiceRequests/{traineeId}/{safeRequestType}");
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
            var path = Path.Combine(directoryPath, fileName);
            file.SaveAs(path);
            return $"/Uploads/ServiceRequests/{traineeId}/{safeRequestType}/{fileName}";
        }

        private PaymentVoucher CreatePaymentVoucher(int traineeId, int feeTypeId, string description, UserModel issuer)
        {
            if (issuer == null) return null; // لا يمكن إنشاء قسيمة بدون موظف
            try
            {
                var feeType = db.FeeTypes.Find(feeTypeId);
                if (feeType == null) return null;
                var voucher = new PaymentVoucher
                {
                    GraduateApplicationId = traineeId,
                    IssueDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(7),
                    Status = "صادر",
                    TotalAmount = feeType.DefaultAmount,
                    IssuedByUserId = issuer.Id,
                    IssuedByUserName = issuer.FullNameArabic,
                    VoucherDetails = new List<VoucherDetail>
                       {
                           new VoucherDetail
                           {
                               FeeTypeId = feeTypeId,
                               Amount = feeType.DefaultAmount,
                               BankAccountId = feeType.BankAccountId
                           }
                       }
                };
                return voucher;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // GET: Members/ServiceRequests/Details/5
        // (تم تعديل هذه الدالة أيضاً لتعرض المرفق)


        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}