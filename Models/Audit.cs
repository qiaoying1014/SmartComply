using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SmartComply.Models
{
  public class Audit
  {
    [Key]
    public int AuditId { get; set; }

    public string AuditName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Draft";

    public int? StaffId { get; set; } 
    public Staff? Staff { get; set; }

    public int CategoryId { get; set; }
    public ComplianceCategory Category { get; set; }

    public ICollection<FormResponder> FormResponders { get; set; } = new List<FormResponder>();

    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; }

    /// A secret GUID string used to share this audit externally.
    public string? ShareToken { get; set; }
  }

}
