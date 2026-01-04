using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("TransactionParties")]
    public class TransactionParty
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "المعاملة")]
        public int ContractTransactionId { get; set; }
        [ForeignKey("ContractTransactionId")]
        public virtual ContractTransaction ContractTransaction { get; set; }

        [Required]
        [Display(Name = "نوع الطرف (1=طرف أول, 2=طرف ثاني)")]
        public int PartyType { get; set; }

        [Required]
        [Display(Name = "اسم الطرف")]
        [StringLength(200)]
        public string PartyName { get; set; }

        [Required]
        [Display(Name = "رقم الهوية")]
        [StringLength(50)]
        public string PartyIDNumber { get; set; }

        [Required]
        [Display(Name = "المحافظة")]
        public int ProvinceId { get; set; }
        [ForeignKey("ProvinceId")]
        public virtual Province Province { get; set; }

        [Required]
        [Display(Name = "صفة الطرف")]
        public int PartyRoleId { get; set; }
        [ForeignKey("PartyRoleId")]
        public virtual PartyRole PartyRole { get; set; }
    }
}