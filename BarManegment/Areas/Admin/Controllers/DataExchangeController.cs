using BarManegment.Areas.Admin.ViewModels;
using BarManegment.Helpers;
using BarManegment.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.Controllers
{
    [CustomAuthorize(Permission = "CanEdit")]
    public class DataExchangeController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        public DataExchangeController()
        {
          //  ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        // GET: Admin/DataExchange
        public ActionResult Index()
        {
            return View();
        }

        #region 1. تحميل القوالب (Templates)
        public ActionResult DownloadTemplate(string type)
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(type);
                var headers = GetHeaders(type);

                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cells[1, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Template_{type}.xlsx");
            }
        }
        #endregion

        #region 2. تصدير البيانات (Export)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExportData(string type)
        {
            try
            {
                using (var package = new ExcelPackage())
                {
                    var headers = GetHeaders(type);

                    switch (type)
                    {
                        case "UserTypes":
                        case "Currencies":
                        case "QualificationTypes":
                        case "ApplicationStatuses":
                        case "Genders":
                        case "NationalIdTypes":
                        case "ExamTypes":
                        case "AttachmentTypes":
                        case "Provinces":
                        case "PartyRoles":
                        case "ContractExemptionReasons":
                        case "ContractTypes":
                            ExportLookupSheet(package, type, headers);
                            break;

                        case "BankAccounts": ExportBankAccounts(package, headers); break;
                        case "FeeTypes": ExportFeeTypes(package, headers); break;
                        case "Graduates": ExportGraduates(package, headers); break;
                        case "Exams": ExportExams(package, headers); break;
                        case "ExamResults": ExportExamResults(package, headers); break;
                        case "Payments": ExportPayments(package, headers); break;
                        case "Contracts": ExportContracts(package, headers); break;
                        case "StampContractors": ExportStampContractors(package, headers); break;
                        case "StampBooks": ExportStampBooks(package, headers); break;
                        case "StampSales": ExportStampSales(package, headers); break;
                        case "OralExamResults": ExportOralResults(package, headers); break;
                        case "LegalResearch": ExportResearch(package, headers); break;
                        case "Loans": ExportLoans(package, headers); break;
                        case "OpeningJournal":
                            // نقوم فقط بإنشاء الورقة والعناوين (قالب فارغ)
                            PrepareSheet(package, "OpeningJournal", headers, out var wsOp);
                            wsOp.Cells.AutoFitColumns();
                            break;

                        default: return new HttpStatusCodeResult(400, "Invalid Type");
                    }

                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;
                    return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{type}_Export_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء التصدير: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
        #endregion

        #region 3. استيراد البيانات (Import)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ImportData(ImportViewModel model)
        {
            if (!ModelState.IsValid || model.File == null || model.File.ContentLength == 0)
            {
                TempData["ErrorMessage"] = "يرجى اختيار ملف صالح.";
                return RedirectToAction("Index");
            }

            db.Configuration.AutoDetectChangesEnabled = false;
            db.Configuration.ValidateOnSaveEnabled = false;

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    using (var package = new ExcelPackage(model.File.InputStream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null || worksheet.Dimension == null) throw new Exception("الملف فارغ.");

                        int rowCount = worksheet.Dimension.Rows;
                        if (rowCount < 2) throw new Exception("لا توجد بيانات في الملف.");

                        switch (model.EntityType)
                        {
                            case "UserTypes":
                            case "ApplicationStatuses":
                            case "Genders":
                            case "NationalIdTypes":
                            case "ExamTypes":
                            case "Currencies":
                            case "AttachmentTypes":
                            case "QualificationTypes":
                            case "Provinces":
                            case "PartyRoles":
                            case "ContractExemptionReasons":
                            case "ContractTypes":
                            case "BankAccounts":
                            case "FeeTypes":
                                ImportLookupsLogic(worksheet, model.EntityType, rowCount);
                                break;

                            case "Graduates": ImportGraduatesLogic(worksheet, rowCount); break;
                            case "Exams": ImportExamsLogic(worksheet, rowCount); break;
                            case "ExamResults": ImportExamResultsLogic(worksheet, rowCount); break;
                            case "Payments": ImportPaymentsLogic(worksheet, rowCount); break;
                            case "Contracts": ImportContractsLogic(worksheet, rowCount); break;
                            case "StampContractors": ImportContractorsLogic(worksheet, rowCount); break;
                            case "StampBooks": ImportStampBooksLogic(worksheet, rowCount); break;
                            case "StampSales": ImportStampSalesLogic(worksheet, rowCount); break;
                            case "OralExamResults": ImportOralExamResultsLogic(worksheet, rowCount); break;
                            case "LegalResearch": ImportLegalResearchLogic(worksheet, rowCount); break;
                            case "Loans": ImportLoansLogic(worksheet, rowCount); break;
                            case "OpeningJournal": ImportOpeningJournalLogic(worksheet, rowCount); break;

                            default:
                                throw new Exception("نوع البيانات غير معروف.");
                        }
                    }

                    transaction.Commit();
                    TempData["SuccessMessage"] = "تم استيراد البيانات بنجاح.";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["ErrorMessage"] = "فشل الاستيراد: " + ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "");
                }
                finally
                {
                    db.Configuration.AutoDetectChangesEnabled = true;
                    db.Configuration.ValidateOnSaveEnabled = true;
                }
            }

            return RedirectToAction("Index");
        }
        #endregion

        #region 4. تعريف العناوين
        private string[] GetHeaders(string type)
        {
            switch (type)
            {
                case "UserTypes": return new[] { "الاسم العربي", "الاسم الإنجليزي" };
                case "Currencies": return new[] { "اسم العملة", "الرمز" };
                case "QualificationTypes": return new[] { "نوع المؤهل", "نسبة القبول" };

                case "ApplicationStatuses":
                case "Genders":
                case "NationalIdTypes":
                case "ExamTypes":
                case "AttachmentTypes":
                case "Provinces":
                case "PartyRoles":
                case "ContractExemptionReasons":
                case "ContractTypes":
                    return new[] { "الاسم" };

                case "BankAccounts": return new[] { "اسم البنك", "اسم الحساب", "رقم الحساب", "الآيبان", "العملة" };
                case "FeeTypes": return new[] { "اسم الرسم", "القيمة", "العملة", "رقم حساب البنك", "نسبة المحامي", "نسبة النقابة" };

                case "Graduates":
                    return new[] {
                    "الاسم العربي", "الاسم الإنجليزي", "الرقم الوطني", "نوع الهوية", "الجنس", "الجنسية",
                    "تاريخ الميلاد", "مكان الميلاد", "حالة الطلب", "الرقم المتسلسل", "رقم العضوية",
                    "تاريخ بدء التدريب", "تاريخ بدء المزاولة", "ملاحظات", "رقم الجوال", "البريد الإلكتروني",
                    "المحافظة", "المدينة", "الشارع", "البناية", "رقم الوطنية", "الهاتف الأرضي",
                    "واتساب", "شخص الطوارئ", "رقم الطوارئ", "اسم البنك", "الفرع", "رقم الحساب",
                    "الآيبان", "معرف تليجرام", "الرقم الوطني للمشرف"
                };

                case "Exams": return new[] { "عنوان الامتحان", "نوع الامتحان", "وقت البدء", "وقت الانتهاء", "المدة (دقائق)", "نسبة النجاح", "الحالة المطلوبة" };
                case "ExamResults": return new[] { "عنوان الامتحان", "الرقم الوطني للمتقدم", "العلامة", "النتيجة (ناجح/راسب)" };

                case "Payments": return new[] { "الرقم الوطني", "نوع الرسم", "المبلغ", "تاريخ الدفع", "رقم وصل البنك", "ملاحظات" };

                case "Contracts":
                    return new[] {
                    "رقم هوية المحامي", "نوع العقد", "تاريخ المعاملة", "قيمة الرسوم", "الحالة", "ملاحظات",
                    "هل معفى؟", "سبب الإعفاء",
                    "اسم الطرف الأول", "هوية الطرف الأول", "صفة الطرف الأول", "محافظة الطرف الأول",
                    "اسم الطرف الثاني", "هوية الطرف الثاني", "صفة الطرف الثاني", "محافظة الطرف الثاني"
                };

                case "StampContractors": return new[] { "اسم المتعهد", "رقم الجوال", "رقم الهوية", "المحافظة", "الموقع" };
                case "StampBooks": return new[] { "الرقم التسلسلي البداية", "الرقم التسلسلي النهاية", "القيمة للطابع", "الحالة (متاح/مصروف)", "اسم المتعهد (إن وجد)" };
                case "StampSales": return new[] { "رقم الطابع", "اسم المتعهد", "رقم هوية المحامي المشتري", "تاريخ البيع", "هل تم الصرف؟ (نعم/لا)" };

                case "OralExamResults": return new[] { "الرقم الوطني", "تاريخ الامتحان", "النتيجة (ناجح/راسب)", "الدرجة", "ملاحظات" };
                case "LegalResearch": return new[] { "الرقم الوطني", "عنوان البحث", "تاريخ التقديم", "حالة البحث (مقبول/مرفوض)" };
                case "Loans": return new[] { "الرقم الوطني", "مبلغ القرض", "عدد الأقساط", "تاريخ البدء", "الحالة (مسدد/قائم)", "ملاحظات" };
                case "OpeningJournal": return new[] { "رقم الحساب", "اسم الحساب", "مدين", "دائن", "مركز التكلفة", "ملاحظات" };

                default: return new[] { "Unknown" };
            }
        }
        #endregion

        #region 5. دوال التصدير (Export Helper Methods)
        private void PrepareSheet(ExcelPackage p, string sheetName, string[] headers, out ExcelWorksheet ws)
        {
            ws = p.Workbook.Worksheets.Add(sheetName);
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Cells[1, i + 1].Style.Font.Bold = true;
                ws.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightSkyBlue);
            }
        }

        private void ExportLookupSheet(ExcelPackage p, string type, string[] headers)
        {
            PrepareSheet(p, type, headers, out var ws);
            int rowIndex = 2; // ✅ تم تغيير r إلى rowIndex

            if (type == "UserTypes") foreach (var x in db.UserTypes) { ws.Cells[rowIndex, 1].Value = x.NameArabic; ws.Cells[rowIndex, 2].Value = x.NameEnglish; rowIndex++; }
            else if (type == "Currencies") foreach (var x in db.Currencies) { ws.Cells[rowIndex, 1].Value = x.Name; ws.Cells[rowIndex, 2].Value = x.Symbol; rowIndex++; }
            else if (type == "QualificationTypes") foreach (var x in db.QualificationTypes) { ws.Cells[rowIndex, 1].Value = x.Name; ws.Cells[rowIndex, 2].Value = x.MinimumAcceptancePercentage; rowIndex++; }
            else if (type == "ApplicationStatuses") foreach (var x in db.ApplicationStatuses) { ws.Cells[rowIndex, 1].Value = x.Name; rowIndex++; }
            else if (type == "Genders") foreach (var x in db.Genders) { ws.Cells[rowIndex, 1].Value = x.Name; rowIndex++; }
            else if (type == "NationalIdTypes") foreach (var x in db.NationalIdTypes) { ws.Cells[rowIndex, 1].Value = x.Name; rowIndex++; }
            else if (type == "ExamTypes") foreach (var x in db.ExamTypes) { ws.Cells[rowIndex, 1].Value = x.Name; rowIndex++; }
            else if (type == "AttachmentTypes") foreach (var x in db.AttachmentTypes) { ws.Cells[rowIndex, 1].Value = x.Name; rowIndex++; }
            else if (type == "Provinces") foreach (var x in db.Provinces) { ws.Cells[rowIndex, 1].Value = x.Name; rowIndex++; }
            else if (type == "PartyRoles") foreach (var x in db.PartyRoles) { ws.Cells[rowIndex, 1].Value = x.Name; rowIndex++; }
            else if (type == "ContractExemptionReasons") foreach (var x in db.ContractExemptionReasons) { ws.Cells[rowIndex, 1].Value = x.Reason; rowIndex++; }
            else if (type == "ContractTypes") foreach (var x in db.ContractTypes) { ws.Cells[rowIndex, 1].Value = x.Name; rowIndex++; }

            ws.Cells.AutoFitColumns();
        }

        private void ExportBankAccounts(ExcelPackage p, string[] headers)
        {
            PrepareSheet(p, "BankAccounts", headers, out var ws);
            int rowIndex = 2; // ✅
            foreach (var b in db.BankAccounts.Include(b => b.Currency))
            {
                ws.Cells[rowIndex, 1].Value = b.BankName;
                ws.Cells[rowIndex, 2].Value = b.AccountName;
                ws.Cells[rowIndex, 3].Value = b.AccountNumber;
                ws.Cells[rowIndex, 4].Value = b.Iban;
                ws.Cells[rowIndex, 5].Value = b.Currency?.Name;
                rowIndex++;
            }
            ws.Cells.AutoFitColumns();
        }

        private void ExportFeeTypes(ExcelPackage p, string[] headers)
        {
            PrepareSheet(p, "FeeTypes", headers, out var ws);
            int rowIndex = 2; // ✅
            foreach (var f in db.FeeTypes.Include(f => f.Currency).Include(f => f.BankAccount))
            {
                ws.Cells[rowIndex, 1].Value = f.Name;
                ws.Cells[rowIndex, 2].Value = f.DefaultAmount;
                ws.Cells[rowIndex, 3].Value = f.Currency?.Name;
                ws.Cells[rowIndex, 4].Value = f.BankAccount?.AccountNumber;
                ws.Cells[rowIndex, 5].Value = f.LawyerPercentage;
                ws.Cells[rowIndex, 6].Value = f.BarSharePercentage;
                rowIndex++;
            }
            ws.Cells.AutoFitColumns();
        }

        private void ExportGraduates(ExcelPackage p, string[] headers)
        {
            PrepareSheet(p, "Graduates", headers, out var ws);
            var data = db.GraduateApplications.AsNoTracking()
                .Include(g => g.Gender).Include(g => g.ApplicationStatus).Include(g => g.NationalIdType)
                .Include(g => g.ContactInfo).Include(g => g.Supervisor).ToList();

            int rowIndex = 2; // ✅
            foreach (var item in data)
            {
                ws.Cells[rowIndex, 1].Value = item.ArabicName;
                ws.Cells[rowIndex, 2].Value = item.EnglishName;
                ws.Cells[rowIndex, 3].Value = item.NationalIdNumber;
                ws.Cells[rowIndex, 4].Value = item.NationalIdType?.Name;
                ws.Cells[rowIndex, 5].Value = item.Gender?.Name;
                ws.Cells[rowIndex, 6].Value = item.Nationality;
                ws.Cells[rowIndex, 7].Value = item.BirthDate.ToString("yyyy-MM-dd");
                ws.Cells[rowIndex, 8].Value = item.BirthPlace;
                ws.Cells[rowIndex, 9].Value = item.ApplicationStatus?.Name;
                ws.Cells[rowIndex, 10].Value = item.TraineeSerialNo;
                ws.Cells[rowIndex, 11].Value = item.MembershipId;
                ws.Cells[rowIndex, 12].Value = item.TrainingStartDate?.ToString("yyyy-MM-dd");
                ws.Cells[rowIndex, 13].Value = item.PracticeStartDate?.ToString("yyyy-MM-dd");
                ws.Cells[rowIndex, 14].Value = item.Notes;
                ws.Cells[rowIndex, 15].Value = item.ContactInfo?.MobileNumber;
                ws.Cells[rowIndex, 16].Value = item.ContactInfo?.Email;
                ws.Cells[rowIndex, 17].Value = item.ContactInfo?.Governorate;
                ws.Cells[rowIndex, 18].Value = item.ContactInfo?.City;
                ws.Cells[rowIndex, 19].Value = item.ContactInfo?.Street;
                ws.Cells[rowIndex, 20].Value = item.ContactInfo?.BuildingNumber;
                ws.Cells[rowIndex, 21].Value = item.ContactInfo?.NationalMobileNumber;
                ws.Cells[rowIndex, 22].Value = item.ContactInfo?.HomePhoneNumber;
                ws.Cells[rowIndex, 23].Value = item.ContactInfo?.WhatsAppNumber;
                ws.Cells[rowIndex, 24].Value = item.ContactInfo?.EmergencyContactPerson;
                ws.Cells[rowIndex, 25].Value = item.ContactInfo?.EmergencyContactNumber;
                ws.Cells[rowIndex, 26].Value = item.BankName;
                ws.Cells[rowIndex, 27].Value = item.BankBranch;
                ws.Cells[rowIndex, 28].Value = item.AccountNumber;
                ws.Cells[rowIndex, 29].Value = item.Iban;
                ws.Cells[rowIndex, 30].Value = item.TelegramChatId;
                ws.Cells[rowIndex, 31].Value = item.Supervisor?.NationalIdNumber;
                rowIndex++;
            }
            ws.Cells.AutoFitColumns();
        }

        private void ExportExams(ExcelPackage p, string[] headers)
        {
            PrepareSheet(p, "Exams", headers, out var ws);
            int rowIndex = 2; // ✅
            foreach (var e in db.Exams.Include(x => x.ExamType).Include(x => x.RequiredApplicationStatus))
            {
                ws.Cells[rowIndex, 1].Value = e.Title;
                ws.Cells[rowIndex, 2].Value = e.ExamType?.Name;
                ws.Cells[rowIndex, 3].Value = e.StartTime.ToString("yyyy-MM-dd HH:mm");
                ws.Cells[rowIndex, 4].Value = e.EndTime.ToString("yyyy-MM-dd HH:mm");
                ws.Cells[rowIndex, 5].Value = e.DurationInMinutes;
                ws.Cells[rowIndex, 6].Value = e.PassingPercentage;
                ws.Cells[rowIndex, 7].Value = e.RequiredApplicationStatus?.Name;
                rowIndex++;
            }
            ws.Cells.AutoFitColumns();
        }

        private void ExportExamResults(ExcelPackage p, string[] headers)
        {
            PrepareSheet(p, "ExamResults", headers, out var ws);
            int rowIndex = 2; // ✅
            foreach (var en in db.ExamEnrollments.Include(x => x.Exam).Include(x => x.GraduateApplication))
            {
                ws.Cells[rowIndex, 1].Value = en.Exam?.Title;
                ws.Cells[rowIndex, 2].Value = en.GraduateApplication?.NationalIdNumber;
                ws.Cells[rowIndex, 3].Value = en.Score;
                ws.Cells[rowIndex, 4].Value = en.Result;
                rowIndex++;
            }
            ws.Cells.AutoFitColumns();
        }

        private void ExportPayments(ExcelPackage p, string[] headers)
        {
            PrepareSheet(p, "Payments", headers, out var ws);
            // ✅ تم تغيير المتغير داخل Include من r إلى x لمنع التداخل
            var receipts = db.Receipts
                .Include(x => x.PaymentVoucher.GraduateApplication)
                .Include(x => x.PaymentVoucher.VoucherDetails.Select(d => d.FeeType))
                .ToList();

            int rowIndex = 2; // ✅
            foreach (var rec in receipts)
            {
                var v = rec.PaymentVoucher;
                if (v == null) continue;
                var det = v.VoucherDetails.FirstOrDefault();
                ws.Cells[rowIndex, 1].Value = v.GraduateApplication?.NationalIdNumber;
                ws.Cells[rowIndex, 2].Value = det?.FeeType?.Name ?? det?.Description;
                ws.Cells[rowIndex, 3].Value = v.TotalAmount;
                ws.Cells[rowIndex, 4].Value = rec.BankPaymentDate.ToString("yyyy-MM-dd");
                ws.Cells[rowIndex, 5].Value = rec.BankReceiptNumber;
                ws.Cells[rowIndex, 6].Value = rec.Notes;
                rowIndex++;
            }
            ws.Cells.AutoFitColumns();
        }

        private void ExportContracts(ExcelPackage p, string[] headers)
        {
            PrepareSheet(p, "Contracts", headers, out var ws);
            var trans = db.ContractTransactions.Include(t => t.Lawyer).Include(t => t.ContractType).Include(t => t.ExemptionReason).Include(t => t.Parties.Select(pp => pp.PartyRole)).Include(t => t.Parties.Select(pp => pp.Province)).ToList();
            int rowIndex = 2; // ✅
            foreach (var t in trans)
            {
                ws.Cells[rowIndex, 1].Value = t.Lawyer?.NationalIdNumber;
                ws.Cells[rowIndex, 2].Value = t.ContractType?.Name;
                ws.Cells[rowIndex, 3].Value = t.TransactionDate.ToString("yyyy-MM-dd");
                ws.Cells[rowIndex, 4].Value = t.FinalFee;
                ws.Cells[rowIndex, 5].Value = t.Status;
                ws.Cells[rowIndex, 6].Value = t.Notes;
                ws.Cells[rowIndex, 7].Value = t.IsExempt ? "نعم" : "لا";
                ws.Cells[rowIndex, 8].Value = t.ExemptionReason?.Reason;
                var p1 = t.Parties.FirstOrDefault(x => x.PartyType == 1);
                if (p1 != null) { ws.Cells[rowIndex, 9].Value = p1.PartyName; ws.Cells[rowIndex, 10].Value = p1.PartyIDNumber; ws.Cells[rowIndex, 11].Value = p1.PartyRole?.Name; ws.Cells[rowIndex, 12].Value = p1.Province?.Name; }
                var p2 = t.Parties.FirstOrDefault(x => x.PartyType == 2);
                if (p2 != null) { ws.Cells[rowIndex, 13].Value = p2.PartyName; ws.Cells[rowIndex, 14].Value = p2.PartyIDNumber; ws.Cells[rowIndex, 15].Value = p2.PartyRole?.Name; ws.Cells[rowIndex, 16].Value = p2.Province?.Name; }
                rowIndex++;
            }
            ws.Cells.AutoFitColumns();
        }

        private void ExportStampContractors(ExcelPackage p, string[] headers)
        {
            PrepareSheet(p, "StampContractors", headers, out var ws);
            int rowIndex = 2; // ✅
            foreach (var c in db.StampContractors)
            {
                ws.Cells[rowIndex, 1].Value = c.Name;
                ws.Cells[rowIndex, 2].Value = c.Phone;
                ws.Cells[rowIndex, 3].Value = c.NationalId;
                ws.Cells[rowIndex, 4].Value = c.Governorate;
                ws.Cells[rowIndex, 5].Value = c.Location;
                rowIndex++;
            }
            ws.Cells.AutoFitColumns();
        }

        private void ExportStampBooks(ExcelPackage p, string[] headers)
        {
            PrepareSheet(p, "StampBooks", headers, out var ws);
            int rowIndex = 2; // ✅
            foreach (var b in db.StampBooks)
            {
                ws.Cells[rowIndex, 1].Value = b.StartSerial;
                ws.Cells[rowIndex, 2].Value = b.EndSerial;
                ws.Cells[rowIndex, 3].Value = b.ValuePerStamp;
                ws.Cells[rowIndex, 4].Value = b.Status;
                rowIndex++;
            }
            ws.Cells.AutoFitColumns();
        }

        private void ExportStampSales(ExcelPackage p, string[] headers)
        {
            PrepareSheet(p, "StampSales", headers, out var ws);
            int rowIndex = 2; // ✅
            foreach (var s in db.StampSales.Include(x => x.Stamp).Include(x => x.Contractor).Include(x => x.Lawyer))
            {
                ws.Cells[rowIndex, 1].Value = s.Stamp?.SerialNumber; ws.Cells[rowIndex, 2].Value = s.Contractor?.Name;
                ws.Cells[rowIndex, 3].Value = s.Lawyer?.NationalIdNumber; ws.Cells[rowIndex, 4].Value = s.SaleDate.ToString("yyyy-MM-dd");
                ws.Cells[rowIndex, 5].Value = s.IsPaidToLawyer ? "نعم" : "لا"; rowIndex++;
            }
            ws.Cells.AutoFitColumns();
        }

        private void ExportOralResults(ExcelPackage p, string[] headers)
        {
            PrepareSheet(p, "OralResults", headers, out var ws);
            int rowIndex = 2; // ✅
            foreach (var x in db.OralExamEnrollments.Include(t => t.Trainee))
            {
                ws.Cells[rowIndex, 1].Value = x.Trainee?.NationalIdNumber;
                ws.Cells[rowIndex, 2].Value = x.ExamDate.ToString("yyyy-MM-dd");
                ws.Cells[rowIndex, 3].Value = x.Result;
                ws.Cells[rowIndex, 4].Value = x.Score;
                ws.Cells[rowIndex, 5].Value = x.Notes;
                rowIndex++;
            }
            ws.Cells.AutoFitColumns();
        }

        private void ExportResearch(ExcelPackage p, string[] headers)
        {
            PrepareSheet(p, "Research", headers, out var ws);
            int rowIndex = 2; // ✅
            foreach (var x in db.LegalResearches.Include(t => t.Trainee))
            {
                ws.Cells[rowIndex, 1].Value = x.Trainee?.NationalIdNumber;
                ws.Cells[rowIndex, 2].Value = x.Title;
                ws.Cells[rowIndex, 3].Value = x.SubmissionDate.ToString("yyyy-MM-dd");
                ws.Cells[rowIndex, 4].Value = x.Status;
                rowIndex++;
            }
            ws.Cells.AutoFitColumns();
        }

        private void ExportLoans(ExcelPackage p, string[] headers)
        {
            PrepareSheet(p, "Loans", headers, out var ws);
            int rowIndex = 2; // ✅
            foreach (var x in db.LoanApplications.Include(t => t.Lawyer))
            {
                ws.Cells[rowIndex, 1].Value = x.Lawyer?.NationalIdNumber;
                ws.Cells[rowIndex, 2].Value = x.Amount;
                ws.Cells[rowIndex, 3].Value = x.InstallmentCount;
                ws.Cells[rowIndex, 4].Value = x.StartDate.ToString("yyyy-MM-dd");
                ws.Cells[rowIndex, 5].Value = x.Status;
                ws.Cells[rowIndex, 6].Value = x.Notes;
                rowIndex++;
            }
            ws.Cells.AutoFitColumns();
        }
        #endregion

        #region 6. دوال الاستيراد (Import Logic Methods)
        private void ImportLookupsLogic(ExcelWorksheet sheet, string entityType, int rowCount)
        {
            for (int row = 2; row <= rowCount; row++)
            {
                string name = sheet.Cells[row, 1].Text.Trim();
                if (string.IsNullOrEmpty(name)) continue;

                if (entityType == "UserTypes" && !db.UserTypes.Any(x => x.NameArabic == name))
                    db.UserTypes.Add(new UserTypeModel { NameArabic = name, NameEnglish = sheet.Cells[row, 2].Text });

                else if (entityType == "ApplicationStatuses" && !db.ApplicationStatuses.Any(x => x.Name == name)) db.ApplicationStatuses.Add(new ApplicationStatus { Name = name });
                else if (entityType == "Genders" && !db.Genders.Any(x => x.Name == name)) db.Genders.Add(new Gender { Name = name });
                else if (entityType == "NationalIdTypes" && !db.NationalIdTypes.Any(x => x.Name == name)) db.NationalIdTypes.Add(new NationalIdType { Name = name });
                else if (entityType == "Provinces" && !db.Provinces.Any(x => x.Name == name)) db.Provinces.Add(new Province { Name = name });
                else if (entityType == "PartyRoles" && !db.PartyRoles.Any(x => x.Name == name)) db.PartyRoles.Add(new PartyRole { Name = name });
                else if (entityType == "ContractExemptionReasons" && !db.ContractExemptionReasons.Any(x => x.Reason == name)) db.ContractExemptionReasons.Add(new ContractExemptionReason { Reason = name });
                else if (entityType == "ContractTypes" && !db.ContractTypes.Any(x => x.Name == name)) db.ContractTypes.Add(new ContractType { Name = name });
                else if (entityType == "Currencies" && !db.Currencies.Any(x => x.Name == name)) db.Currencies.Add(new Currency { Name = name, Symbol = sheet.Cells[row, 2].Text });

                else if (entityType == "QualificationTypes" && !db.QualificationTypes.Any(x => x.Name == name))
                {
                    double.TryParse(sheet.Cells[row, 2].Text, out double min);
                    db.QualificationTypes.Add(new QualificationType { Name = name, MinimumAcceptancePercentage = min });
                }
                else if (entityType == "BankAccounts")
                {
                    string accNum = sheet.Cells[row, 3].Text.Trim();
                    if (!string.IsNullOrEmpty(accNum) && !db.BankAccounts.Any(x => x.AccountNumber == accNum))
                    {
                        var curName = sheet.Cells[row, 5].Text;
                        var cur = db.Currencies.FirstOrDefault(c => c.Name == curName) ?? db.Currencies.FirstOrDefault();
                        db.BankAccounts.Add(new BankAccount { BankName = name, AccountName = sheet.Cells[row, 2].Text, AccountNumber = accNum, Iban = sheet.Cells[row, 4].Text, CurrencyId = cur?.Id ?? 1, IsActive = true });
                    }
                }
                else if (entityType == "FeeTypes")
                {
                    if (!db.FeeTypes.Any(x => x.Name == name))
                    {
                        decimal.TryParse(sheet.Cells[row, 2].Text, out decimal amt);
                        string curName = sheet.Cells[row, 3].Text;
                        string accNum = sheet.Cells[row, 4].Text;
                        decimal.TryParse(sheet.Cells[row, 5].Text, out decimal lPer);
                        decimal.TryParse(sheet.Cells[row, 6].Text, out decimal bPer);
                        var cur = db.Currencies.FirstOrDefault(c => c.Name == curName) ?? db.Currencies.FirstOrDefault();
                        var bank = db.BankAccounts.FirstOrDefault(b => b.AccountNumber == accNum) ?? db.BankAccounts.FirstOrDefault();
                        db.FeeTypes.Add(new FeeType { Name = name, DefaultAmount = amt, CurrencyId = cur?.Id ?? 1, BankAccountId = bank?.Id ?? 1, LawyerPercentage = lPer, BarSharePercentage = bPer == 0 ? 1 : bPer, IsActive = true });
                    }
                }
            }
            db.SaveChanges();
        }

        private void ImportGraduatesLogic(ExcelWorksheet sheet, int rowCount)
        {
            var genders = db.Genders.ToList();
            var statuses = db.ApplicationStatuses.ToList();
            var idTypes = db.NationalIdTypes.ToList();
            var userTypes = db.UserTypes.ToList();
            var defaultIdType = idTypes.FirstOrDefault()?.Id ?? 1;
            var defaultUserType = userTypes.FirstOrDefault(u => u.NameEnglish == "Graduate")?.Id ?? 1;
            var defaultStatus = statuses.FirstOrDefault(s => s.Name == "طلب جديد");

            for (int row = 2; row <= rowCount; row++)
            {
                string nameAr = sheet.Cells[row, 1].Text.Trim();
                string natId = sheet.Cells[row, 3].Text.Trim();
                if (string.IsNullOrEmpty(natId) || string.IsNullOrEmpty(nameAr) || db.Users.Any(u => u.Username == natId)) continue;

                var gender = genders.FirstOrDefault(g => g.Name.Contains(sheet.Cells[row, 5].Text)) ?? genders.First();
                var status = statuses.FirstOrDefault(s => s.Name.Contains(sheet.Cells[row, 9].Text)) ?? defaultStatus;
                DateTime.TryParse(sheet.Cells[row, 7].Text, out DateTime birth);
                if (birth == DateTime.MinValue) birth = new DateTime(1990, 1, 1);

                DateTime? tStart = null; if (DateTime.TryParse(sheet.Cells[row, 12].Text, out DateTime d1)) tStart = d1;
                DateTime? pStart = null; if (DateTime.TryParse(sheet.Cells[row, 13].Text, out DateTime d2)) pStart = d2;
                long.TryParse(sheet.Cells[row, 30].Text, out long tId);

                var user = new UserModel { FullNameArabic = nameAr, Username = natId, IdentificationNumber = natId, Email = $"{natId}@sys.local", UserTypeId = defaultUserType, IsActive = true, HashedPassword = PasswordHelper.HashPassword("User@1234") };
                db.Users.Add(user);

                var app = new GraduateApplication
                {
                    ArabicName = nameAr,
                    EnglishName = sheet.Cells[row, 2].Text,
                    NationalIdNumber = natId,
                    NationalIdTypeId = defaultIdType,
                    GenderId = gender.Id,
                    Nationality = sheet.Cells[row, 6].Text,
                    BirthDate = birth,
                    BirthPlace = sheet.Cells[row, 8].Text,
                    ApplicationStatusId = status.Id,
                    TraineeSerialNo = sheet.Cells[row, 10].Text,
                    MembershipId = sheet.Cells[row, 11].Text,
                    TrainingStartDate = tStart,
                    PracticeStartDate = pStart,
                    Notes = sheet.Cells[row, 14].Text,
                    BankName = sheet.Cells[row, 26].Text,
                    BankBranch = sheet.Cells[row, 27].Text,
                    AccountNumber = sheet.Cells[row, 28].Text,
                    Iban = sheet.Cells[row, 29].Text,
                    TelegramChatId = tId,
                    User = user,
                    SubmissionDate = DateTime.Now,
                    ContactInfo = new ContactInfo { MobileNumber = sheet.Cells[row, 15].Text, Email = sheet.Cells[row, 16].Text, Governorate = sheet.Cells[row, 17].Text, City = sheet.Cells[row, 18].Text, Street = sheet.Cells[row, 19].Text, BuildingNumber = sheet.Cells[row, 20].Text }
                };
                db.GraduateApplications.Add(app);
            }
            db.SaveChanges();

            for (int row = 2; row <= rowCount; row++)
            {
                string traineeId = sheet.Cells[row, 3].Text.Trim();
                string superId = sheet.Cells[row, 31].Text.Trim();
                if (!string.IsNullOrEmpty(superId))
                {
                    var trainee = db.GraduateApplications.FirstOrDefault(g => g.NationalIdNumber == traineeId);
                    var super = db.GraduateApplications.FirstOrDefault(g => g.NationalIdNumber == superId);
                    if (trainee != null && super != null && trainee.Id != super.Id)
                    {
                        trainee.SupervisorId = super.Id;
                        db.Entry(trainee).State = EntityState.Modified;
                    }
                }
            }
            db.SaveChanges();
        }

        private void ImportExamsLogic(ExcelWorksheet sheet, int rowCount)
        {
            var types = db.ExamTypes.ToList();
            for (int row = 2; row <= rowCount; row++)
            {
                string title = sheet.Cells[row, 1].Text.Trim();
                if (string.IsNullOrEmpty(title) || db.Exams.Any(e => e.Title == title)) continue;
                var type = types.FirstOrDefault(t => t.Name.Contains(sheet.Cells[row, 2].Text)) ?? types.First();
                DateTime.TryParse(sheet.Cells[row, 3].Text, out DateTime start);
                DateTime.TryParse(sheet.Cells[row, 4].Text, out DateTime end);
                int.TryParse(sheet.Cells[row, 5].Text, out int dur);
                double.TryParse(sheet.Cells[row, 6].Text, out double pass);
                db.Exams.Add(new Exam { Title = title, ExamTypeId = type.Id, StartTime = start, EndTime = end, DurationInMinutes = dur > 0 ? dur : 120, PassingPercentage = pass > 0 ? pass : 50, IsActive = false });
            }
            db.SaveChanges();
        }

        private void ImportExamResultsLogic(ExcelWorksheet sheet, int rowCount)
        {
            var exams = db.Exams.ToList();
            var grads = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber }).ToList();

            for (int row = 2; row <= rowCount; row++)
            {
                string exTitle = sheet.Cells[row, 1].Text;
                string natId = sheet.Cells[row, 2].Text;
                if (string.IsNullOrEmpty(natId)) continue;

                var exam = exams.FirstOrDefault(e => e.Title == exTitle);
                var grad = grads.FirstOrDefault(g => g.NationalIdNumber == natId);
                if (exam == null || grad == null) continue;
                if (db.ExamEnrollments.Any(e => e.ExamId == exam.Id && e.GraduateApplicationId == grad.Id)) continue;

                double.TryParse(sheet.Cells[row, 3].Text, out double score);
                db.ExamEnrollments.Add(new ExamEnrollment { ExamId = exam.Id, GraduateApplicationId = grad.Id, Score = score, Result = sheet.Cells[row, 4].Text });
            }
            db.SaveChanges();
        }

        private void ImportPaymentsLogic(ExcelWorksheet sheet, int rowCount)
        {
            var fees = db.FeeTypes.ToList();
            var defBank = db.BankAccounts.FirstOrDefault();
            if (defBank == null) throw new Exception("لا يوجد حسابات بنكية معرفة.");
            var grads = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber }).ToList();
            var admin = db.Users.FirstOrDefault();

            for (int row = 2; row <= rowCount; row++)
            {
                string natId = sheet.Cells[row, 1].Text;
                if (string.IsNullOrEmpty(natId)) continue;
                var grad = grads.FirstOrDefault(g => g.NationalIdNumber == natId);
                if (grad == null) continue;

                string feeName = sheet.Cells[row, 2].Text;
                var fee = fees.FirstOrDefault(f => f.Name.Contains(feeName));
                decimal.TryParse(sheet.Cells[row, 3].Text, out decimal amt);
                DateTime.TryParse(sheet.Cells[row, 4].Text, out DateTime date);
                if (date == DateTime.MinValue) date = DateTime.Now;

                var voucher = new PaymentVoucher { GraduateApplicationId = grad.Id, TotalAmount = amt, IssueDate = date, ExpiryDate = date.AddMonths(1), Status = "مسدد", PaymentMethod = "بنكي", IssuedByUserId = admin.Id, IssuedByUserName = "System" };
                voucher.VoucherDetails = new List<VoucherDetail> { new VoucherDetail { FeeTypeId = fee?.Id ?? fees.First().Id, BankAccountId = fee?.BankAccountId ?? defBank.Id, Amount = amt, Description = feeName } };
                db.PaymentVouchers.Add(voucher);
                db.SaveChanges();

                int year = date.Year;
                var lastSeq = db.Receipts.Where(x => x.Year == year).Max(x => (int?)x.SequenceNumber) ?? 0;
                db.Receipts.Add(new Receipt { Id = voucher.Id, Year = year, SequenceNumber = lastSeq + 1, BankPaymentDate = date, BankReceiptNumber = sheet.Cells[row, 5].Text, CreationDate = DateTime.Now, Notes = sheet.Cells[row, 6].Text, IssuedByUserId = admin.Id, IssuedByUserName = "System" });
                db.SaveChanges();
            }
        }

        private void ImportContractsLogic(ExcelWorksheet sheet, int rowCount)
        {
            var lawyers = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber }).ToList();
            var types = db.ContractTypes.ToList();
            var provs = db.Provinces.ToList();
            var roles = db.PartyRoles.ToList();
            var admin = db.Users.FirstOrDefault();

            for (int row = 2; row <= rowCount; row++)
            {
                string lawId = sheet.Cells[row, 1].Text;
                var lawyer = lawyers.FirstOrDefault(l => l.NationalIdNumber == lawId);
                if (lawyer == null) continue;

                var cType = types.FirstOrDefault(c => c.Name.Contains(sheet.Cells[row, 2].Text)) ?? types.First();
                DateTime.TryParse(sheet.Cells[row, 3].Text, out DateTime date);
                decimal.TryParse(sheet.Cells[row, 4].Text, out decimal fee);

                var trans = new ContractTransaction { LawyerId = lawyer.Id, ContractTypeId = cType.Id, TransactionDate = date == DateTime.MinValue ? DateTime.Now : date, FinalFee = fee, Status = "مكتمل", Notes = sheet.Cells[row, 6].Text, EmployeeId = admin.Id, CertificationDate = date, IsActingForSelf = true };
                db.ContractTransactions.Add(trans);
                db.SaveChanges();

                var p1Prov = provs.FirstOrDefault(p => p.Name == sheet.Cells[row, 12].Text) ?? provs.First();
                var p1Role = roles.FirstOrDefault(r => r.Name == sheet.Cells[row, 11].Text) ?? roles.First();
                if (!string.IsNullOrEmpty(sheet.Cells[row, 9].Text))
                    db.TransactionParties.Add(new TransactionParty { ContractTransactionId = trans.Id, PartyType = 1, PartyName = sheet.Cells[row, 9].Text, PartyIDNumber = sheet.Cells[row, 10].Text, PartyRoleId = p1Role.Id, ProvinceId = p1Prov.Id });

                var p2Prov = provs.FirstOrDefault(p => p.Name == sheet.Cells[row, 16].Text) ?? provs.First();
                var p2Role = roles.FirstOrDefault(r => r.Name == sheet.Cells[row, 15].Text) ?? roles.First();
                if (!string.IsNullOrEmpty(sheet.Cells[row, 13].Text))
                    db.TransactionParties.Add(new TransactionParty { ContractTransactionId = trans.Id, PartyType = 2, PartyName = sheet.Cells[row, 13].Text, PartyIDNumber = sheet.Cells[row, 14].Text, PartyRoleId = p2Role.Id, ProvinceId = p2Prov.Id });

                db.SaveChanges();
            }
        }

        private void ImportContractorsLogic(ExcelWorksheet sheet, int rowCount)
        {
            var existingContractors = db.StampContractors.Select(c => c.Name).ToList();
            for (int row = 2; row <= rowCount; row++)
            {
                string name = sheet.Cells[row, 1].Text.Trim();
                if (string.IsNullOrEmpty(name) || existingContractors.Contains(name)) continue;
                db.StampContractors.Add(new StampContractor
                {
                    Name = name,
                    Phone = sheet.Cells[row, 2].Text.Trim(),
                    NationalId = sheet.Cells[row, 3].Text.Trim(),
                    Governorate = sheet.Cells[row, 4].Text.Trim(),
                    Location = sheet.Cells[row, 5].Text.Trim(),
                    IsActive = true
                });
                existingContractors.Add(name);
            }
            db.SaveChanges();
        }

        private void ImportStampBooksLogic(ExcelWorksheet sheet, int rowCount)
        {
            var contractors = db.StampContractors.ToList();
            for (int row = 2; row <= rowCount; row++)
            {
                long.TryParse(sheet.Cells[row, 1].Text, out long startSerial);
                long.TryParse(sheet.Cells[row, 2].Text, out long endSerial);
                decimal.TryParse(sheet.Cells[row, 3].Text, out decimal value);
                string status = sheet.Cells[row, 4].Text.Trim();
                string contractorName = sheet.Cells[row, 5].Text.Trim();

                if (startSerial == 0 || endSerial == 0 || endSerial < startSerial) continue;
                if (db.Stamps.Any(s => s.SerialNumber >= startSerial && s.SerialNumber <= endSerial)) continue;

                int quantity = (int)(endSerial - startSerial + 1);
                var book = new StampBook
                {
                    StartSerial = startSerial,
                    EndSerial = endSerial,
                    Quantity = quantity,
                    ValuePerStamp = value,
                    DateAdded = DateTime.Now,
                    CouncilDecisionRef = "أرشيف مستورد",
                    Status = status
                };
                db.StampBooks.Add(book);
                db.SaveChanges();

                var stamps = new List<Stamp>();
                var contractor = contractors.FirstOrDefault(c => c.Name == contractorName);

                for (long serial = startSerial; serial <= endSerial; serial++)
                {
                    stamps.Add(new Stamp
                    {
                        StampBookId = book.Id,
                        SerialNumber = serial,
                        Value = value,
                        Status = (status == "متاح") ? "في المخزن" : (status == "مصروف" ? "مع المتعهد" : "مباع"),
                        ContractorId = contractor?.Id,
                        IsPaidToLawyer = false
                    });
                }
                db.Stamps.AddRange(stamps);

                if (contractor != null)
                {
                    db.StampBookIssuances.Add(new StampBookIssuance
                    {
                        ContractorId = contractor.Id,
                        StampBookId = book.Id,
                        IssuanceDate = DateTime.Now,
                        PaymentVoucherId = 0
                    });
                }
                db.SaveChanges();
            }
        }

        private void ImportStampSalesLogic(ExcelWorksheet sheet, int rowCount)
        {
            var lawyers = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber, g.MembershipId, g.ArabicName, g.BankName, g.AccountNumber }).ToList();
            var contractors = db.StampContractors.ToList();
            var admin = db.Users.FirstOrDefault();

            for (int row = 2; row <= rowCount; row++)
            {
                long.TryParse(sheet.Cells[row, 1].Text, out long serial);
                string contractorName = sheet.Cells[row, 2].Text.Trim();
                string lawyerIdNum = sheet.Cells[row, 3].Text.Trim();
                string saleDateTxt = sheet.Cells[row, 4].Text.Trim();
                bool isPaid = (sheet.Cells[row, 5].Text.Trim() == "نعم");

                var stamp = db.Stamps.FirstOrDefault(s => s.SerialNumber == serial);
                if (stamp == null) continue;

                var lawyer = lawyers.FirstOrDefault(l => l.NationalIdNumber == lawyerIdNum);
                var contractor = contractors.FirstOrDefault(c => c.Name == contractorName);
                if (contractor == null) continue;

                DateTime.TryParse(saleDateTxt, out DateTime date);
                if (date == DateTime.MinValue) date = DateTime.Now;

                stamp.Status = "مباع";
                stamp.ContractorId = contractor.Id;
                stamp.SoldToLawyerId = lawyer?.Id;
                stamp.DateSold = date;
                stamp.IsPaidToLawyer = isPaid;

                var sale = new StampSale
                {
                    StampId = stamp.Id,
                    ContractorId = contractor.Id,
                    SaleDate = date,
                    GraduateApplicationId = lawyer?.Id,
                    LawyerMembershipId = lawyer?.MembershipId ?? "غير معروف",
                    LawyerName = lawyer?.ArabicName ?? "غير معروف",
                    StampValue = stamp.Value,
                    AmountToLawyer = stamp.Value * 0.40m,
                    AmountToBar = stamp.Value * 0.60m,
                    IsPaidToLawyer = isPaid,
                    BankSendDate = isPaid ? (DateTime?)DateTime.Now : null,
                    RecordedByUserId = admin.Id,
                    RecordedByUserName = "System",
                    LawyerBankName = lawyer?.BankName,
                    LawyerAccountNumber = lawyer?.AccountNumber
                };
                db.StampSales.Add(sale);
            }
            db.SaveChanges();
        }

        private void ImportOralExamResultsLogic(ExcelWorksheet sheet, int rowCount)
        {
            var trainees = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber }).ToList();
            var defaultCommittee = db.OralExamCommittees.FirstOrDefault(c => c.CommitteeName == "لجنة الأرشيف المستورد");
            if (defaultCommittee == null)
            {
                defaultCommittee = new OralExamCommittee { CommitteeName = "لجنة الأرشيف المستورد", FormationDate = DateTime.Now, IsActive = false };
                db.OralExamCommittees.Add(defaultCommittee);
                db.SaveChanges();
            }

            for (int row = 2; row <= rowCount; row++)
            {
                string nationalId = sheet.Cells[row, 1].Text.Trim();
                string dateTxt = sheet.Cells[row, 2].Text.Trim();
                string result = sheet.Cells[row, 3].Text.Trim();
                string scoreTxt = sheet.Cells[row, 4].Text.Trim();
                string notes = sheet.Cells[row, 5].Text.Trim();

                var trainee = trainees.FirstOrDefault(t => t.NationalIdNumber == nationalId);
                if (trainee == null) continue;
                if (db.OralExamEnrollments.Any(e => e.GraduateApplicationId == trainee.Id && e.OralExamCommitteeId == defaultCommittee.Id)) continue;

                DateTime.TryParse(dateTxt, out DateTime examDate);
                if (examDate == DateTime.MinValue) examDate = DateTime.Now;

                double? score = null;
                if (double.TryParse(scoreTxt, out double s)) score = s;

                db.OralExamEnrollments.Add(new OralExamEnrollment
                {
                    GraduateApplicationId = trainee.Id,
                    OralExamCommitteeId = defaultCommittee.Id,
                    ExamDate = examDate,
                    Result = !string.IsNullOrEmpty(result) ? result : "ناجح",
                    Score = score,
                    Notes = notes + " (استيراد)"
                });
            }
            db.SaveChanges();
        }

        private void ImportLegalResearchLogic(ExcelWorksheet sheet, int rowCount)
        {
            var trainees = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber }).ToList();
            for (int row = 2; row <= rowCount; row++)
            {
                string nationalId = sheet.Cells[row, 1].Text.Trim();
                string title = sheet.Cells[row, 2].Text.Trim();
                string dateTxt = sheet.Cells[row, 3].Text.Trim();
                string status = sheet.Cells[row, 4].Text.Trim();

                var trainee = trainees.FirstOrDefault(t => t.NationalIdNumber == nationalId);
                if (trainee == null) continue;
                if (db.LegalResearches.Any(r => r.GraduateApplicationId == trainee.Id && r.Title == title)) continue;

                DateTime.TryParse(dateTxt, out DateTime subDate);
                if (subDate == DateTime.MinValue) subDate = DateTime.Now;

                db.LegalResearches.Add(new LegalResearch
                {
                    GraduateApplicationId = trainee.Id,
                    Title = !string.IsNullOrEmpty(title) ? title : "بحث قانوني (أرشيف)",
                    SubmissionDate = subDate,
                    Status = !string.IsNullOrEmpty(status) ? status : "مقبول"
                });
            }
            db.SaveChanges();
        }

        private void ImportLoansLogic(ExcelWorksheet sheet, int rowCount)
        {
            var lawyers = db.GraduateApplications.Select(g => new { g.Id, g.NationalIdNumber }).ToList();
            var loanTypes = db.LoanTypes.ToList();
            var defaultLoanType = loanTypes.FirstOrDefault();
            if (defaultLoanType == null)
            {
                var bank = db.BankAccounts.FirstOrDefault();
                if (bank == null) throw new Exception("يجب تعريف حساب بنكي واحد على الأقل.");
                defaultLoanType = new LoanType { Name = "قرض أرشيف", BankAccountForRepaymentId = bank.Id, MaxAmount = 10000, MaxInstallments = 24 };
                db.LoanTypes.Add(defaultLoanType);
                db.SaveChanges();
            }

            for (int row = 2; row <= rowCount; row++)
            {
                string nationalId = sheet.Cells[row, 1].Text.Trim();
                string amountTxt = sheet.Cells[row, 2].Text.Trim();
                string installmentsCountTxt = sheet.Cells[row, 3].Text.Trim();
                string startDateTxt = sheet.Cells[row, 4].Text.Trim();
                string status = sheet.Cells[row, 5].Text.Trim();
                string notes = sheet.Cells[row, 6].Text.Trim();

                var lawyer = lawyers.FirstOrDefault(l => l.NationalIdNumber == nationalId);
                if (lawyer == null) continue;

                decimal.TryParse(amountTxt, out decimal amount);
                int.TryParse(installmentsCountTxt, out int count);
                if (count == 0) count = 10;

                DateTime.TryParse(startDateTxt, out DateTime start);
                if (start == DateTime.MinValue) start = DateTime.Now;

                decimal installmentAmount = amount / count;

                var loan = new LoanApplication
                {
                    LawyerId = lawyer.Id,
                    LoanTypeId = defaultLoanType.Id,
                    Amount = amount,
                    InstallmentCount = count,
                    InstallmentAmount = installmentAmount,
                    StartDate = start,
                    ApplicationDate = start,
                    Status = status,
                    IsDisbursed = true,
                    DisbursementDate = start,
                    Notes = notes + " (استيراد)"
                };
                db.LoanApplications.Add(loan);
                db.SaveChanges();

                var installments = new List<LoanInstallment>();
                for (int i = 1; i <= count; i++)
                {
                    installments.Add(new LoanInstallment
                    {
                        LoanApplicationId = loan.Id,
                        InstallmentNumber = i,
                        DueDate = start.AddMonths(i - 1),
                        Amount = installmentAmount,
                        Status = (status == "مكتمل" || status == "مسدد") ? "مدفوع" : "مستحق"
                    });
                }
                db.LoanInstallments.AddRange(installments);
                db.SaveChanges();
            }
        }
        #endregion
        private void ImportOpeningJournalLogic(ExcelWorksheet sheet, int rowCount)
        {
            // 1. تجهيز البيانات الأساسية
            var accounts = db.Accounts.ToList();
            var admin = db.Users.FirstOrDefault();

            // إنشاء رأس القيد
            var entry = new JournalEntry
            {
                // ✅ تصحيح 1: تغيير Date إلى EntryDate (أو الاسم الموجود في الموديل الخاص بك)
                EntryDate = new DateTime(DateTime.Now.Year, 1, 1),

                Description = "القيد الافتتاحي (مرحل من النظام السابق)",
                ReferenceNumber = "OP-" + DateTime.Now.Year,
                IsPosted = true,
                CreatedByUserId = admin?.Id,
                CreatedAt = DateTime.Now,
                TotalDebit = 0,
                TotalCredit = 0,
                JournalEntryDetails = new List<JournalEntryDetail>()
            };

            decimal totalDebit = 0;
            decimal totalCredit = 0;

            // 2. قراءة الملف
            for (int row = 2; row <= rowCount; row++)
            {
                string accCode = sheet.Cells[row, 1].Text.Trim();
                string accName = sheet.Cells[row, 2].Text.Trim();

                if (string.IsNullOrEmpty(accCode)) continue;

                decimal.TryParse(sheet.Cells[row, 3].Text, out decimal debit);
                decimal.TryParse(sheet.Cells[row, 4].Text, out decimal credit);
                string notes = sheet.Cells[row, 6].Text.Trim();

                if (debit == 0 && credit == 0) continue;

                // البحث عن الحساب أو إنشاؤه
                var account = accounts.FirstOrDefault(a => a.Code == accCode);
                if (account == null)
                {
                    account = new Account
                    {
                        Code = accCode,
                        Name = !string.IsNullOrEmpty(accName) ? accName : $"حساب {accCode}",

                        // ❌ تصحيح 2: تم حذف ParentAccountId لأنه غير موجود في الموديل لديك

                        // ✅ تصحيح 3: استخدام قيمة Enum بدلاً من النص
                        // نفترض أن القيمة 1 تعني (أصول) أو (ميزانية) حسب الـ Enum لديك
                        // يمكنك تغيير (AccountType)1 إلى BarManegment.Models.AccountType.Asset إذا كنت تعرف الاسم
                        AccountType = (BarManegment.Models.AccountType)1,

                        IsTransactional = true,
                        OpeningBalance = 0 // الرصيد نثبته عبر القيد وليس هنا
                    };
                    db.Accounts.Add(account);
                    db.SaveChanges();
                    accounts.Add(account);
                }

                var detail = new JournalEntryDetail
                {
                    AccountId = account.Id,
                    Debit = debit,
                    Credit = credit,
                    Description = string.IsNullOrEmpty(notes) ? "رصيد افتتاحي" : notes
                };

                entry.JournalEntryDetails.Add(detail);
                totalDebit += debit;
                totalCredit += credit;
            }

            // 3. التحقق من التوازن
            if (Math.Abs(totalDebit - totalCredit) > 0.01m)
            {
                var diffAccount = db.Accounts.FirstOrDefault(a => a.Name == "فروقات أرصدة افتتاحية");
                if (diffAccount == null)
                {
                    diffAccount = new Account
                    {
                        Code = "9999",
                        Name = "فروقات أرصدة افتتاحية",
                        // ✅ تصحيح 3 مكرر: استخدام Enum
                        AccountType = (BarManegment.Models.AccountType)1,
                        IsTransactional = true
                    };
                    db.Accounts.Add(diffAccount);
                    db.SaveChanges();
                }

                decimal diff = totalDebit - totalCredit;
                var diffRow = new JournalEntryDetail
                {
                    AccountId = diffAccount.Id,
                    Description = "تسوية فروقات القيد الافتتاحي"
                };

                if (diff > 0) diffRow.Credit = diff;
                else diffRow.Debit = Math.Abs(diff);

                entry.JournalEntryDetails.Add(diffRow);

                if (diff > 0) totalCredit += diff; else totalDebit += Math.Abs(diff);
            }

            entry.TotalDebit = totalDebit;
            entry.TotalCredit = totalCredit;

            db.JournalEntries.Add(entry);
            db.SaveChanges();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}