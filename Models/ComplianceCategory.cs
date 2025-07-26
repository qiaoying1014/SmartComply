using System.ComponentModel.DataAnnotations;

namespace SmartComply.Models
{
    public class ComplianceCategory
    {
        [Key]
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryDescription { get; set; }
        public bool CategoryIsEnabled { get; set; } = true;
    }
}
