using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Tafqeet;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class PaymentVouchersController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. العرض الرئيسي (Index) - لوحة تحكم القسائم
        // ============================================================
        public ActionResult Index()
        {
            var excludedVoucherIds = new HashSet<int>(
                db.ContractTransactions.Where(c => c.PaymentVoucherId != null).Select(c => c.PaymentVoucherId.Value)
            );

            var loanIds = db.LoanInstallments.Where(i => i.PaymentVoucherId != null).Select(i => i.PaymentVoucherId.Value).ToList();
            foreach (var id in loanIds) excludedVoucherIds.Add(id);

            var allVouchersQuery = db.PaymentVouchers.AsNoTracking()
                .Include(v => v.GraduateApplication)
                .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency))
                .AsQueryable();

            var viewModel = new VoucherIndexViewModel();

            var pendingTrainee = allVouchersQuery
                .Where(v => (v.Status == "صادر" || v.Status == "بانتظار الدفع")
                            && v.GraduateApplicationId != null)
                .ToList();

            viewModel.UnpaidTraineeVouchers = pendingTrainee
                .Where(v => !excludedVoucherIds.Contains(v.Id))
                .OrderByDescending(v => v.IssueDate)
                .ToList();

            viewModel.UnpaidContractorVouchers = db.StampBookIssuances.AsNoTracking()
                .Include(i => i.Contractor)
                .Include(i => i.PaymentVoucher)
                .Where(i => i.PaymentVoucher != null && (i.PaymentVoucher.Status == "صادر" || i.PaymentVoucher.Status == "بانتظار الدفع"))
                .ToList()
                .GroupBy(i => i.PaymentVoucher)
                .Select(g => new ContractorVoucherDisplay
                {
                    Voucher = g.Key,
                    ContractorName = g.FirstOrDefault()?.Contractor?.Name ?? "متعهد عام"
                })
                .OrderByDescending(x => x.Voucher.IssueDate)
                .ToList();

            var pendingGeneral = allVouchersQuery
                .Where(v => (v.Status == "صادر" || v.Status == "بانتظار الدفع")
                            && v.GraduateApplicationId == null)
                .ToList();

            var contractorVoucherIds = db.StampBookIssuances.Select(i => i.PaymentVoucherId).ToList();
            var excludedGeneralIds = new HashSet<int>(contractorVoucherIds);
            excludedGeneralIds.UnionWith(excludedVoucherIds);

            viewModel.UnpaidGeneralVouchers = pendingGeneral
                .Where(v => !excludedGeneralIds.Contains(v.Id))
                .OrderByDescending(v => v.IssueDate)
                .ToList();

            viewModel.PaidVouchers = allVouchersQuery
                .Where(v => v.Status == "مسدد")
                .OrderByDescending(v => v.IssueDate)
                .Take(100)
                .ToList();

            return View(viewModel);
        }

        // ============================================================
        // 2. البحث واختيار متدرب/محامي (SelectTrainee)
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult SelectTrainee(string searchTerm)
        {
            var pendingIds = db.PaymentVouchers
                .Where(v => (v.Status == "صادر" || v.Status == "بانتظار الدفع") && v.GraduateApplicationId != null)
                .Select(v => v.GraduateApplicationId.Value)
                .ToList();

            // يفضل نقل هذه القائمة لإعدادات النظام مستقبلاً
            var allowedStatuses = new List<string> {
                "مقبول (بانتظار الدفع)",
                "متدرب مقيد",
                "متدرب موقوف",
                "محامي مزاول",
                "محامي غير مزاول",
                "بانتظار تجديد المزاولة"
            };

            var query = db.GraduateApplications.AsNoTracking()
                .Include(a => a.ApplicationStatus)
                .Where(a => allowedStatuses.Contains(a.ApplicationStatus.Name) && !pendingIds.Contains(a.Id));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(g => g.ArabicName.Contains(searchTerm) ||
                                         g.NationalIdNumber.Contains(searchTerm) ||
                                         g.TraineeSerialNo.Contains(searchTerm));
            }

            var result = query.OrderByDescending(g => g.ApplicationStatus.Name == "مقبول (بانتظار الدفع)")
                              .ThenByDescending(g => g.SubmissionDate)
                              .Take(50)
                              .ToList();

            ViewBag.SearchTerm = searchTerm;
            return View(result);
        }

        // ============================================================
        // 3. إصدار قسيمة رسوم امتحان (يدوياً) - محدثة
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateExamVoucher(int? examAppId)
        {
            if (examAppId == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var app = db.ExamApplications.Find(examAppId);
            if (app == null) return HttpNotFound();

            var existingVoucher = db.PaymentVouchers.FirstOrDefault(v => v.CheckNumber == app.FullName && v.Status != "ملغى" && v.VoucherDetails.Any(d => d.Description.Contains("امتحان")));
            if (existingVoucher != null)
            {
                TempData["InfoMessage"] = "يوجد قسيمة صادرة لهذا المتقدم بالفعل.";
                return RedirectToAction("Details", new { id = existingVoucher.Id });
            }

            // ✅ التعديل: الاعتماد على الإعدادات
            int? feeTypeId = GetSettingOrFindByName<FeeType>("Exam_Registration_FeeTypeId", "امتحان القبول");
            var examFeeType = db.FeeTypes.Include(f => f.Currency).FirstOrDefault(f => f.Id == feeTypeId);

            if (examFeeType == null) return Content("خطأ: نوع رسم 'امتحان القبول' غير محدد في إعدادات النظام.");

            var voucher = new PaymentVoucher
            {
                GraduateApplicationId = null,
                IssueDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(14),
                TotalAmount = examFeeType.DefaultAmount,
                Status = "صادر",
                PaymentMethod = "إيداع بنكي",
                IssuedByUserId = GetCurrentUserId(),
                IssuedByUserName = Session["FullName"]?.ToString(),
                CheckNumber = app.FullName,

                VoucherDetails = new List<VoucherDetail>
                {
                    new VoucherDetail
                    {
                        FeeTypeId = examFeeType.Id,
                        Amount = examFeeType.DefaultAmount,
                        BankAccountId = examFeeType.BankAccountId,
                        Description = $"رسوم امتحان القبول - {app.FullName} ({app.NationalIdNumber})"
                    }
                }
            };

            db.PaymentVouchers.Add(voucher);
            app.Status = "بانتظار دفع الرسوم";
            db.SaveChanges();

            AuditService.LogAction("Create Exam Voucher", "PaymentVouchers", $"Created manual voucher for Exam App {app.NationalIdNumber}");

            return RedirectToAction("PrintVoucher", new { id = voucher.Id });
        }

        // ============================================================
        // 4. إنشاء قسيمة متدرب/محامي (Create - Standard)
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var app = db.GraduateApplications.Include(a => a.ApplicationStatus).FirstOrDefault(a => a.Id == id);
            if (app == null) return HttpNotFound();

            List<string> suggestedFees = new List<string>();
            string status = app.ApplicationStatus.Name;

            if (status.Contains("مقبول"))
                suggestedFees.AddRange(new[] { "رسوم تسجيل متدرب جديد", "رسوم بطاقة التدريب (الكارنيه)", "رسوم صندوق تعاون (متدرب)" });
            else if (status.Contains("متدرب"))
                suggestedFees.AddRange(new[] { "رسوم تجديد سنوي للمتدربين", "رسوم صندوق تعاون (متدرب)" });
            else if (status.Contains("محامي") || status.Contains("تجديد"))
                suggestedFees.AddRange(new[] { "تجديد مزاولة (سنوي)", "رسوم صندوق التعاون", "رسوم الزمالة" });

            var fees = db.FeeTypes
                .Include(f => f.Currency)
                .Include(f => f.BankAccount)
                .Where(f => f.IsActive && !f.Name.Contains("تصديق"))
                .ToList();

            var model = new CreateVoucherViewModel
            {
                GraduateApplicationId = app.Id,
                TraineeName = app.ArabicName,
                CurrentTraineeStatus = status,
                ExpiryDate = DateTime.Now.AddDays(7),
                PaymentMethod = "نقدي",
                Fees = fees.Select(f => new FeeSelection
                {
                    FeeTypeId = f.Id,
                    FeeTypeName = f.Name,
                    Amount = f.DefaultAmount,
                    CurrencySymbol = f.Currency.Symbol,
                    IsSelected = suggestedFees.Any(s => f.Name.Contains(s)),
                    BankAccountId = f.BankAccountId,
                    BankName = f.BankAccount?.BankName,
                    BankAccountNumber = f.BankAccount?.AccountNumber,
                    Iban = f.BankAccount?.Iban
                }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(CreateVoucherViewModel model)
        {
            if (ModelState.IsValid)
            {
                var selectedFees = model.Fees.Where(f => f.IsSelected).ToList();
                if (!selectedFees.Any())
                {
                    ModelState.AddModelError("", "يجب اختيار رسم واحد على الأقل.");
                    return View(model);
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var voucher = new PaymentVoucher
                        {
                            GraduateApplicationId = model.GraduateApplicationId,
                            IssueDate = DateTime.Now,
                            ExpiryDate = model.ExpiryDate,
                            Status = "صادر",
                            TotalAmount = selectedFees.Sum(f => f.Amount),
                            IssuedByUserId = GetCurrentUserId(),
                            IssuedByUserName = Session["FullName"]?.ToString() ?? "System",
                            PaymentMethod = model.PaymentMethod,
                            VoucherDetails = new List<VoucherDetail>()
                        };

                        foreach (var fee in selectedFees)
                        {
                            voucher.VoucherDetails.Add(new VoucherDetail
                            {
                                FeeTypeId = fee.FeeTypeId,
                                Amount = fee.Amount,
                                BankAccountId = fee.BankAccountId,
                                Description = fee.FeeTypeName
                            });
                        }

                        db.PaymentVouchers.Add(voucher);

                        var trainee = db.GraduateApplications.Find(model.GraduateApplicationId);
                        var pendingStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "مقبول (بانتظار الدفع)");

                        if (trainee != null && trainee.ApplicationStatusId == pendingStatus?.Id)
                        {
                            var nextStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "بانتظار دفع الرسوم");
                            if (nextStatus != null)
                            {
                                trainee.ApplicationStatusId = nextStatus.Id;
                                db.Entry(trainee).State = EntityState.Modified;
                            }
                        }

                        db.SaveChanges();
                        transaction.Commit();

                        AuditService.LogAction("Create Voucher", "PaymentVouchers", $"Created Voucher #{voucher.Id} for Trainee {model.TraineeName}");
                        TempData["SuccessMessage"] = "تم إصدار قسيمة الدفع بنجاح.";
                        return RedirectToAction("PrintVoucher", new { id = voucher.Id });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.Message);
                    }
                }
            }
            return View(model);
        }

        // ============================================================
        // 5. قسائم المتعهدين - طوابع (محدثة)
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateContractorVoucher()
        {
            var viewModel = new CreateContractorVoucherViewModel
            {
                AvailableBooksList = db.StampBooks.Where(b => b.Status == "في المخزن").OrderBy(b => b.StartSerial).ToList(),
                ContractorsList = new SelectList(db.StampContractors.Where(c => c.IsActive), "Id", "Name")
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateContractorVoucher(CreateContractorVoucherViewModel viewModel)
        {
            if (viewModel.SelectedBookIds == null || !viewModel.SelectedBookIds.Any())
                ModelState.AddModelError("SelectedBookIds", "يجب اختيار دفتر طوابع واحد على الأقل.");

            if (!ModelState.IsValid)
            {
                viewModel.AvailableBooksList = db.StampBooks.Where(b => b.Status == "في المخزن").OrderBy(b => b.StartSerial).ToList();
                viewModel.ContractorsList = new SelectList(db.StampContractors.Where(c => c.IsActive), "Id", "Name", viewModel.SelectedContractorId);
                return View(viewModel);
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var selectedBooks = db.StampBooks.Where(b => viewModel.SelectedBookIds.Contains(b.Id) && b.Status == "في المخزن").ToList();

                    if (selectedBooks.Count != viewModel.SelectedBookIds.Count)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = "خطأ: بعض الدفاتر المختارة لم تعد متاحة.";
                        return RedirectToAction("CreateContractorVoucher");
                    }

                    // ✅ التعديل: الاعتماد على الإعدادات
                    int? feeTypeId = GetSettingOrFindByName<FeeType>("Stamp_Contractor_FeeTypeId", "طوابع");
                    var feeType = db.FeeTypes.FirstOrDefault(f => f.Id == feeTypeId);

                    if (feeType == null)
                    {
                        feeType = db.FeeTypes.FirstOrDefault();
                        if (feeType == null) throw new Exception("لا يوجد أي أنواع رسوم معرفة في النظام.");
                    }

                    var voucher = new PaymentVoucher
                    {
                        GraduateApplicationId = null,
                        PaymentMethod = "بنكي",
                        IssueDate = DateTime.Now,
                        ExpiryDate = DateTime.Now.AddDays(7),
                        Status = "صادر",
                        TotalAmount = selectedBooks.Sum(b => b.Quantity * b.ValuePerStamp),
                        IssuedByUserId = GetCurrentUserId(),
                        IssuedByUserName = Session["FullName"]?.ToString() ?? "System",
                        VoucherDetails = new List<VoucherDetail>()
                    };

                    foreach (var book in selectedBooks)
                    {
                        voucher.VoucherDetails.Add(new VoucherDetail
                        {
                            FeeTypeId = feeType.Id,
                            Amount = book.Quantity * book.ValuePerStamp,
                            BankAccountId = feeType.BankAccountId,
                            Description = $"دفتر طوابع فئة {book.ValuePerStamp} ({book.StartSerial}-{book.EndSerial})"
                        });
                    }

                    db.PaymentVouchers.Add(voucher);
                    db.SaveChanges();

                    foreach (var book in selectedBooks)
                    {
                        db.StampBookIssuances.Add(new StampBookIssuance
                        {
                            ContractorId = viewModel.SelectedContractorId,
                            StampBookId = book.Id,
                            PaymentVoucherId = voucher.Id,
                            IssuanceDate = DateTime.Now
                        });
                        book.Status = "بانتظار الدفع";
                        db.Entry(book).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    AuditService.LogAction("Create Contractor Voucher", "PaymentVouchers", $"Created Voucher #{voucher.Id}.");

                    return RedirectToAction("PrintStampContractorVoucher", new { id = voucher.Id });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "خطأ: " + ex.Message;
                    return RedirectToAction("CreateContractorVoucher");
                }
            }
        }

        // ============================================================
        // 6. القسائم العامة (General Vouchers)
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateGeneralVoucher()
        {
            var fees = db.FeeTypes
                .Include(f => f.Currency)
                .Include(f => f.BankAccount)
                .Where(f => f.IsActive && !f.Name.Contains("تصديق"))
                .ToList();

            var viewModel = new CreateGeneralVoucherViewModel
            {
                ExpiryDate = DateTime.Now.AddDays(7),
                PaymentMethod = "نقدي",
                Fees = fees.Select(f => new FeeSelection
                {
                    FeeTypeId = f.Id,
                    FeeTypeName = f.Name,
                    Amount = f.DefaultAmount,
                    CurrencySymbol = f.Currency.Symbol,
                    IsSelected = false,
                    BankAccountId = f.BankAccountId,
                    BankName = f.BankAccount?.BankName,
                    BankAccountNumber = f.BankAccount?.AccountNumber,
                    Iban = f.BankAccount?.Iban
                }).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateGeneralVoucher(CreateGeneralVoucherViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var selectedFees = viewModel.Fees.Where(f => f.IsSelected).ToList();
                if (!selectedFees.Any())
                {
                    ModelState.AddModelError("", "يجب اختيار رسم واحد على الأقل.");
                    return View(viewModel);
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var voucher = new PaymentVoucher
                        {
                            GraduateApplicationId = null,
                            CheckNumber = viewModel.PayerName,
                            IssueDate = DateTime.Now,
                            ExpiryDate = viewModel.ExpiryDate,
                            Status = "صادر",
                            TotalAmount = selectedFees.Sum(f => f.Amount),
                            IssuedByUserId = GetCurrentUserId(),
                            IssuedByUserName = Session["FullName"]?.ToString() ?? "System",
                            PaymentMethod = viewModel.PaymentMethod,
                            VoucherDetails = new List<VoucherDetail>()
                        };

                        foreach (var fee in selectedFees)
                        {
                            voucher.VoucherDetails.Add(new VoucherDetail
                            {
                                FeeTypeId = fee.FeeTypeId,
                                Amount = fee.Amount,
                                BankAccountId = fee.BankAccountId,
                                Description = string.IsNullOrEmpty(viewModel.Notes) ? fee.FeeTypeName : $"{fee.FeeTypeName} - {viewModel.Notes}"
                            });
                        }

                        db.PaymentVouchers.Add(voucher);
                        db.SaveChanges();
                        transaction.Commit();

                        AuditService.LogAction("Create General Voucher", "PaymentVouchers", $"Created Voucher #{voucher.Id} for {viewModel.PayerName}.");
                        TempData["SuccessMessage"] = "تم إصدار القسيمة العامة بنجاح.";

                        return RedirectToAction("PrintVoucher", new { id = voucher.Id });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "خطأ أثناء الحفظ: " + ex.Message);
                    }
                }
            }
            return View(viewModel);
        }

        // ============================================================
        // 7. تأكيد الدفع النقدي (Confirm Cash Payment) - هام جداً للقيود
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult ConfirmCashPayment(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var voucher = db.PaymentVouchers.AsNoTracking()
                .Include(v => v.GraduateApplication)
                .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency))
                .FirstOrDefault(v => v.Id == id);

            if (voucher == null || voucher.Status != "صادر")
            {
                TempData["ErrorMessage"] = "القسيمة غير موجودة أو تم تسديدها بالفعل.";
                return RedirectToAction("Index");
            }
            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult ConfirmCashPayment(int PaymentVoucherId)
        {
            var employeeId = GetCurrentUserId();
            var employeeName = Session["FullName"] as string;

            if (employeeId == -1) return RedirectToAction("Login", "AdminLogin");

            var paymentVoucher = db.PaymentVouchers
                .Include(v => v.GraduateApplication.ApplicationStatus)
                .Include(v => v.VoucherDetails.Select(d => d.FeeType))
                .Include(v => v.VoucherDetails.Select(d => d.BankAccount)) // نحتاج البنك للقيد
                .FirstOrDefault(v => v.Id == PaymentVoucherId);

            if (paymentVoucher == null || paymentVoucher.Status != "صادر")
            {
                TempData["ErrorMessage"] = "القسيمة غير موجودة أو تم سدادها بالفعل.";
                return RedirectToAction("Index");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // 1. إنشاء الإيصال
                    int currentYear = DateTime.Now.Year;
                    int lastSequenceNumber = db.Receipts.Where(r => r.Year == currentYear).Select(r => (int?)r.SequenceNumber).Max() ?? 0;
                    int newSequenceNumber = lastSequenceNumber + 1;

                    var receipt = new Receipt
                    {
                        Id = paymentVoucher.Id, // One-to-One with Voucher
                        BankPaymentDate = DateTime.Now,
                        BankReceiptNumber = "تحصيل نقدي",
                        CreationDate = DateTime.Now,
                        IssuedByUserId = employeeId,
                        IssuedByUserName = employeeName,
                        Year = currentYear,
                        SequenceNumber = newSequenceNumber
                    };
                    db.Receipts.Add(receipt);

                    // 2. تحديث حالة القسيمة
                    paymentVoucher.Status = "مسدد";
                    db.Entry(paymentVoucher).State = EntityState.Modified;

                    // ====================================================================
                    // 🛑🛑🛑 التعديل الجديد: معالجة صرف الطوابع للمتعهد 🛑🛑🛑
                    // ====================================================================
                    var stampIssuances = db.StampBookIssuances
                        .Where(i => i.PaymentVoucherId == paymentVoucher.Id)
                        .ToList();

                    if (stampIssuances.Any())
                    {
                        foreach (var issue in stampIssuances)
                        {
                            // أ. تحديث حالة الدفتر
                            var book = db.StampBooks.Find(issue.StampBookId);
                            if (book != null)
                            {
                                book.Status = "مع المتعهد"; // تفعيل الدفتر
                                db.Entry(book).State = EntityState.Modified;

                                // ب. تحديث الطوابع الفردية داخل الدفتر ليراها المتعهد في شاشة البيع
                                db.Database.ExecuteSqlCommand(
                                    "UPDATE Stamps SET Status = 'مع المتعهد', ContractorId = {0} WHERE StampBookId = {1}",
                                    issue.ContractorId, book.Id
                                );
                            }
                        }
                    }
                    // ====================================================================

                    // 3. تحديث حالة المعاملة المرتبطة (إن وجدت - للعقود)
                    var contractTransaction = db.ContractTransactions
                        .Include(c => c.ContractType)
                        .FirstOrDefault(c => c.PaymentVoucherId == paymentVoucher.Id);

                    if (contractTransaction != null)
                    {
                        contractTransaction.Status = "بانتظار التصديق";
                        db.Entry(contractTransaction).State = EntityState.Modified;

                        // توزيع الحصص
                        decimal lawyerShare = contractTransaction.FinalFee * contractTransaction.ContractType.LawyerPercentage;
                        decimal barShare = contractTransaction.FinalFee * contractTransaction.ContractType.BarSharePercentage;

                        db.FeeDistributions.Add(new FeeDistribution { ReceiptId = receipt.Id, ContractTransactionId = contractTransaction.Id, LawyerId = contractTransaction.LawyerId, Amount = lawyerShare, ShareType = "حصة محامي", IsSentToBank = false });
                        db.FeeDistributions.Add(new FeeDistribution { ReceiptId = receipt.Id, ContractTransactionId = contractTransaction.Id, LawyerId = null, Amount = barShare, ShareType = "حصة نقابة", IsSentToBank = true });
                    }

                    // 4. تحديث حالة المتدرب (إن وجد)
                    if (paymentVoucher.GraduateApplication != null && paymentVoucher.GraduateApplication.ApplicationStatus.Name == "بانتظار دفع الرسوم")
                    {
                        var activeStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "متدرب مقيد");
                        if (activeStatus != null)
                        {
                            paymentVoucher.GraduateApplication.ApplicationStatusId = activeStatus.Id;
                            db.Entry(paymentVoucher.GraduateApplication).State = EntityState.Modified;
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();

                    // ============================================================
                    // === 💡 التكامل المالي (Accounting Integration) 💡 ===
                    // ============================================================
                    try
                    {
                        using (var accService = new AccountingService())
                        {
                            bool isPosted = accService.GenerateEntryForReceipt(receipt.Id, employeeId);

                            if (isPosted)
                                TempData["SuccessMessage"] = $"تم التحصيل النقدي (إيصال {newSequenceNumber}) وتم ترحيل القيد المحاسبي بنجاح.";
                            else
                                TempData["SuccessMessage"] = $"تم التحصيل (إيصال {newSequenceNumber})، ولكن حدثت مشكلة في القيد الآلي. يرجى مراجعة المحاسب.";
                        }
                    }
                    catch (Exception ex)
                    {
                        TempData["SuccessMessage"] = $"تم التحصيل بنجاح، ولكن فشل القيد الآلي: {ex.Message}";
                    }

                    // تحديد صفحة الطباعة المناسبة
                    if (stampIssuances.Any())
                    {
                        TempData["PrintReceiptUrl"] = Url.Action("PrintStampContractorVoucher", "PaymentVouchers", new { id = receipt.Id });
                    }
                    else if (contractTransaction != null)
                    {
                        TempData["PrintReceiptUrl"] = Url.Action("PrintContractReceipt", "ContractTransactions", new { id = receipt.Id });
                    }
                    else
                    {
                        TempData["PrintReceiptUrl"] = Url.Action("PrintVoucher", "PaymentVouchers", new { id = receipt.Id });
                    }

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "حدث خطأ أثناء الحفظ: " + ex.Message;
                    return RedirectToAction("Index");
                }
            }
        }

        // ============================================================
        // 8. الطباعة (كما هي، لم تتغير)
        // ============================================================
        public ActionResult PrintVoucher(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (db.StampBookIssuances.Any(i => i.PaymentVoucherId == id))
                return RedirectToAction("PrintStampContractorVoucher", new { id });

            var viewModel = PrepareVoucherViewModel(id.Value);
            if (viewModel == null) return HttpNotFound("القسيمة غير موجودة");

            AuditService.LogAction("Print Voucher", "PaymentVouchers", $"Printed Voucher #{id}.");
            return View("PrintVoucher", viewModel);
        }

        public ActionResult PrintStampContractorVoucher(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var viewModel = PrepareVoucherViewModel(id.Value);
            if (viewModel == null) return Content("خطأ: القسيمة غير موجودة.");

            AuditService.LogAction("Print Contractor Voucher", "PaymentVouchers", $"Printed Contractor Voucher #{id}.");
            return View("~/Areas/Admin/Views/PaymentVouchers/PrintStampContractorVoucher.cshtml", viewModel);
        }

        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var voucher = db.PaymentVouchers.AsNoTracking()
                .Include(v => v.GraduateApplication)
                .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency))
                .Include(v => v.VoucherDetails.Select(d => d.BankAccount.Currency))
                .FirstOrDefault(v => v.Id == id);

            if (voucher == null) return HttpNotFound();
            return View(voucher);
        }

        // ============================================================
        // 9. الحذف (Delete) - كما هي
        // ============================================================
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var voucher = db.PaymentVouchers.Include(p => p.GraduateApplication).FirstOrDefault(p => p.Id == id);
            if (voucher == null) return HttpNotFound();

            if (voucher.Status != "صادر" && voucher.Status != "بانتظار الدفع")
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذه القسيمة لأنها مدفوعة أو ملغاة مسبقاً.";
                return RedirectToAction("Index");
            }
            return View(voucher);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteConfirmed(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var voucher = db.PaymentVouchers.Find(id);
                    if (voucher != null)
                    {
                        if (voucher.Status == "مسدد")
                        {
                            TempData["ErrorMessage"] = "عذراً، تم سداد القسيمة للتو ولا يمكن حذفها.";
                            return RedirectToAction("Index");
                        }

                        // إعادة حالة المتدرب
                        if (voucher.GraduateApplicationId.HasValue)
                        {
                            var app = db.GraduateApplications.Find(voucher.GraduateApplicationId);
                            var pendingPay = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "بانتظار دفع الرسوم");
                            var accepted = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "مقبول (بانتظار الدفع)");

                            if (app != null && app.ApplicationStatusId == pendingPay?.Id && accepted != null)
                            {
                                app.ApplicationStatusId = accepted.Id;
                                db.Entry(app).State = EntityState.Modified;
                            }
                        }

                        // إعادة حالة الطوابع
                        var issuances = db.StampBookIssuances.Where(i => i.PaymentVoucherId == id).ToList();
                        foreach (var i in issuances)
                        {
                            var book = db.StampBooks.Find(i.StampBookId);
                            if (book != null)
                            {
                                book.Status = "في المخزن";
                                db.Entry(book).State = EntityState.Modified;
                            }
                            db.StampBookIssuances.Remove(i);
                        }

                        var details = db.VoucherDetails.Where(d => d.PaymentVoucherId == id).ToList();
                        db.VoucherDetails.RemoveRange(details);
                        db.PaymentVouchers.Remove(voucher);

                        db.SaveChanges();
                        transaction.Commit();

                        AuditService.LogAction("Delete Voucher", "PaymentVouchers", $"Deleted Voucher #{id}.");
                        TempData["SuccessMessage"] = "تم حذف القسيمة بنجاح.";
                    }
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "خطأ أثناء الحذف: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }

        // ============================================================
        // Helper Functions (دوال مساعدة)
        // ============================================================
        private int GetCurrentUserId()
        {
            if (Session["UserId"] == null) return -1;
            return (int)Session["UserId"];
        }

        // دالة لجلب المعرف من الإعدادات أو استخدام البحث كخيار بديل
        private int? GetSettingOrFindByName<T>(string settingKey, string nameToFind) where T : class
        {
            var setting = db.SystemSettings.FirstOrDefault(s => s.SettingKey == settingKey);
            if (setting != null && setting.ValueInt.HasValue)
            {
                return setting.ValueInt.Value;
            }

            // Fallback: البحث بالاسم
            if (typeof(T) == typeof(FeeType))
            {
                var item = db.FeeTypes.FirstOrDefault(f => f.Name.Contains(nameToFind));
                return item?.Id;
            }
            if (typeof(T) == typeof(ContractType))
            {
                var item = db.ContractTypes.FirstOrDefault(c => c.Name.Contains(nameToFind));
                return item?.Id;
            }
            return null;
        }

        private PrintVoucherViewModel PrepareVoucherViewModel(int id)
        {
            var voucher = db.PaymentVouchers
                .Include(v => v.GraduateApplication)
                .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency))
                .Include(v => v.VoucherDetails.Select(d => d.BankAccount))
                .FirstOrDefault(v => v.Id == id);

            if (voucher == null) return null;

            string applicantName = voucher.GraduateApplication?.ArabicName;
            if (string.IsNullOrEmpty(applicantName))
            {
                if (!string.IsNullOrEmpty(voucher.CheckNumber))
                {
                    applicantName = voucher.CheckNumber;
                }
                else
                {
                    var issuance = db.StampBookIssuances.Include(i => i.Contractor)
                                         .FirstOrDefault(i => i.PaymentVoucherId == voucher.Id);
                    applicantName = issuance?.Contractor.Name ?? "جهة خارجية / متعهد";
                }
            }

            string currencySymbol = voucher.VoucherDetails.FirstOrDefault()?.FeeType?.Currency?.Symbol ?? "₪";
            string amountInWords = TafqeetHelper.ConvertToArabic(voucher.TotalAmount, currencySymbol);

            ViewBag.AmountInWords = amountInWords;
            ViewBag.CurrencySymbol = currencySymbol;

            return new PrintVoucherViewModel
            {
                VoucherId = voucher.Id,
                TraineeName = applicantName,
                IssueDate = voucher.IssueDate,
                ExpiryDate = voucher.ExpiryDate,
                TotalAmount = voucher.TotalAmount,
                PaymentMethod = voucher.PaymentMethod,
                IssuedByUserName = voucher.IssuedByUserName,
                Details = voucher.VoucherDetails.Select(d => new VoucherPrintDetail
                {
                    FeeTypeName = d.Description ?? d.FeeType?.Name,
                    Amount = d.Amount,
                    CurrencySymbol = d.FeeType?.Currency?.Symbol ?? "",
                    BankName = d.BankAccount?.BankName ?? "",
                    AccountName = d.BankAccount?.AccountName ?? "",
                    AccountNumber = d.BankAccount?.AccountNumber ?? "",
                    Iban = d.BankAccount?.Iban ?? ""
                }).ToList()
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}