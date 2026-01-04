using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using BarManegment.ViewModels;
using Tafqeet;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanView")]
    public class PaymentVouchersController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // ============================================================
        // 1. العرض الرئيسي (Index)
        // ============================================================
        public ActionResult Index()
        {
            // 1. تحديد القسائم المستثناة

            // أ. العقود (عادة تكون اختيارية int? لذا نبقي التحقق)
            var contractVoucherIds = db.ContractTransactions
                .Where(c => c.PaymentVoucherId != null)
                .Select(c => c.PaymentVoucherId.Value)
                .ToList();

            // ب. القروض (اختيارية int?)
            var loanVoucherIds = db.LoanInstallments
                .Where(i => i.PaymentVoucherId != null)
                .Select(i => i.PaymentVoucherId.Value)
                .ToList();

            // ج. المتعهدين (إلزامية int - تمت إزالة التحقق من null لإصلاح التحذير)
            var contractorVoucherIds = db.StampBookIssuances
                .Select(i => i.PaymentVoucherId) // لا حاجة لـ .Value أو (int) لأنها int أصلاً
                .ToList();

            // دمج المعرفات في HashSet للأداء العالي
            var excludedVoucherIds = new HashSet<int>(contractVoucherIds);
            excludedVoucherIds.UnionWith(loanVoucherIds);
            excludedVoucherIds.UnionWith(contractorVoucherIds);

            // الاستعلام الأساسي
            var allVouchersQuery = db.PaymentVouchers.AsNoTracking()
                .Include(v => v.GraduateApplication)
                .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency))
                .AsQueryable();

            var viewModel = new VoucherIndexViewModel();

            // 1. قسائم المتدربين والمحامين (Unpaid)
            // نجلب البيانات للذاكرة أولاً ثم نفلتر باستخدام HashSet
            var pendingTraineeVouchers = allVouchersQuery
                .Where(v => (v.Status == "صادر" || v.Status == "بانتظار الدفع")
                            && v.GraduateApplicationId != null)
                .ToList();

            viewModel.UnpaidTraineeVouchers = pendingTraineeVouchers
                .Where(v => !excludedVoucherIds.Contains(v.Id)) // الفلترة هنا في الذاكرة
                .OrderByDescending(v => v.IssueDate)
                .ToList();

            // 2. قسائم المتعهدين (Unpaid)
            var contractorIssuances = db.StampBookIssuances.AsNoTracking()
                .Include(i => i.Contractor)
                .Include(i => i.PaymentVoucher)
                .Where(i => i.PaymentVoucher != null && (i.PaymentVoucher.Status == "صادر" || i.PaymentVoucher.Status == "بانتظار الدفع"))
                .ToList();

            viewModel.UnpaidContractorVouchers = contractorIssuances
                .GroupBy(i => i.PaymentVoucher)
                .Select(g => new ContractorVoucherDisplay
                {
                    Voucher = g.Key,
                    ContractorName = g.FirstOrDefault()?.Contractor?.Name ?? "متعهد عام"
                })
                .OrderByDescending(x => x.Voucher.IssueDate)
                .ToList();

            // 3. القسائم العامة (Unpaid)
            var pendingGeneralVouchers = allVouchersQuery
                .Where(v => (v.Status == "صادر" || v.Status == "بانتظار الدفع")
                            && v.GraduateApplicationId == null)
                .ToList();

            viewModel.UnpaidGeneralVouchers = pendingGeneralVouchers
                .Where(v => !excludedVoucherIds.Contains(v.Id))
                .OrderByDescending(v => v.IssueDate)
                .ToList();

            // 4. الأرشيف (Paid)
            viewModel.PaidVouchers = allVouchersQuery
                .Where(v => v.Status == "مسدد")
                .OrderByDescending(v => v.IssueDate)
                .Take(100)
                .ToList();

            return View(viewModel);
        }

        // ============================================================
        // 2. قسائم المتدربين (Standard Vouchers)
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult SelectTrainee(string searchTerm)
        {
            // استثناء من لديهم قسائم معلقة
            var traineesWithPendingVouchers = db.PaymentVouchers
                .Where(v => v.Status == "صادر" || v.Status == "بانتظار الدفع")
                .Where(v => v.GraduateApplicationId != null)
                .Select(v => v.GraduateApplicationId.Value)
                .ToList();

            var allowedStatuses = new List<string> {
                "مقبول (بانتظار الدفع)",
                "متدرب مقيد", "متدرب موقوف",
                "محامي مزاول", "محامي غير مزاول",
                "بانتظار تجديد المزاولة"
            };

            var query = db.GraduateApplications.AsNoTracking()
                .Include(a => a.ApplicationStatus)
                .Where(a => allowedStatuses.Contains(a.ApplicationStatus.Name) &&
                            !traineesWithPendingVouchers.Contains(a.Id));

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(g => g.ArabicName.Contains(searchTerm) ||
                                         g.NationalIdNumber.Contains(searchTerm) ||
                                         g.TraineeSerialNo.Contains(searchTerm));
            }

            var trainees = query.OrderByDescending(g => g.ApplicationStatus.Name == "مقبول (بانتظار الدفع)")
                                .ThenByDescending(g => g.SubmissionDate)
                                .Take(50)
                                .ToList();

            ViewBag.SearchTerm = searchTerm;
            return View(trainees);
        }

        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(int? id, int? feeTypeId)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var application = db.GraduateApplications.Include(a => a.ApplicationStatus).FirstOrDefault(a => a.Id == id);
            if (application == null) return HttpNotFound();

            List<string> suggestedFees = new List<string>();
            string status = application.ApplicationStatus.Name;

            if (status.Contains("مقبول"))
            {
                suggestedFees.AddRange(new[] { "رسوم تسجيل متدرب جديد", "رسوم بطاقة التدريب (الكارنيه)", "رسوم صندوق تعاون (متدرب)", "رسوم متعلقات التدريب" });
            }
            else if (status.Contains("متدرب"))
            {
                suggestedFees.AddRange(new[] { "رسوم تجديد سنوي للمتدربين", "رسوم صندوق تعاون (متدرب)" });
            }
            else if (status.Contains("محامي") || status.Contains("تجديد"))
            {
                suggestedFees.AddRange(new[] { "تجديد مزاولة (سنوي)", "رسوم صندوق التعاون", "رسوم الزمالة" });
            }

            var availableFeesQuery = db.FeeTypes.Include(f => f.Currency).Include(f => f.BankAccount)
                .Where(f => f.IsActive && !f.Name.Contains("تصديق"));

            if (feeTypeId.HasValue) availableFeesQuery = availableFeesQuery.Where(f => f.Id == feeTypeId.Value);

            var viewModel = new CreateVoucherViewModel
            {
                GraduateApplicationId = application.Id,
                TraineeName = application.ArabicName,
                ExpiryDate = DateTime.Now.AddDays(7),
                PaymentMethod = "نقدي",
                Fees = availableFeesQuery.Select(f => new FeeSelection
                {
                    FeeTypeId = f.Id,
                    FeeTypeName = f.Name,
                    Amount = f.DefaultAmount,
                    CurrencySymbol = f.Currency.Symbol,
                    IsSelected = (feeTypeId.HasValue && f.Id == feeTypeId.Value) || suggestedFees.Contains(f.Name),
                    BankAccountId = f.BankAccountId,
                    BankName = f.BankAccount.BankName,
                    BankAccountNumber = f.BankAccount.AccountNumber,
                    Iban = f.BankAccount.Iban
                }).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult Create(CreateVoucherViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var selectedFees = viewModel.Fees.Where(f => f.IsSelected).ToList();
                if (!selectedFees.Any())
                {
                    ModelState.AddModelError("", "يجب اختيار رسم واحد على الأقل.");
                    return View(viewModel);
                }

                var voucher = new PaymentVoucher
                {
                    GraduateApplicationId = viewModel.GraduateApplicationId,
                    IssueDate = DateTime.Now,
                    ExpiryDate = viewModel.ExpiryDate,
                    Status = "صادر",
                    TotalAmount = selectedFees.Sum(f => f.Amount),
                    IssuedByUserId = (int)Session["UserId"],
                    IssuedByUserName = Session["FullName"] as string,
                    PaymentMethod = viewModel.PaymentMethod,
                    VoucherDetails = selectedFees.Select(f => new VoucherDetail
                    {
                        FeeTypeId = f.FeeTypeId,
                        Amount = f.Amount,
                        BankAccountId = f.BankAccountId,
                        Description = f.FeeTypeName
                    }).ToList()
                };

                db.PaymentVouchers.Add(voucher);

                var trainee = db.GraduateApplications.Find(viewModel.GraduateApplicationId);
                if (trainee != null && trainee.ApplicationStatusId == db.ApplicationStatuses.FirstOrDefault(s => s.Name == "مقبول (بانتظار الدفع)")?.Id)
                {
                    var pendingPayStatus = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "بانتظار دفع الرسوم");
                    if (pendingPayStatus != null) trainee.ApplicationStatusId = pendingPayStatus.Id;
                }

                try
                {
                    db.SaveChanges();
                    AuditService.LogAction("Create Voucher", "PaymentVouchers", $"Voucher #{voucher.Id} created for Trainee ID {viewModel.GraduateApplicationId}.");
                    TempData["SuccessMessage"] = "تم إصدار قسيمة الدفع بنجاح.";
                    return RedirectToAction("PrintVoucher", new { id = voucher.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.Message);
                }
            }
            return View(viewModel);
        }

        // ============================================================
        // 3. قسائم المتعهدين (Contractors)
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
                ModelState.AddModelError("SelectedBookIds", "يجب اختيار دفتر واحد على الأقل.");

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
                        TempData["ErrorMessage"] = "خطأ: بعض الدفاتر المختارة لم تعد متاحة.";
                        return RedirectToAction("CreateContractorVoucher");
                    }

                    var feeType = db.FeeTypes.FirstOrDefault(f => f.Name == "رسوم طوابع");
                    if (feeType == null) throw new Exception("لم يتم تعريف رسوم طوابع في النظام.");

                    var voucher = new PaymentVoucher
                    {
                        GraduateApplicationId = null,
                        PaymentMethod = "بنكي",
                        IssueDate = DateTime.Now,
                        ExpiryDate = DateTime.Now.AddDays(7),
                        Status = "صادر",
                        TotalAmount = selectedBooks.Sum(b => b.Quantity * b.ValuePerStamp),
                        IssuedByUserId = (int)Session["UserId"],
                        IssuedByUserName = Session["FullName"] as string,
                        VoucherDetails = new List<VoucherDetail>()
                    };

                    foreach (var book in selectedBooks)
                    {
                        voucher.VoucherDetails.Add(new VoucherDetail
                        {
                            FeeTypeId = feeType.Id,
                            Amount = book.Quantity * book.ValuePerStamp,
                            BankAccountId = feeType.BankAccountId,
                            Description = $"دفتر طوابع ({book.StartSerial}-{book.EndSerial})"
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

                    AuditService.LogAction("Create Contractor Voucher", "PaymentVouchers", $"Voucher #{voucher.Id} created for Contractor ID {viewModel.SelectedContractorId}.");

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
        // 6. صفحة القسائم العامة المنفصلة
        // ============================================================
        [CustomAuthorize(Permission = "CanView")]
        public ActionResult GeneralVouchers()
        {
            var contractVoucherIds = db.ContractTransactions
                .Where(c => c.PaymentVoucherId != null)
                .Select(c => c.PaymentVoucherId.Value)
                .ToList();

            var loanVoucherIds = db.LoanInstallments
                .Where(i => i.PaymentVoucherId != null)
                .Select(i => i.PaymentVoucherId.Value)
                .ToList();

            var contractorVoucherIds = db.StampBookIssuances
                .Select(i => i.PaymentVoucherId)
                .ToList();

            var excludedIds = new HashSet<int>(contractVoucherIds);
            excludedIds.UnionWith(loanVoucherIds);
            excludedIds.UnionWith(contractorVoucherIds);

            var generalVouchersQuery = db.PaymentVouchers.AsNoTracking()
                .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency))
                .Where(v => (v.Status == "صادر" || v.Status == "بانتظار الدفع")
                            && v.GraduateApplicationId == null)
                .ToList();

            var generalVouchers = generalVouchersQuery
                .Where(v => !excludedIds.Contains(v.Id))
                .OrderByDescending(v => v.IssueDate)
                .ToList();

            return View(generalVouchers);
        }

        // ============================================================
        // 4. القسائم العامة (General Vouchers - Create)
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

                var voucher = new PaymentVoucher
                {
                    GraduateApplicationId = null,
                    CheckNumber = viewModel.PayerName,
                    IssueDate = DateTime.Now,
                    ExpiryDate = viewModel.ExpiryDate,
                    Status = "صادر",
                    TotalAmount = selectedFees.Sum(f => f.Amount),
                    IssuedByUserId = (int)Session["UserId"],
                    IssuedByUserName = Session["FullName"] as string,
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

                try
                {
                    db.SaveChanges();
                    AuditService.LogAction("Create General Voucher", "PaymentVouchers", $"General Voucher #{voucher.Id} created for {viewModel.PayerName}.");
                    TempData["SuccessMessage"] = "تم إصدار القسيمة العامة بنجاح.";
                    return RedirectToAction("PrintVoucher", new { id = voucher.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "حدث خطأ أثناء الحفظ: " + ex.Message);
                }
            }
            return View(viewModel);
        }

        // ============================================================
        // 5. الطباعة (Printing)
        // ============================================================
        private PrintVoucherViewModel PrepareVoucherViewModel(int id)
        {
            var voucher = db.PaymentVouchers.Find(id);
            if (voucher == null) return null;

            db.Entry(voucher).Collection(v => v.VoucherDetails).Load();
            if (voucher.GraduateApplicationId.HasValue)
            {
                db.Entry(voucher).Reference(v => v.GraduateApplication).Load();
            }

            foreach (var detail in voucher.VoucherDetails)
            {
                db.Entry(detail).Reference(d => d.FeeType).Load();
                if (detail.FeeType != null)
                {
                    db.Entry(detail.FeeType).Reference(f => f.Currency).Load();
                }
                db.Entry(detail).Reference(d => d.BankAccount).Load();
            }

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

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintVoucher(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var viewModel = PrepareVoucherViewModel(id.Value);
            if (viewModel == null) return HttpNotFound("القسيمة غير موجودة");

            AuditService.LogAction("Print Voucher", "PaymentVouchers", $"User printed Voucher #{id}.");
            return View("PrintVoucher", viewModel);
        }

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult PrintStampContractorVoucher(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var viewModel = PrepareVoucherViewModel(id.Value);
            if (viewModel == null) return Content("خطأ: القسيمة غير موجودة.");

            AuditService.LogAction("Print Contractor Voucher", "PaymentVouchers", $"User printed Contractor Voucher #{id}.");
            return View("~/Areas/Admin/Views/PaymentVouchers/PrintStampContractorVoucher.cshtml", viewModel);
        }

        // ============================================================
        // 6. الحذف والتفاصيل
        // ============================================================
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var voucher = db.PaymentVouchers.Include(p => p.GraduateApplication).FirstOrDefault(p => p.Id == id);
            if (voucher == null) return HttpNotFound();

            if (voucher.Status != "صادر" && voucher.Status != "بانتظار الدفع")
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذه القسيمة لأنها مدفوعة أو ملغاة.";
                return RedirectToAction("Index");
            }
            return View(voucher);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var voucher = db.PaymentVouchers.Find(id);
            if (voucher != null)
            {
                if (voucher.Status == "مسدد")
                {
                    TempData["ErrorMessage"] = "عذراً، تم سداد القسيمة للتو ولا يمكن حذفها.";
                    return RedirectToAction("Index");
                }

                if (voucher.GraduateApplicationId.HasValue)
                {
                    var app = db.GraduateApplications.Find(voucher.GraduateApplicationId);
                    var pendingPay = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "بانتظار دفع الرسوم");
                    var accepted = db.ApplicationStatuses.FirstOrDefault(s => s.Name == "مقبول (بانتظار الدفع)");

                    if (app != null && app.ApplicationStatusId == pendingPay?.Id && accepted != null)
                    {
                        app.ApplicationStatusId = accepted.Id;
                    }
                }

                var issuances = db.StampBookIssuances.Where(i => i.PaymentVoucherId == id).ToList();
                foreach (var i in issuances)
                {
                    var book = db.StampBooks.Find(i.StampBookId);
                    if (book != null) book.Status = "في المخزن";
                    db.StampBookIssuances.Remove(i);
                }

                var details = db.VoucherDetails.Where(d => d.PaymentVoucherId == id).ToList();
                db.VoucherDetails.RemoveRange(details);
                db.PaymentVouchers.Remove(voucher);

                db.SaveChanges();
                AuditService.LogAction("Delete Voucher", "PaymentVouchers", $"Deleted Voucher #{id}.");
                TempData["SuccessMessage"] = "تم حذف القسيمة بنجاح.";
            }
            return RedirectToAction("Index");
        }

        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var voucher = db.PaymentVouchers.AsNoTracking()
                .Include(v => v.GraduateApplication)
                .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency)) // تضمين العملة
                .Include(v => v.VoucherDetails.Select(d => d.BankAccount.Currency)) // 💡 تضمين الحساب البنكي وعملته (الحل)
                .FirstOrDefault(v => v.Id == id);

            if (voucher == null) return HttpNotFound();
            return View(voucher);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}