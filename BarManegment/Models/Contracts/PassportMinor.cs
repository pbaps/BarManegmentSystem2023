using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarManegment.Models
{
    [Table("PassportMinors")]
    public class PassportMinor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "المعاملة")]
        public int ContractTransactionId { get; set; }
        [ForeignKey("ContractTransactionId")]
        public virtual ContractTransaction ContractTransaction { get; set; }

        [Required]
        [Display(Name = "اسم القاصر")]
        [StringLength(200)]
        public string MinorName { get; set; }

        [Required]
        [Display(Name = "رقم هوية القاصر")]
        [StringLength(50)]
        public string MinorIDNumber { get; set; }

 

        // 💡💡 === بداية التعديل === 💡💡
        [Required]
        [Display(Name = "صفة القاصر (علاقته بالموكل)")]
        public int MinorRelationshipId { get; set; } // (استبدال GuardianRoleId)

        [ForeignKey("MinorRelationshipId")]
        public virtual MinorRelationship MinorRelationship { get; set; }
        // 💡💡 === نهاية التعديل === 💡💡
    }
}