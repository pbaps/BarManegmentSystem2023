using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class LoanApplicationsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // دالة مساعدة للحصول على معرف المستخدم الحالي
        private int GetCurrentUserId()
        {
            if (Session["UserId"] == null) return -1;
            return (int)Session["UserId"];
        }

        // ============================================================
        // === 1. الفهرس (Index) ===
        // ============================================================
        public ActionResult Index(string searchString)
        {
            var query = db.LoanApplications
                .Include(l => l.Lawyer)
                .Include(l => l.LoanType)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l => l.Lawyer.ArabicName.Contains(searchString) || l.Lawyer.MembershipId == searchString);
            }
            return View(query.OrderByDescending(l => l.ApplicationDate).ToList());
        }

        // ============================================================
        // === 2. التفاصيل (Details) ===
        // ============================================================
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var loan = db.LoanApplications
                .Include(l => l.Lawyer)
                .Include(l => l.LoanType)
                .Include(l => l.Guarantors.Select(g => g.LawyerGuarantor))
                .Include(l => l.Installments)
                .FirstOrDefault(l => l.Id == id);

            if (loan == null) return HttpNotFound();
            return View(loan);
        }

        // ============================================================
        // === 3. الإنشاء (Create) ===
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create()
        {
            var viewModel = new LoanApplicationViewModel
            {
                ApplicationDate = DateTime.Now,
                StartDate = DateTime.Now.AddMonths(1),
                LoanTypesList = new SelectList(db.LoanTypes, "Id", "Name")
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(LoanApplicationViewModel viewModel)
        {
            // التحقق من صحة البيانات
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // 1. العثور على المحامي
                        var lawyer = db.GraduateApplications.FirstOrDefault(g =>
                            g.NationalIdNumber == viewModel.LawyerIdentifier ||
                            g.MembershipId == viewModel.LawyerIdentifier);

                        if (lawyer == null)
                        {
                            ModelState.AddModelError("LawyerIdentifier", "لم يتم العثور على محامٍ بهذا الرقم.");
                            throw new Exception("بيانات المحامي غير صحيحة");
                        }

                        // 2. حفظ طلب القرض (بيانات فقط بدون ملفات)
                        var loanApp = new LoanApplication
                        {
                            LawyerId = lawyer.Id,
                            LoanTypeId = viewModel.LoanTypeId,
                            Amount = viewModel.Amount,
                            InstallmentCount = viewModel.InstallmentCount,
                            ApplicationDate = viewModel.ApplicationDate,
                            StartDate = viewModel.StartDate,
                            Notes = viewModel.Notes,
                            Status = "جديد",
                            IsDisbursed = false
                        };

                        // ❌ تم حذف كود حفظ الملفات من هنا لأننا لم ننشئ القرض بعد لطباعة نماذجه

                        db.LoanApplications.Add(loanApp);
                        db.SaveChanges(); // هنا حصلنا على ID القرض

                        // 3. حفظ بيانات الكفلاء (بيانات فقط)
                        if (viewModel.Guarantors != null && viewModel.Guarantors.Any())
                        {
                            foreach (var gModel in viewModel.Guarantors)
                            {
                                var guarantor = new Guarantor
                                {
                                    LoanApplicationId = loanApp.Id,
                                    GuarantorType = gModel.GuarantorType,
                                    // ... باقي البيانات ...
                                    IsOverride = gModel.IsOverride,
                                    ExternalName = gModel.ExternalName,
                                    ExternalIdNumber = gModel.ExternalIdNumber,
                                    JobTitle = gModel.JobTitle,
                                    Workplace = gModel.Workplace,
                                    WorkplaceEmployeeId = gModel.WorkplaceEmployeeId,
                                    NetSalary = gModel.NetSalary,
                                    BankName = gModel.BankName,
                                    BankAccountNumber = gModel.BankAccountNumber
                                };

                                if (gModel.GuarantorType == "Lawyer")
                                {
                                    var lawyerGuarantor = db.GraduateApplications.FirstOrDefault(gl =>
                                        gl.NationalIdNumber == gModel.LawyerIdentifier ||
                                        gl.MembershipId == gModel.LawyerIdentifier);

                                    if (lawyerGuarantor != null) guarantor.LawyerGuarantorId = lawyerGuarantor.Id;
                                }

                                // ❌ تم حذف كود حفظ ملف الكفالة من هنا أيضاً

                                db.Guarantors.Add(guarantor);
                            }
                            db.SaveChanges();
                        }

                        transaction.Commit();
                        AuditService.LogAction("Create Loan", "LoanApplications", $"Created Loan #{loanApp.Id}");

                        // ✅ التغيير الجوهري: توجيه لصفحة التفاصيل للبدء بطباعة ورفع النماذج
                        TempData["SuccessMessage"] = "تم إنشاء القرض بنجاح. يرجى الآن طباعة النماذج المطلوبة، توقيعها، ثم رفعها في الأسفل.";
                        return RedirectToAction("Details", new { id = loanApp.Id });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        if (ModelState.IsValid) ModelState.AddModelError("", "خطأ: " + ex.Message);
                    }
                }
            }

            viewModel.LoanTypesList = new SelectList(db.LoanTypes, "Id", "Name", viewModel.LoanTypeId);
            return View(viewModel);
        }

        // ============================================================
        // === 4. التعديل (Edit) ===
        // ============================================================
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var loan = db.LoanApplications
                .Include(l => l.Lawyer)
                .Include(l => l.Guarantors) // وتأكد من تضمين الكفلاء المحامين أيضاً
                .Include(l => l.Guarantors.Select(g => g.LawyerGuarantor))
                .FirstOrDefault(l => l.Id == id);

            if (loan == null) return HttpNotFound();

            if (loan.IsDisbursed)
            {
                TempData["ErrorMessage"] = "لا يمكن تعديل طلب تم صرفه بالفعل.";
                return RedirectToAction("Details", new { id = id });
            }

            var viewModel = new LoanApplicationViewModel
            {
                Id = loan.Id,
                LawyerIdentifier = loan.Lawyer.NationalIdNumber,
                LoanTypeId = loan.LoanTypeId,
                Amount = loan.Amount,
                InstallmentCount = loan.InstallmentCount,
                ApplicationDate = loan.ApplicationDate,
                StartDate = loan.StartDate,
                Notes = loan.Notes,
                LoanTypesList = new SelectList(db.LoanTypes, "Id", "Name", loan.LoanTypeId),

                Guarantors = loan.Guarantors.Select(g => new GuarantorViewModel
                {
                    GuarantorType = g.GuarantorType,
                    LawyerIdentifier = g.LawyerGuarantor != null ? g.LawyerGuarantor.NationalIdNumber : "",
                    IsOverride = g.IsOverride,
                    ExternalName = g.ExternalName,
                    ExternalIdNumber = g.ExternalIdNumber,
                    JobTitle = g.JobTitle,
                    Workplace = g.Workplace,
                    WorkplaceEmployeeId = g.WorkplaceEmployeeId,
                    NetSalary = g.NetSalary,
                    BankName = g.BankName,
                    BankAccountNumber = g.BankAccountNumber
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Edit(LoanApplicationViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var loan = db.LoanApplications.Include(l => l.Guarantors).FirstOrDefault(l => l.Id == viewModel.Id);
                        if (loan == null) return HttpNotFound();
                        if (loan.IsDisbursed) throw new Exception("لا يمكن تعديل قرض تم صرفه.");

                        loan.LoanTypeId = viewModel.LoanTypeId;
                        loan.Amount = viewModel.Amount;
                        loan.InstallmentCount = viewModel.InstallmentCount;
                        loan.ApplicationDate = viewModel.ApplicationDate;
                        loan.StartDate = viewModel.StartDate;
                        loan.Notes = viewModel.Notes;

                        db.Guarantors.RemoveRange(loan.Guarantors);

                        if (viewModel.Guarantors != null && viewModel.Guarantors.Any())
                        {
                            foreach (var gModel in viewModel.Guarantors)
                            {
                                var guarantor = new Guarantor
                                {
                                    LoanApplicationId = loan.Id,
                                    GuarantorType = gModel.GuarantorType
                                };

                                if (gModel.GuarantorType == "Lawyer")
                                {
                                    var lawyerGuarantor = db.GraduateApplications.FirstOrDefault(gl =>
                                        gl.NationalIdNumber == gModel.LawyerIdentifier ||
                                        gl.MembershipId == gModel.LawyerIdentifier);

                                    if (lawyerGuarantor != null) guarantor.LawyerGuarantorId = lawyerGuarantor.Id;
                                    guarantor.IsOverride = gModel.IsOverride;
                                }
                                else
                                {
                                    guarantor.ExternalName = gModel.ExternalName;
                                    guarantor.ExternalIdNumber = gModel.ExternalIdNumber;
                                    guarantor.JobTitle = gModel.JobTitle;
                                    guarantor.Workplace = gModel.Workplace;
                                    guarantor.WorkplaceEmployeeId = gModel.WorkplaceEmployeeId;
                                    guarantor.NetSalary = gModel.NetSalary;
                                    guarantor.BankName = gModel.BankName;
                                    guarantor.BankAccountNumber = gModel.BankAccountNumber;
                                }
                                db.Guarantors.Add(guarantor);
                            }
                        }

                        db.Entry(loan).State = EntityState.Modified;
                        db.SaveChanges();
                        transaction.Commit();

                        AuditService.LogAction("Edit Loan", "LoanApplications", $"Edited Loan #{loan.Id}");
                        TempData["SuccessMessage"] = "تم تعديل الطلب بنجاح.";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "خطأ أثناء التعديل: " + ex.Message);
                    }
                }
            }

            viewModel.LoanTypesList = new SelectList(db.LoanTypes, "Id", "Name", viewModel.LoanTypeId);
            return View(viewModel);
        }

        // ============================================================
        // === 5. توليد الأقساط والقسائم (معدلة) ===
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult GenerateInstallments(int id)
        {
            var loan = db.LoanApplications.Include(l => l.Lawyer).FirstOrDefault(l => l.Id == id);
            if (loan == null) return HttpNotFound();

            if (loan.Installments != null && loan.Installments.Any())
            {
                TempData["InfoMessage"] = "الأقساط مولدة بالفعل.";
                return RedirectToAction("Details", new { id = id });
            }

            // 1. تحديد نوع الرسم والحساب البنكي للقرض
            var loanFeeType = db.FeeTypes.Include(f => f.BankAccount)
                                         .FirstOrDefault(f => f.Name.Contains("قرض") || f.Name.Contains("سداد"));

            // في حال عدم وجود نوع رسم مخصص، نأخذ الأول (يجب ضبط هذا في الإعدادات)
            if (loanFeeType == null) loanFeeType = db.FeeTypes.Include(f => f.BankAccount).FirstOrDefault();

            int currentUserId = GetCurrentUserId();
            string currentUserName = Session["FullName"] as string ?? "System";
            decimal monthlyAmount = loan.Amount / loan.InstallmentCount;
            DateTime dueDate = loan.StartDate;

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    for (int i = 1; i <= loan.InstallmentCount; i++)
                    {
                        // أ. إنشاء القسيمة أولاً
                        var voucher = new PaymentVoucher
                        {
                            GraduateApplicationId = loan.LawyerId,
                            IssueDate = DateTime.Now,
                            ExpiryDate = dueDate, // تاريخ انتهاء القسيمة هو تاريخ استحقاق القسط
                            Status = "غير مدفوع", // حالة جديدة
                            TotalAmount = monthlyAmount,
                            PaymentMethod = "إيداع بنكي",
                            IssuedByUserId = currentUserId,
                            IssuedByUserName = currentUserName,
                            CheckNumber = "",
                            ReferenceNumber = "", // سيتم ملؤه عند السداد
                            VoucherDetails = new List<VoucherDetail>()
                        };

                        voucher.VoucherDetails.Add(new VoucherDetail
                        {
                            FeeTypeId = loanFeeType.Id,
                            Amount = monthlyAmount,
                            Description = $"قسط قرض رقم {i} من {loan.InstallmentCount} - {loan.Lawyer.ArabicName}",
                            BankAccountId = loanFeeType.BankAccountId // تثبيت البنك المطلوب السداد عليه
                        });

                        db.PaymentVouchers.Add(voucher);
                        db.SaveChanges(); // حفظ للحصول على ID

                        // ب. إنشاء القسط وربطه بالقسيمة
                        var installment = new LoanInstallment
                        {
                            LoanApplicationId = loan.Id,
                            InstallmentNumber = i,
                            DueDate = dueDate,
                            Amount = monthlyAmount,
                            IsPaid = false,
                            Status = "غير مدفوع",
                            PaymentVoucherId = voucher.Id // ✅ الربط هنا
                        };
                        db.LoanInstallments.Add(installment);

                        // الانتقال للشهر التالي
                        dueDate = dueDate.AddMonths(1);
                    }

                    loan.Status = "بانتظار الصرف";
                    db.SaveChanges();
                    transaction.Commit();

                    AuditService.LogAction("Approve Loan", "LoanApplications", $"Generated {loan.InstallmentCount} vouchers for Loan #{loan.Id}");
                    TempData["SuccessMessage"] = "تم إنشاء الأقساط وإصدار دفاتر السداد بنجاح.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "حدث خطأ: " + ex.Message;
                }
            }

            return RedirectToAction("Details", new { id = id });
        }

        // ============================================================
        // === 8. طباعة دفتر الأقساط (جديد) ===
        // ============================================================
        public ActionResult PrintLoanBooklet(int id)
        {
            var loan = db.LoanApplications
                .Include(l => l.Lawyer)
                .Include(l => l.Installments.Select(i => i.PaymentVoucher.VoucherDetails.Select(d => d.BankAccount)))
                .FirstOrDefault(l => l.Id == id);

            if (loan == null) return HttpNotFound();

            // جلب بيانات البنك من القسط الأول (بافتراض أن جميع الأقساط لنفس البنك)
            var firstVoucher = loan.Installments.FirstOrDefault()?.PaymentVoucher;
            var bankInfo = firstVoucher?.VoucherDetails.FirstOrDefault()?.BankAccount;

            ViewBag.BankName = bankInfo?.BankName ?? "غير محدد";
            ViewBag.AccountNumber = bankInfo?.AccountNumber ?? "---";
            ViewBag.IBAN = bankInfo?.Iban ?? "---"; // تأكد أن الموديل BankAccount يحتوي على IBAN

            return View(loan);
        }

        // ============================================================
        // === 6. صرف القرض (Disburse) ===
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult DisburseLoan(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var loanApp = db.LoanApplications.Include(l => l.LoanType).Include(l => l.Lawyer).FirstOrDefault(l => l.Id == id);
                    if (loanApp == null) return HttpNotFound();

                    if (loanApp.IsDisbursed)
                    {
                        TempData["InfoMessage"] = "هذا القرض تم صرفه مسبقاً.";
                        return RedirectToAction("Details", new { id = id });
                    }

                    // 1. تحديث الحالة
                    loanApp.IsDisbursed = true;
                    loanApp.DisbursementDate = DateTime.Now;
                    loanApp.Status = "مفعل (تم الصرف)";
                    db.Entry(loanApp).State = EntityState.Modified;
                    db.SaveChanges();

                    // 2. القيد المحاسبي
                    bool entryCreated = false;
                    using (var accService = new AccountingService())
                    {
                        entryCreated = accService.GenerateEntryForLoanDisbursement(loanApp.Id, GetCurrentUserId());
                    }

                    if (!entryCreated) throw new Exception("تم تحديث الحالة ولكن فشل إنشاء القيد المحاسبي (تأكد من إعدادات الحسابات).");

                    transaction.Commit();
                    TempData["SuccessMessage"] = "تم صرف القرض وإنشاء القيد المحاسبي بنجاح.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "خطأ أثناء الصرف: " + ex.Message;
                }
            }
            return RedirectToAction("Details", new { id = id });
        }

        // ============================================================
        // === 7. رفع الملفات (Upload) ===
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult UploadLoanDocument(int id, string docType, HttpPostedFileBase file)
        {
            var loan = db.LoanApplications.Find(id);
            if (loan == null) return HttpNotFound();

            if (file != null && file.ContentLength > 0)
            {
                string path = SaveFile(file, loan.LawyerId, "Docs");

                if (docType == "ApplicationForm") loan.ApplicationFormPath = path;
                else if (docType == "CouncilApproval") loan.CouncilApprovalPath = path;
                else if (docType == "MainPromissoryNote") loan.MainPromissoryNotePath = path;
                else if (docType == "DebtBond") loan.DebtBondPath = path;
                else if (docType.StartsWith("Guarantor_"))
                {
                    int gId = int.Parse(docType.Split('_')[1]);
                    var guarantor = db.Guarantors.Find(gId);
                    if (guarantor != null) guarantor.GuarantorFormScannedPath = path;
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = "تم رفع الملف بنجاح.";
            }
            return RedirectToAction("Details", new { id = id });
        }

        // دالة مساعدة للحفظ
        private string SaveFile(HttpPostedFileBase file, int lawyerId, string subFolder)
        {
            if (file == null || file.ContentLength == 0) return null;
            string folder = Server.MapPath($"~/Uploads/Loans/{lawyerId}/{subFolder}/");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            string path = Path.Combine(folder, fileName);
            file.SaveAs(path);
            return $"/Uploads/Loans/{lawyerId}/{subFolder}/{fileName}";
        }

        // ============================================================
        // === 9. دوال الطباعة (النماذج) - مفقودة ويجب إضافتها ===
        // ============================================================

        // 1. طباعة طلب القرض
        public ActionResult PrintApplicationForm(int id)
        {
            var loan = db.LoanApplications
                .Include(l => l.Lawyer)
                .Include(l => l.Lawyer.User)
                .Include(l => l.Lawyer.ApplicationStatus)
                .Include(l => l.Lawyer.ContactInfo) // تأكد من وجود هذا الجدول
                .Include(l => l.LoanType)
                .Include(l => l.LoanType.BankAccount)
                .Include(l => l.LoanType.BankAccount.Currency)
                .Include(l => l.Guarantors)
                .Include(l => l.Guarantors.Select(g => g.LawyerGuarantor))
                .Include(l => l.Guarantors.Select(g => g.LawyerGuarantor.ApplicationStatus))
                .FirstOrDefault(l => l.Id == id);

            if (loan == null) return HttpNotFound();

            ViewBag.Guarantors = loan.Guarantors.ToList();
            // يمكنك جلب اسم المحافظة الحقيقي هنا إذا كان لديك جدول محافظات
            ViewBag.ProvinceName = "غزة";

            return View(loan);
        }

        // 2. طباعة قرار المجلس
        public ActionResult PrintCouncilApproval(int id)
        {
            var loan = db.LoanApplications
                .Include(l => l.Lawyer)
                .Include(l => l.LoanType)
                .FirstOrDefault(l => l.Id == id);

            if (loan == null) return HttpNotFound();

            // جلب أعضاء المجلس الحاليين للطباعة
            // تأكد من أن لديك جدول CouncilMembers أو قم بتمرير قائمة وهمية إذا لم يوجد
            var councilMembers = db.CouncilMembers.Where(m => m.IsActive).ToList();
            ViewBag.CouncilMembers = councilMembers;

            return View(loan);
        }

        // 3. طباعة سند المديونية
        public ActionResult PrintDebtBond(int id)
        {
            var loan = db.LoanApplications
               .Include(l => l.Lawyer)
               .Include(l => l.Lawyer.User)
               .Include(l => l.LoanType)
               .Include(l => l.LoanType.BankAccount) // مهم لجلب العملة
               .Include(l => l.LoanType.BankAccount.Currency)
               .FirstOrDefault(l => l.Id == id);

            if (loan == null) return HttpNotFound();

            string currencyName = loan.LoanType.BankAccount?.Currency?.Name ?? "شيكل";
            ViewBag.AmountInWords = TafqeetHelper.ConvertToArabic(loan.Amount, currencyName);

            return View(loan);
        }

        // 4. طباعة الكمبيالة الكبرى (Main Promissory Note)
        public ActionResult PrintMainPromissoryNote(int id)
        {
            var loan = db.LoanApplications
               .Include(l => l.Lawyer)
               .Include(l => l.Lawyer.User)
               .Include(l => l.LoanType)
               .Include(l => l.LoanType.BankAccount)
               .Include(l => l.LoanType.BankAccount.Currency)
               .FirstOrDefault(l => l.Id == id);

            if (loan == null) return HttpNotFound();

            string currencyName = loan.LoanType.BankAccount?.Currency?.Name ?? "شيكل";
            ViewBag.AmountInWords = TafqeetHelper.ConvertToArabic(loan.Amount, currencyName);

            return View(loan);
        }

        // 5. طباعة نموذج الكفالة (ملاحظة: هذا يستقبل ID الكفيل وليس القرض)
        public ActionResult PrintGuarantorForm(int id)
        {
            var guarantor = db.Guarantors
                .Include(g => g.LoanApplication)
                .Include(g => g.LoanApplication.Lawyer)
                .Include(g => g.LoanApplication.Lawyer.User) // للمقترض
                .Include(g => g.LoanApplication.LoanType)
                .Include(g => g.LoanApplication.LoanType.BankAccount)
                .Include(g => g.LoanApplication.LoanType.BankAccount.Currency)

                .Include(g => g.LawyerGuarantor)
                .Include(g => g.LawyerGuarantor.User)
                // ✅✅ هذا السطر ضروري جداً لإظهار جوال وعنوان المحامي الكفيل ✅✅
                .Include(g => g.LawyerGuarantor.ContactInfo)

                .FirstOrDefault(g => g.Id == id);

            if (guarantor == null) return HttpNotFound();

            return View(guarantor);
        }


        // 2. طباعة قرار المجلس
 

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}