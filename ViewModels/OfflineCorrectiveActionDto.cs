namespace SmartComply.ViewModels
{
  public class OfflineCorrectiveActionDto
  {
    public string AuditId { get; set; }
    public string Description { get; set; }
    public string RootCause { get; set; }
    public string ProposedAction { get; set; }
    public string ResponsiblePerson { get; set; }
    public string TargetDate { get; set; }
    public string CompletionDate { get; set; }
    public string Status { get; set; }
    public string Remarks { get; set; }
    public string BeforePhotoBase64 { get; set; }
    public string BeforePhotoName { get; set; }
    public string AfterPhotoBase64 { get; set; }
    public string AfterPhotoName { get; set; }
  }
}
