using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Areas.Admin.ViewModels;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.IO;
using System.Web;
using System.Collections.Generic;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class SupervisorChangeRequestsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Admin/SupervisorChangeRequests
        public ActionResult Index()
        {
            var requests = db.SupervisorChangeRequests
                .Include(r => r.Trainee)
                .Include(r => r.OldSupervisor)
                .Include(r => r.NewSupervisor)
                .OrderByDescending(r => r.RequestDate)
                .ToList();
            return View(requests);
        }

        // GET: Admin/SupervisorChangeRequests/SelectTrainee
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult SelectTrainee()
        {
            var trainees = db.GraduateApplications
                .Include(a => a.ApplicationStatus)
                .Where(a => a.ApplicationStatus.Name == "متدرب مقيد" || a.ApplicationStatus.Name == "متدرب موقوف")
                .OrderBy(a => a.ArabicName)
                .ToList();
            return View(trainees);
        }

        // GET: Admin/SupervisorChangeRequests/Create?traineeId=5
        // GET: Admin/SupervisorChangeRequests/Create?traineeId=5
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(int? traineeId)
        {
            if (traineeId == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            // (تعديل: جلب المشرف مع المتدرب)
            var trainee = db.GraduateApplications
                            .Include(t => t.ApplicationStatus)
                            .Include(t => t.Supervisor)
                            .FirstOrDefault(t => t.Id == traineeId);

            if (trainee == null) return HttpNotFound();

            if (trainee.ApplicationStatus.Name != "متدرب مقيد" && trainee.ApplicationStatus.Name != "متدرب موقوف")
            {
                TempData["ErrorMessage"] = "لا يمكن تقديم طلب لهذا المتدرب حالته الحالية لا تسمح بذلك.";
                return RedirectToAction("Details", "TraineeProfile", new { id = traineeId });
            }

            var viewModel = new SupervisorRequestViewModel
            {
                TraineeId = trainee.Id,
                TraineeName = trainee.ArabicName,
                // === 
                // === بداية الإضافة: تعبئة المشرف الحالي
                // ===
                CurrentSupervisorName = trainee.Supervisor?.ArabicName ?? "لا يوجد"
                // === نهاية الإضافة ===
            };
            return View(viewModel); // توجيه إلى Create.cshtml
        }

        // POST: Admin/SupervisorChangeRequests/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(SupervisorRequestViewModel viewModel)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });
            }

            // --- التحقق الإضافي بناءً على نوع الطلب ---
            if (string.IsNullOrEmpty(viewModel.RequestType))
            {
                ModelState.AddModelError("RequestType", "الرجاء اختيار نوع الطلب.");
            }
            else if (viewModel.RequestType == "نقل")
            {
                if (!viewModel.NewSupervisorId.HasValue || viewModel.NewSupervisorId <= 0)
                    ModelState.AddModelError("SupervisorSearch", "اختيار المشرف الجديد مطلوب لطلب النقل.");
                if (viewModel.NewSupervisorApprovalFile == null || viewModel.NewSupervisorApprovalFile.ContentLength == 0)
                    ModelState.AddModelError("NewSupervisorApprovalFile", "مرفق موافقة المشرف الجديد مطلوب لطلب النقل.");
            }

            if (ModelState.IsValid)
            {
                var trainee = db.GraduateApplications.Find(viewModel.TraineeId);
                if (trainee == null) return HttpNotFound();

                FeeType requiredFeeType = null;
                string feeDescription = "";
                string successMessage = "";
                string finalStatus = "قيد المراجعة";
                bool isDeferred = viewModel.DeferPayment;

                if (viewModel.RequestType == "نقل")
                {
                    requiredFeeType = db.FeeTypes.FirstOrDefault(f => f.IsActive && f.Name.Contains("نقل إشراف"));
                    if (requiredFeeType == null)
                        ModelState.AddModelError("", "خطأ: لم يتم العثور على 'رسوم نقل إشراف' نشطة.");
                    else
                    {
                        feeDescription = $"رسوم نقل إشراف للمتدرب {trainee.ArabicName}";
                        finalStatus = isDeferred ? "قيد المراجعة (دفع مؤجل)" : "بانتظار دفع الرسوم";
                        successMessage = isDeferred ? "تم تقديم طلب النقل وتأجيل الرسوم." : "تم تقديم طلب نقل الإشراف بنجاح. يرجى سداد قسيمة الدفع.";
                    }
                }
                else if (viewModel.RequestType == "استكمال")
                {
                    requiredFeeType = db.FeeTypes.FirstOrDefault(f => f.IsActive && f.Name.Contains("استئناف تدريب"));
                    if (requiredFeeType == null)
                        ModelState.AddModelError("", "خطأ: لم يتم العثور على 'رسوم استئناف تدريب' نشطة.");
                    else
                    {
                        feeDescription = $"رسوم استئناف تدريب للمتدرب {trainee.ArabicName}";
                        finalStatus = isDeferred ? "قيد المراجعة (دفع مؤجل)" : "بانتظار دفع الرسوم";
                        successMessage = isDeferred ? "تم تقديم طلب الاستكمال وتأجيل الرسوم." : "تم تقديم طلب الاستكمال بنجاح. يرجى سداد قسيمة الدفع.";
                    }
                }
                else // طلب "وقف"
                {
                    successMessage = "تم تقديم طلب الوقف بنجاح وهو الآن قيد المراجعة.";
                }

                if (!ModelState.IsValid)
                {
                    viewModel.TraineeName = trainee.ArabicName;
                    return View(viewModel);
                }

                var request = new SupervisorChangeRequest
                {
                    TraineeId = viewModel.TraineeId,
                    RequestType = viewModel.RequestType,
                    RequestDate = DateTime.Now,
                    OldSupervisorId = trainee.SupervisorId,
                    NewSupervisorId = (viewModel.RequestType == "نقل" || viewModel.RequestType == "استكمال") ? viewModel.NewSupervisorId : null,
                    CommitteeNotes = viewModel.Reason,
                    Status = finalStatus
                };

                if (viewModel.OldSupervisorApprovalFile != null)
                    request.OldSupervisorApprovalPath = SaveFile(viewModel.OldSupervisorApprovalFile, trainee.Id);
                if (viewModel.NewSupervisorApprovalFile != null)
                    request.NewSupervisorApprovalPath = SaveFile(viewModel.NewSupervisorApprovalFile, trainee.Id);

                db.SupervisorChangeRequests.Add(request);

                PaymentVoucher voucher = null;
                DeferredFee deferredFee = null;

                if (requiredFeeType != null)
                {
                    if (isDeferred)
                    {
                        deferredFee = new DeferredFee
                        {
                            GraduateApplicationId = trainee.Id,
                            FeeTypeId = requiredFeeType.Id,
                            Amount = requiredFeeType.DefaultAmount,
                            Reason = feeDescription,
                            DateDeferred = DateTime.Now,
                            IsCharged = false
                        };
                        db.DeferredFees.Add(deferredFee);
                    }
                    else
                    {
                        voucher = CreatePaymentVoucher(trainee.Id, requiredFeeType.Id, feeDescription);
                        if (voucher == null)
                        {
                            TempData["ErrorMessage"] = "حدث خطأ أثناء إنشاء قسيمة الدفع. لم يتم حفظ الطلب.";
                            viewModel.TraineeName = trainee.ArabicName;
                            return View(viewModel);
                        }
                        db.PaymentVouchers.Add(voucher);
                    }
                }

                try
                {
                    db.SaveChanges();

                    if (voucher != null)
                    {
                        request.PaymentVoucherId = voucher.Id;
                        db.Entry(request).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ أثناء حفظ الطلب: " + (ex.InnerException?.Message ?? ex.Message));
                    viewModel.TraineeName = trainee.ArabicName;
                    if (voucher != null) db.PaymentVouchers.Local.Remove(voucher);
                    if (deferredFee != null) db.DeferredFees.Local.Remove(deferredFee);
                    db.SupervisorChangeRequests.Local.Remove(request);
                    return View(viewModel);
                }

                TempData["SuccessMessage"] = successMessage;
                if (voucher != null)
                {
                    return RedirectToAction("PrintVoucher", "PaymentVouchers", new { area = "Admin", id = voucher.Id });
                }
                else
                {
                    return RedirectToAction("Details", "TraineeProfile", new { area = "Admin", id = viewModel.TraineeId });
                }
            }

            var originalTrainee = db.GraduateApplications.Find(viewModel.TraineeId);
            viewModel.TraineeName = originalTrainee?.ArabicName ?? viewModel.TraineeName;
            return View(viewModel);
        }

        // GET: Admin/SupervisorChangeRequests/CommitteeReview
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult CommitteeReview()
        {
            var pendingRequests = db.SupervisorChangeRequests
                .Include(r => r.Trainee.Supervisor)
                .Include(r => r.NewSupervisor)
                .Where(r => r.Status == "قيد المراجعة" ||
                            r.Status == "قيد المراجعة (دفع مؤجل)" ||
                            r.Status == "بانتظار دفع الرسوم")
                .OrderBy(r => r.RequestDate)
                .ToList();
            return View(pendingRequests);
        }

        // GET: Admin/SupervisorChangeRequests/RequestDetails/5
        public ActionResult RequestDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var request = db.SupervisorChangeRequests
                .Include(r => r.Trainee.Supervisor)
                .Include(r => r.Trainee.ApplicationStatus)
                .Include(r => r.NewSupervisor)
                .FirstOrDefault(r => r.Id == id);

            if (request == null)
            {
                return HttpNotFound();
            }

            // (التحقق من أن الطلب لا يزال قيد المعالجة)
            if (request.Status == "معتمد" || request.Status == "مرفوض")
            {
                TempData["InfoMessage"] = "لقد تمت معالجة هذا الطلب مسبقاً.";
            }

            var viewModel = new RequestReviewViewModel
            {
                Request = request,
                Trainee = request.Trainee
            };

            return View(viewModel);
        }

        // POST: Admin/SupervisorChangeRequests/RejectRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectRequest(int id, string committeeNotes)
        {
            var request = db.SupervisorChangeRequests.Find(id);
            if (request == null) return HttpNotFound();

            request.Status = "مرفوض";
            request.CommitteeNotes = committeeNotes;
            request.DecisionDate = DateTime.Now;
            db.Entry(request).State = EntityState.Modified;

            if (request.Status.Contains("دفع مؤجل"))
            {
                var deferredFee = db.DeferredFees
                    .Include(d => d.FeeType)
                    .FirstOrDefault(d => d.GraduateApplicationId == request.TraineeId &&
                                        d.IsCharged == false &&
                                        d.FeeType.Name.Contains(request.RequestType));

                if (deferredFee != null)
                {
                    db.DeferredFees.Remove(deferredFee);
                }
            }

            db.SaveChanges();
            TempData["InfoMessage"] = "تم رفض الطلب.";

            return RedirectToAction("CommitteeReview");
        }

        // POST: Admin/SupervisorChangeRequests/ApproveResumeRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ApproveResumeRequest(int id)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });
            }

            var resumeRequest = db.SupervisorChangeRequests.Find(id);

            if (resumeRequest == null || (resumeRequest.RequestType != "استكمال" && resumeRequest.RequestType != "استئناف تدريب"))
            {
                return HttpNotFound("الطلب غير موجود أو ليس طلب استئناف.");
            }

            if (resumeRequest.Status != "قيد المراجعة" && resumeRequest.Status != "قيد المراجعة (دفع مؤجل)")
            {
                TempData["ErrorMessage"] = "لا يمكن الموافقة على هذا الطلب لأنه ليس قيد المراجعة.";
                return RedirectToAction("CommitteeReview");
            }

            var trainee = db.GraduateApplications
                            .Include(g => g.ApplicationStatus)
                            .FirstOrDefault(g => g.Id == resumeRequest.TraineeId);

            if (trainee == null || trainee.ApplicationStatus.Name != "متدرب موقوف")
            {
                TempData["ErrorMessage"] = "لا يمكن استئناف تدريب هذا المتدرب لأنه ليس في حالة (متدرب موقوف).";
                return RedirectToAction("CommitteeReview");
            }

            var activeSuspension = db.TraineeSuspensions
                .Where(s => s.GraduateApplicationId == trainee.Id &&
                            s.Status == "معتمد" &&
                            s.SuspensionEndDate == null)
                .OrderByDescending(s => s.SuspensionStartDate)
                .FirstOrDefault();

            try
            {
                if (activeSuspension != null)
                {
                    activeSuspension.SuspensionEndDate = DateTime.Now;
                    activeSuspension.Status = "منتهية";
                    db.Entry(activeSuspension).State = EntityState.Modified;
                }

                var activeStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "متدرب مقيد");
                trainee.ApplicationStatusId = activeStatus.Id;

                // (إضافة: إعادة المشرف الجديد إذا تم تحديده عند الاستئناف)
                if (resumeRequest.NewSupervisorId.HasValue)
                {
                    trainee.SupervisorId = resumeRequest.NewSupervisorId;
                }

                db.Entry(trainee).State = EntityState.Modified;

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

            return RedirectToAction("CommitteeReview");
        }


        // === 
        // === بداية الإضافة: الدوال المفقودة التي تسبب خطأ 404
        // ===

        // POST: Admin/SupervisorChangeRequests/ApproveTransferRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ApproveTransferRequest(int id, string committeeNotes)
        {
            var request = db.SupervisorChangeRequests.Include(r => r.Trainee).FirstOrDefault(r => r.Id == id);

            if (request == null || (request.RequestType != "نقل إشراف" && request.RequestType != "نقل")) return HttpNotFound();

            if (!request.NewSupervisorId.HasValue)
            {
                TempData["ErrorMessage"] = "خطأ: لا يمكن الموافقة على طلب النقل لعدم تحديد مشرف جديد.";
                return RedirectToAction("RequestDetails", new { id = id });
            }

            if (request.Status != "قيد المراجعة" && request.Status != "قيد المراجعة (دفع مؤجل)")
            {
                TempData["ErrorMessage"] = "لا يمكن الموافقة على هذا الطلب لأنه ليس قيد المراجعة.";
                return RedirectToAction("CommitteeReview");
            }

            var history = new SupervisorHistory
            {
                GraduateApplicationId = request.TraineeId,
                OldSupervisorId = request.Trainee.SupervisorId,
                NewSupervisorId = request.NewSupervisorId.Value,
                ChangeDate = DateTime.Now,
                // (تم حذف حقل 'Reason' ليتوافق مع المودل)
            };
            db.SupervisorHistories.Add(history);

            request.Trainee.SupervisorId = request.NewSupervisorId;
            db.Entry(request.Trainee).State = EntityState.Modified;

            request.Status = "معتمد";
            request.CommitteeNotes = committeeNotes;
            request.DecisionDate = DateTime.Now;
            db.Entry(request).State = EntityState.Modified;

            db.SaveChanges();
            TempData["SuccessMessage"] = "تم اعتماد طلب نقل الإشراف بنجاح.";
            return RedirectToAction("CommitteeReview");
        }

        // POST: Admin/SupervisorChangeRequests/ApproveStopRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult ApproveStopRequest(int id, string committeeNotes)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "AdminLogin", new { area = "Admin" });
            }

            var request = db.SupervisorChangeRequests.Include(r => r.Trainee).FirstOrDefault(r => r.Id == id);

            if (request == null || (request.RequestType != "وقف تدريب" && request.RequestType != "وقف")) return HttpNotFound();

            // (يجب أن يكون "قيد المراجعة" فقط، لأن الوقف ليس له رسوم أو تأجيل)
            if (request.Status != "قيد المراجعة")
            {
                TempData["ErrorMessage"] = "لا يمكن الموافقة على هذا الطلب لأنه ليس قيد المراجعة.";
                return RedirectToAction("CommitteeReview");
            }

            var stoppedStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "متدرب موقوف");
            if (stoppedStatus == null)
            {
                TempData["ErrorMessage"] = "خطأ فادح: لم يتم العثور على حالة (متدرب موقوف).";
                return RedirectToAction("RequestDetails", new { id = id });
            }

            // === 
            // === بداية التعديل: تطبيق إزالة المشرف
            // ===
            request.Trainee.ApplicationStatusId = stoppedStatus.Id;
            request.Trainee.SupervisorId = null; // (إزالة المشرف)
            db.Entry(request.Trainee).State = EntityState.Modified;
            // === نهاية التعديل ===

            request.Status = "معتمد";
            request.CommitteeNotes = committeeNotes;
            request.DecisionDate = DateTime.Now;
            db.Entry(request).State = EntityState.Modified;

            var suspension = new TraineeSuspension
            {
                GraduateApplicationId = request.TraineeId,
                Reason = "موافقة لجنة على طلب وقف تدريب رقم " + request.Id + ". " + committeeNotes,
                SuspensionStartDate = DateTime.Now,
                SuspensionEndDate = null, // إيقاف مفتوح
                DecisionDate = DateTime.Now,
                CreatedByUserId = (int)Session["UserId"],
                Status = "معتمد"
            };
            db.TraineeSuspensions.Add(suspension);

            db.SaveChanges();
            TempData["SuccessMessage"] = "تم اعتماد طلب وقف التدريب وتغيير حالة المتدرب إلى (موقوف).";
            return RedirectToAction("CommitteeReview");
        }

        // === نهاية الإضافة ===


        // --- الدوال المساعدة ---
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult GetRequestAttachment(int id, string type)
        {
            var request = db.SupervisorChangeRequests.Find(id);
            if (request == null) return HttpNotFound("الطلب غير موجود.");
            string filePath = null;
            if (type?.ToLower() == "old" && !string.IsNullOrEmpty(request.OldSupervisorApprovalPath))
                filePath = request.OldSupervisorApprovalPath;
            else if (type?.ToLower() == "new" && !string.IsNullOrEmpty(request.NewSupervisorApprovalPath))
                filePath = request.NewSupervisorApprovalPath;
            if (string.IsNullOrEmpty(filePath)) return HttpNotFound("مسار الملف غير محدد.");
            string physicalPath = Server.MapPath(filePath);
            if (!System.IO.File.Exists(physicalPath)) return HttpNotFound("الملف غير موجود على الخادم.");
            string contentType = MimeMapping.GetMimeMapping(physicalPath);
            var fileBytes = System.IO.File.ReadAllBytes(physicalPath);
            return File(fileBytes, contentType);
        }

        [HttpGet]
        public ActionResult SearchSupervisors(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            var practicingStatusId = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "محامي مزاول")?.Id;
            if (practicingStatusId == null)
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            var fiveYearsAgo = DateTime.Now.AddYears(-5);
            var supervisors = db.GraduateApplications
                .Where(s => s.ApplicationStatusId == practicingStatusId && s.SubmissionDate <= fiveYearsAgo)
                .Where(s => s.ArabicName.Contains(term) || s.Id.ToString().Contains(term))
                .Select(s => new
                {
                    id = s.Id,
                    label = s.ArabicName + " (الرقم: " + s.Id + ")",
                    value = s.ArabicName
                })
                .Take(15).ToList();
            return Json(supervisors, JsonRequestBehavior.AllowGet);
        }

        private string SaveFile(HttpPostedFileBase file, int traineeId)
        {
            if (file == null || file.ContentLength == 0) return null;
            var directoryPath = Server.MapPath($"~/Uploads/SupervisorRequests/{traineeId}");
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var path = Path.Combine(directoryPath, fileName);
            file.SaveAs(path);
            return $"/Uploads/SupervisorRequests/{traineeId}/{fileName}";
        }

        protected new PaymentVoucher CreatePaymentVoucher(int traineeId, int feeTypeId, string description)
        {
            if (Session["UserId"] == null)
            {
                return null;
            }
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
                    IssuedByUserId = (int)Session["UserId"],
                    IssuedByUserName = Session["FullName"] as string,
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
                // (تم نقل Add إلى دالة Create [POST])
                return voucher;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating payment voucher: {ex.Message}");
                return null;
            }
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