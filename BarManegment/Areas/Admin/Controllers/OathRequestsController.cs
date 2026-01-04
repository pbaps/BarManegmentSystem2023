using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Services; // 💡 لإضافة AuditService
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class OathRequestsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. القائمة والبحث (Index)
        // ============================================================
        public ActionResult Index()
        {
            // توجيه تلقائي لقائمة المراجعة (الأكثر استخداماً للموظف)
            return RedirectToAction("ReviewList");
        }

        public ActionResult ReviewList(string searchTerm = null)
        {
            var query = db.OathRequests
                .Include(o => o.Trainee)
                .Where(o => o.Status == "بانتظار موافقة لجنة اليمين");

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(o => o.Trainee.ArabicName.Contains(searchTerm) ||
                                         o.Trainee.NationalIdNumber.Contains(searchTerm));
            }

            var pendingRequests = query.OrderBy(o => o.RequestDate).ToList();
            ViewBag.SearchTerm = searchTerm;
            return View(pendingRequests);
        }

        // ============================================================
        // 2. تقديم الطلب (Create)
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(int? traineeId)
        {
            if (traineeId == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var trainee = db.GraduateApplications.Find(traineeId.Value);
            if (trainee == null) return HttpNotFound();

            // التحقق من عدم وجود طلب قيد المعالجة
            bool hasPending = db.OathRequests.Any(o => o.GraduateApplicationId == traineeId && (o.Status != "مرفوض" && o.Status != "مكتمل"));
            if (hasPending)
            {
                TempData["ErrorMessage"] = "لدى هذا المتدرب طلب قيد المعالجة بالفعل.";
                return RedirectToAction("Details", "RegisteredTrainees", new { id = traineeId });
            }

            var viewModel = new OathRequestCreateViewModel
            {
                TraineeId = trainee.Id,
                TraineeName = trainee.ArabicName
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(OathRequestCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // إعادة التحقق
                bool hasPending = db.OathRequests.Any(o => o.GraduateApplicationId == viewModel.TraineeId && (o.Status != "مرفوض" && o.Status != "مكتمل"));
                if (hasPending)
                {
                    TempData["ErrorMessage"] = "لدى هذا المتدرب طلب يمين آخر نشط.";
                    return RedirectToAction("Details", "RegisteredTrainees", new { id = viewModel.TraineeId });
                }

                try
                {
                    // حفظ الملفات
                    string completionFormPath = SaveFile(viewModel.CompletionFormFile, viewModel.TraineeId, "OathForms");
                    string supervisorCertPath = SaveFile(viewModel.SupervisorCertificateFile, viewModel.TraineeId, "OathForms");

                    if (completionFormPath == null || supervisorCertPath == null)
                    {
                        ModelState.AddModelError("", "حدث خطأ أثناء حفظ الملفات. تأكد من اختيار ملفات صالحة.");
                    }
                    else
                    {
                        var oathRequest = new OathRequest
                        {
                            GraduateApplicationId = viewModel.TraineeId,
                            RequestDate = DateTime.Now,
                            Status = "بانتظار موافقة لجنة اليمين",
                            CompletionFormPath = completionFormPath,
                            SupervisorCertificatePath = supervisorCertPath
                        };

                        db.OathRequests.Add(oathRequest);
                        db.SaveChanges();

                        AuditService.LogAction("Create Oath Request", "OathRequests", $"Request created for Trainee ID {viewModel.TraineeId}");

                        TempData["SuccessMessage"] = "تم تقديم طلب أداء اليمين بنجاح.";
                        return RedirectToAction("Details", "RegisteredTrainees", new { id = viewModel.TraineeId });
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ غير متوقع: " + ex.Message);
                }
            }
            return View(viewModel);
        }

        // ============================================================
        // 3. مراجعة الطلب والموافقة (Review & Approve)
        // ============================================================
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ReviewDetails(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var oathRequest = db.OathRequests
                .Include(o => o.Trainee.ApplicationStatus)
                .FirstOrDefault(o => o.Id == id);

            if (oathRequest == null)
            {
                TempData["ErrorMessage"] = "الطلب غير موجود.";
                return RedirectToAction("ReviewList");
            }

            // تجميع الرسوم المطلوبة
            var feesToDisplay = new List<FeeSelectionViewModel>();
            var trainee = oathRequest.Trainee;

            // جلب الرسوم الفعالة
            var allActiveFees = db.FeeTypes
                .Include(f => f.Currency).Include(f => f.BankAccount)
                .Where(f => f.IsActive).ToList();

            // =================================================================================
            // أ. تحديد الرسوم المطلوبة بناءً على الكلمات المفتاحية في ملف Seed
            // =================================================================================

            // الكلمات التي نبحث عنها في أسماء الرسوم
            var targetKeywords = new List<string>
    {
        "انتماء مزاولة", // رسوم انتماء مزاولة (أول مرة)
        "إجازة المحاماة", // رسوم شهادة إجازة المحاماة
        "بطاقة المزاولة", // رسوم بطاقة المزاولة (الكارنيه)
        "صندوق التعاون",  // رسوم صندوق التعاون
        "الزمالة",        // رسوم الزمالة
        "تقاعد"           // رسوم تقاعد (بكافة فئاته)
    };

            foreach (var fee in allActiveFees)
            {
                // هل اسم الرسم يحتوي على أي من الكلمات المفتاحية؟
                if (targetKeywords.Any(k => fee.Name.Contains(k)))
                {
                    // منطق خاص لرسوم التقاعد: نحاول تحديد الفئة العمرية تلقائياً
                    // (اختياري: يمكنك تركه للموظف ليختار يدوياً)
                    bool preSelect = true;
                    if (fee.Name.Contains("تقاعد"))
                    {
                        // لا نحدد التقاعد افتراضياً لأن هناك 4 فئات، نترك للموظف اختيار الفئة المناسبة
                        // أو يمكننا حساب العمر وتحديد الفئة (كود إضافي ذكي):
                        int age = DateTime.Now.Year - trainee.BirthDate.Year;
                        if (fee.Name.Contains("الفئة الأولى") && age <= 30) preSelect = true;
                        else if (fee.Name.Contains("الفئة الثانية") && age > 30 && age <= 40) preSelect = true;
                        else if (fee.Name.Contains("الفئة الثالثة") && age > 40 && age <= 50) preSelect = true;
                        else if (fee.Name.Contains("الفئة الرابعة") && age > 50) preSelect = true;
                        else preSelect = false; // لا نحدد الفئات غير المطابقة للعمر
                    }

                    feesToDisplay.Add(new FeeSelectionViewModel
                    {
                        FeeTypeId = fee.Id,
                        FeeTypeName = fee.Name,
                        Amount = fee.DefaultAmount,
                        IsSelected = preSelect, // التحديد الافتراضي
                        CurrencySymbol = fee.Currency?.Symbol,
                        BankName = fee.BankAccount?.BankName,
                        AccountNumber = fee.BankAccount?.AccountNumber,
                        Iban = fee.BankAccount?.Iban
                    });
                }
            }

            // ب. إضافة الديون السابقة (كما هي)
            var deferredFees = db.DeferredFees
                .Include(d => d.FeeType.Currency).Include(d => d.FeeType.BankAccount)
                .Where(d => d.GraduateApplicationId == trainee.Id && !d.IsCharged)
                .ToList();

            foreach (var debt in deferredFees)
            {
                feesToDisplay.Add(new FeeSelectionViewModel
                {
                    FeeTypeId = debt.FeeTypeId,
                    FeeTypeName = $"دين مؤجل: {debt.FeeType.Name} (بتاريخ {debt.DateDeferred:yyyy-MM-dd})",
                    Amount = debt.Amount,
                    IsSelected = true,
                    CurrencySymbol = debt.FeeType.Currency?.Symbol,
                    BankName = debt.FeeType.BankAccount?.BankName,
                    AccountNumber = debt.FeeType.BankAccount?.AccountNumber,
                    Iban = debt.FeeType.BankAccount?.Iban
                });
            }

            var viewModel = new OathRequestReviewViewModel
            {
                OathRequest = oathRequest,
                // ترتيب العرض: الديون أولاً، ثم الرسوم الأساسية
                AvailableFees = feesToDisplay.OrderByDescending(f => f.FeeTypeName.Contains("دين")).ThenBy(f => f.FeeTypeName).ToList()
            };

            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ApproveAndCreateVoucher(OathRequestReviewViewModel viewModel)
        {
            var oathRequest = db.OathRequests.Include(o => o.Trainee).FirstOrDefault(o => o.Id == viewModel.OathRequest.Id);
            if (oathRequest == null) return HttpNotFound();

            var selectedFees = viewModel.AvailableFees?.Where(f => f.IsSelected).ToList();
            if (selectedFees == null || !selectedFees.Any())
            {
                TempData["ErrorMessage"] = "يجب اختيار رسم واحد على الأقل.";
                return RedirectToAction("ReviewDetails", new { id = oathRequest.Id });
            }

            // 1. إنشاء القسيمة المالية المجمعة
            var voucher = CreateBatchPaymentVoucher(oathRequest.GraduateApplicationId, selectedFees, $"رسوم أداء اليمين للمتدرب {oathRequest.Trainee.ArabicName}");

            if (voucher != null)
            {
                // 2. تحديث حالة الطلب
                oathRequest.Status = "بانتظار دفع رسوم اليمين";
                oathRequest.CommitteeNotes = viewModel.CommitteeNotes;
                oathRequest.PaymentVoucherId = voucher.Id;

                // 3. تسوية الديون المؤجلة (تغيير حالتها إلى IsCharged = true)
                var traineeId = oathRequest.GraduateApplicationId;
                var selectedFeeTypes = selectedFees.Select(f => f.FeeTypeId).ToList();

                // (نبحث عن الديون التي تم اختيار رسومها ونسددها)
                // ملاحظة: هذا منطق مبسط، في الواقع قد نحتاج لربط الدين بالقسيمة مباشرة
                var debtsToSettle = db.DeferredFees.Where(d => d.GraduateApplicationId == traineeId && !d.IsCharged && selectedFeeTypes.Contains(d.FeeTypeId)).ToList();
                foreach (var debt in debtsToSettle)
                {
                    debt.IsCharged = true; // تم تحميلها على قسيمة
                }

                db.SaveChanges();

                AuditService.LogAction("Approve Oath Request", "OathRequests", $"Approved request {oathRequest.Id} and created voucher {voucher.Id}");

                TempData["SuccessMessage"] = "تمت الموافقة وإنشاء القسيمة بنجاح.";
                return RedirectToAction("PrintVoucher", "PaymentVouchers", new { id = voucher.Id, area = "Admin" });
            }

            TempData["ErrorMessage"] = "فشل إنشاء القسيمة.";
            return RedirectToAction("ReviewDetails", new { id = oathRequest.Id });
        }

        // ============================================================
        // 4. المرفقات والمساعدات
        // ============================================================
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult GetOathAttachment(int? id, string type)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var request = db.OathRequests.Find(id.Value);
            if (request == null) return HttpNotFound();

            string filePath = (type == "completion") ? request.CompletionFormPath : request.SupervisorCertificatePath;
            if (string.IsNullOrEmpty(filePath)) return HttpNotFound();

            string physicalPath = Server.MapPath(filePath);
            if (!System.IO.File.Exists(physicalPath)) return HttpNotFound();

            return File(physicalPath, MimeMapping.GetMimeMapping(physicalPath));
        }

        private string SaveFile(HttpPostedFileBase file, int id, string subFolder)
        {
            if (file == null || file.ContentLength == 0) return null;
            try
            {
                string directoryPath = Server.MapPath($"~/Uploads/{subFolder}/{id}");
                if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
                string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                string path = Path.Combine(directoryPath, fileName);
                file.SaveAs(path);
                return $"/Uploads/{subFolder}/{id}/{fileName}";
            }
            catch { return null; }
        }

        private PaymentVoucher CreateBatchPaymentVoucher(int traineeId, List<FeeSelectionViewModel> selectedFees, string description)
        {
            try
            {
                var selectedIds = selectedFees.Select(f => f.FeeTypeId).ToList();
                var feeTypes = db.FeeTypes.Where(f => selectedIds.Contains(f.Id)).ToList(); // للحصول على BankAccountId

                var details = new List<VoucherDetail>();
                decimal total = 0;

                foreach (var item in selectedFees)
                {
                    var feeType = feeTypes.FirstOrDefault(f => f.Id == item.FeeTypeId);
                    if (feeType != null)
                    {
                        details.Add(new VoucherDetail
                        {
                            FeeTypeId = item.FeeTypeId,
                            Amount = item.Amount,
                            BankAccountId = feeType.BankAccountId,
                            Description = item.FeeTypeName
                        });
                        total += item.Amount;
                    }
                }

                var voucher = new PaymentVoucher
                {
                    GraduateApplicationId = traineeId,
                    IssueDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(14),
                    Status = "صادر", // Pending payment
                    TotalAmount = total,
                    IssuedByUserId = (int)Session["UserId"],
                    IssuedByUserName = Session["FullName"] as string,
                    VoucherDetails = details
                };

                db.PaymentVouchers.Add(voucher);
                // لا نحفظ هنا، الحفظ يتم في الأكشن الرئيسي
                return voucher;
            }
            catch { return null; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}