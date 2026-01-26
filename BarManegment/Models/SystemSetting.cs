using System.ComponentModel.DataAnnotations;

namespace BarManegment.Models
{
    public class SystemSetting
    {
        [Key]
        [StringLength(100)]
        public string SettingKey { get; set; }

        [Required]
        public string SettingValue { get; set; }

        public string Description { get; set; }

        public int? ValueInt { get; set; }


    }
}
