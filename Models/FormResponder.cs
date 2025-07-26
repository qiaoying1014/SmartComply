namespace SmartComply.Models
{
  public class FormResponder
  {
    public int FormResponderId { get; set; }

    public int FormId { get; set; }
    public Form Form { get; set; }
    public string Name { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FormResponse> Responses { get; set; }

    public int AuditId {  get; set; }
    public Audit Audit { get; set; }

    public int? StaffId { get; set; }
    public Staff Staff { get; set; }

    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

  }
}
