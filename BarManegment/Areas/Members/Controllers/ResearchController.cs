using BarManegment.Models;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System;

namespace BarManegment.Areas.Members.Controllers
{
    [Authorize]
    public class ResearchController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Members/Research/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account", new { area = "Members" });

            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            var research = db.LegalResearches
                .Include(r => r.Committee)
                .Include(r => r.Decisions)
                .FirstOrDefault(r => r.Id == id);

            // تأكيد ملكية البحث
            if (research == null || research.GraduateApplicationId != graduateApp.Id)
            {
                return HttpNotFound();
            }

            return View(research);
        }

        // POST: Members/Research/UploadResearch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadResearch(int researchId, HttpPostedFileBase researchFile)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account", new { area = "Members" });
            var userId = (int)Session["UserId"];
            var graduateApp = db.GraduateApplications.FirstOrDefault(g => g.UserId == userId);

            var research = db.LegalResearches.Find(researchId);

            // تأكيد الملكية + التحقق من الملف
            if (research == null || research.GraduateApplicationId != graduateApp.Id)
            {
                return HttpNotFound();
            }

            if (researchFile == null || researchFile.ContentLength == 0)
            {
                TempData["ErrorMessage"] = "الرجاء اختيار ملف لرفعه.";
                return RedirectToAction("Details", new { id = researchId });
            }

            try
            {
                // (استخدام دالة مساعدة لحفظ الملف، مشابهة لما في ProfileController)
                string path = SaveResearchFile(researchFile, graduateApp.Id);

                // تحديث البحث
                research.FinalDocumentPath = path;
                research.Status = "تم التسليم (بانتظار المراجعة)";
                research.SubmissionDate = DateTime.Now;
                db.Entry(research).State = EntityState.Modified;
                db.SaveChanges();

                TempData["SuccessMessage"] = "تم رفع ملف البحث بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء رفع الملف: " + ex.Message;
            }

            return RedirectToAction("Details", new { id = researchId });
        }

        // دالة مساعدة لحفظ ملف البحث
        private string SaveResearchFile(HttpPostedFileBase file, int traineeId)
        {
            if (file == null || file.ContentLength == 0) return null;
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            // (يمكنك تغيير المجلد إذا أردت)
            var directoryPath = Server.MapPath($"~/Uploads/LegalResearches/{traineeId}");
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
            var path = Path.Combine(directoryPath, fileName);
            file.SaveAs(path);
            return $"/Uploads/LegalResearches/{traineeId}/{fileName}";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}