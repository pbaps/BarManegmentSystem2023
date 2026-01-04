using BarManegment.Models;
using System.Collections.Generic;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class DecisionFollowUpViewModel
    {
        // 1. قائمة القرارات لعرضها في الجدول
        public IEnumerable<AgendaItem> Decisions { get; set; }

        // 2. قائمة الموظفين (للقائمة المنسدلة في المودال)
        public SelectList EmployeesList { get; set; }

        // 3. (الأهم) خريطة لترجمة اسم المستخدم إلى الاسم الكامل
        public Dictionary<string, string> EmployeeNameMap { get; set; }
    }
}