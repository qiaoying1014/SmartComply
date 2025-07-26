using SmartComply.Models;

namespace SmartComply.ViewModels
{
  public class ActivityLogViewModel
  {
    // Grouping information (e.g., Today, Yesterday, specific date)
    public string GroupName { get; set; }

    // List of logs for each group
    public List<ActivityLogDetails> Logs { get; set; } = new List<ActivityLogDetails>();
  }

  public class ActivityLogDetails
  {
    public int LogId { get; set; }
    public int StaffId { get; set; }
    public DateTime ActionTimestamp { get; set; }
    public ActivityLog.ActionTypeEnum ActionType { get; set; } // Use ActionTypeEnum here
    public string ActionDescription { get; set; }
  }
}
