using System.Collections.Generic;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class PermissionsViewModel
    {
        public int? SelectedUserTypeId { get; set; }
        public string UserTypeName { get; set; }
        public SelectList UserTypes { get; set; }
        public List<ModulePermissionViewModel> Modules { get; set; }

        public PermissionsViewModel()
        {
            Modules = new List<ModulePermissionViewModel>();
        }
    }

    public class ModulePermissionViewModel
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }

        // الصلاحيات الأساسية
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }

        // الصلاحيات الإضافية
        public bool CanExport { get; set; }
        public bool CanImport { get; set; }
    }
}