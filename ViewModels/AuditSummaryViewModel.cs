using SmartComply.Models;

namespace SmartComply.ViewModels
{
  public class AuditSummaryViewModel
  {
    public Audit Audit { get; set; }
    public int FormCount { get; set; }
    public int CorrectiveActionCount { get; set; }
  }

}
