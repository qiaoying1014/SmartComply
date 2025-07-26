using System.ComponentModel.DataAnnotations.Schema;

namespace SmartComply.Models
{
    public class Form
    {
        public int FormId { get; set; }
        public int CategoryId { get; set; } // Foreign key
        [ForeignKey("CategoryId")]
        public ComplianceCategory Category { get; set; } // Navigation property

        public DateTime CreatedDate { get; set; }
        public string Status { get; set; }

        public virtual ICollection<FormElement> FormElements { get; set; } = new List<FormElement>();
        public string FormName { get; set; }

    }

}
