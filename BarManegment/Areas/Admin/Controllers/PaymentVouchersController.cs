using BarManegment.Areas.Admin.ViewModels; // ✅ استخدام الـ ViewModels الموحدة
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
            // 1. تحديد القسائم المستثناة (المرتبطة بأنظمة فرعية أخرى) لتجنب التكرار
            var excludedVoucherIds = new HashSet<int>(
                db.ContractTransactions.Where(c => c.PaymentVoucherId != null).Select(c => c.PaymentVoucherId.Value)
            );

            // إضافة قسائم القروض
            var loanIds = db.LoanInstallments.Where(i => i.PaymentVoucherId != null).Select(i => i.PaymentVoucherId.Value).ToList();
            foreach (var id in loanIds) excludedVoucherIds.Add(id);

            // 2. الاستعلام الأساسي
            var allVouchersQuery = db.PaymentVouchers.AsNoTracking()
                .Include(v => v.GraduateApplication)
                .Include(v => v.VoucherDetails.Select(d => d.FeeType.Currency))
                .AsQueryable();

            var viewModel = new VoucherIndexViewModel();

            // أ. قسائم المحامين والمتدربين (غير المدفوعة)
            var pendingTrainee = allVouchersQuery
                .Where(v => (v.Status == "صادر" || v.Status == "بانتظار الدفع")
                            && v.GraduateApplicationId != null)
                .ToList();

            viewModel.UnpaidTraineeVouchers = pendingTrainee
                .Where(v => !excludedVoucherIds.Contains(v.Id))
                .OrderByDescending(v => v.IssueDate)
                .ToList();

            // ب. قسائم المتعهدين (طوابع)
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

            // ج. القسائم العامة (جهات خارجية)
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

            // د. الأرشيف (أحدث 100 قسيمة مدفوعة)
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
        // 3. إنشاء قسيمة متدرب/محامي (Create - Standard)
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
            {
                suggestedFees.AddRange(new[] { "رسوم تسجيل متدرب جديد", "رسوم بطاقة التدريب (الكارنيه)", "رسوم صندوق تعاون (متدرب)" });
            }
            else if (status.Contains("متدرب"))
            {
                suggestedFees.AddRange(new[] { "رسوم تجديد سنوي للمتدربين", "رسوم صندوق تعاون (متدرب)" });
            }
            else if (status.Contains("محامي") || status.Contains("تجديد"))
            {
                suggestedFees.AddRange(new[] { "تجديد مزاولة (سنوي)", "رسوم صندوق التعاون", "رسوم الزمالة" });
            }

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
                            IssuedByUserId = (int?)Session["UserId"] ?? 1,
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
                        if (trainee != null && trainee.ApplicationStatusId == db.ApplicationStatuses.FirstOrDefault(s => s.Name == "مقبول (بانتظار الدفع)")?.Id)
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
        // 4. قسائم المتعهدين - طوابع (Contractor Vouchers)
        // ============================================================
        [CustomAuthorize(Permission = "CanAdd")]
        public ActionResult CreateContractorVoucher()
        {
            var viewModel = new CreateContractorVoucherViewModel
            {
                AvailableBooksList = db.StampBooks
                    .Where(b => b.Status == "في المخزن")
                    .OrderBy(b => b.StartSerial)
                    .ToList(),

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
                    var selectedBooks = db.StampBooks
                        .Where(b => viewModel.SelectedBookIds.Contains(b.Id) && b.Status == "في المخزن")
                        .ToList();

                    if (selectedBooks.Count != viewModel.SelectedBookIds.Count)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = "خطأ: بعض الدفاتر المختارة لم تعد متاحة، يرجى المحاولة مرة أخرى.";
                        return RedirectToAction("CreateContractorVoucher");
                    }

                    var feeType = db.FeeTypes.FirstOrDefault(f => f.Name.Contains("طوابع"));
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
                        IssuedByUserId = (int?)Session["UserId"] ?? 1,
                        IssuedByUserName = Session["FullName"]?.ToString() ?? "System",
                        VoucherDetails = new List<VoucherDetail>()
                    };

                    foreach (var book in selectedBooks)
                    {
                        // ✅ تم تصحيح الخطأ هنا: استخدام ValuePerStamp
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

                    AuditService.LogAction("Create Contractor Voucher", "PaymentVouchers", $"Created Voucher #{voucher.Id} for Contractor ID {viewModel.SelectedContractorId}.");

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
        // 5. القسائم العامة (General Vouchers)
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
                            IssuedByUserId = (int?)Session["UserId"] ?? 1,
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
        // 6. الطباعة والتفاصيل (Shared)
        // ============================================================

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

        [CustomAuthorize(Permission = "CanView")]
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

        [CustomAuthorize(Permission = "CanView")]
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
        // 7. الحذف (Delete)
        // ============================================================
        [CustomAuthorize(Permission = "CanDelete")]
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var voucher = db.PaymentVouchers
                .Include(p => p.GraduateApplication)
                .FirstOrDefault(p => p.Id == id);

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

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}