using BarManegment.Helpers;
using BarManegment.Models;
using BarManegment.Services;
using BarManegment.Areas.Admin.ViewModels;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using BarManegment.ViewModels;

namespace BarManegment.Areas.Admin.Controllers
{
    public class PermissionsController : BaseController
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        [CustomAuthorize(Permission = "CanView")]
        public ActionResult Index(int? userTypeId)
        {
            var viewModel = new PermissionsViewModel
            {
                UserTypes = new SelectList(db.UserTypes.ToList(), "Id", "NameArabic"),
                SelectedUserTypeId = userTypeId
            };

            if (userTypeId.HasValue)
            {
                var modulesInDb = db.Modules.OrderBy(m => m.NameArabic).ToList();
                var permissionsInDb = db.Permissions
                                        .Where(p => p.UserTypeId == userTypeId.Value)
                                        .ToList();

                viewModel.UserTypeName = db.UserTypes.Find(userTypeId.Value)?.NameArabic;

                foreach (var module in modulesInDb)
                {
                    var permission = permissionsInDb.FirstOrDefault(p => p.ModuleId == module.Id);
                    viewModel.Modules.Add(new ModulePermissionViewModel
                    {
                        ModuleId = module.Id,
                        ModuleName = module.NameArabic,
                        CanView = permission?.CanView ?? false,
                        CanAdd = permission?.CanAdd ?? false,
                        CanEdit = permission?.CanEdit ?? false,
                        CanDelete = permission?.CanDelete ?? false,
                        // 💡 ربط الصلاحيات الجديدة عند العرض
                        CanExport = permission?.CanExport ?? false,
                        CanImport = permission?.CanImport ?? false
                    });
                }
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [CustomAuthorize(Permission = "CanEdit")]
        public ActionResult Update(PermissionsViewModel viewModel)
        {
            // التحقق من أن الدور تم اختياره
            if (!viewModel.SelectedUserTypeId.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            foreach (var moduleVm in viewModel.Modules)
            {
                var permissionInDb = db.Permissions.FirstOrDefault(p =>
                    p.UserTypeId == viewModel.SelectedUserTypeId.Value && p.ModuleId == moduleVm.ModuleId);

                if (permissionInDb != null)
                {
                    // تحديث الصلاحيات الموجودة
                    permissionInDb.CanView = moduleVm.CanView;
                    permissionInDb.CanAdd = moduleVm.CanAdd;
                    permissionInDb.CanEdit = moduleVm.CanEdit;
                    permissionInDb.CanDelete = moduleVm.CanDelete;
                    // 💡 تحديث الصلاحيات الجديدة
                    permissionInDb.CanExport = moduleVm.CanExport;
                    permissionInDb.CanImport = moduleVm.CanImport;

                    db.Entry(permissionInDb).State = EntityState.Modified;
                }
                else
                {
                    // إنشاء صلاحية جديدة
                    var newPermission = new PermissionModel
                    {
                        UserTypeId = viewModel.SelectedUserTypeId.Value,
                        ModuleId = moduleVm.ModuleId,
                        CanView = moduleVm.CanView,
                        CanAdd = moduleVm.CanAdd,
                        CanEdit = moduleVm.CanEdit,
                        CanDelete = moduleVm.CanDelete,
                        // 💡 إضافة الصلاحيات الجديدة
                        CanExport = moduleVm.CanExport,
                        CanImport = moduleVm.CanImport
                    };
                    db.Permissions.Add(newPermission);
                }
            }

            db.SaveChanges();

            var userTypeName = db.UserTypes.Find(viewModel.SelectedUserTypeId.Value)?.NameArabic;
            AuditService.LogAction("Update", "Permissions", $"Updated permissions for role '{userTypeName}'.");

            TempData["SuccessMessage"] = "تم تحديث الصلاحيات بنجاح!";
            return RedirectToAction("Index", new { userTypeId = viewModel.SelectedUserTypeId });
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