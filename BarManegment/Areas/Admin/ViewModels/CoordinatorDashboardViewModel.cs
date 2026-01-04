using BarManegment.Models;
using System.Collections.Generic;
using System.Web.Mvc;

namespace BarManegment.Areas.Admin.ViewModels
{
    public class CoordinatorDashboardViewModel
    {
        // الطلبات الواردة التي لم تدرج في جلسة بعد
        public List<AgendaItem> PendingItems { get; set; }

        // الجلسات المفتوحة (التي لم تغلق نهائياً) لاختيار واحدة منها
        public SelectList OpenSessions { get; set; }

        // البيانات المستلمة من الفورم عند الحفظ
        public int SelectedSessionId { get; set; }
        public List<int> SelectedItemIds { get; set; }
    }
}