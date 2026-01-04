using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

 

namespace BarManegment.Areas.Members.Controllers // نستخدم نفس Namespace الكونترولر للسهولة
{
    public class MemberContractViewModel
    {
        public int TransactionId { get; set; }
        public DateTime Date { get; set; }
        public string ContractType { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal LawyerShare { get; set; }
        public string PartiesNames { get; set; }
    }
}