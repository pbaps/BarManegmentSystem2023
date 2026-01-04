namespace BarManegment.Models
{
    public class LeaveType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int DefaultDaysPerYear { get; set; } // الرصيد السنوي الافتراضي
        public bool IsPaid { get; set; } = true; // هل هي مدفوعة الراتب؟
    }
}