namespace SmartComply.ViewModels
{
    public class FormFillViewModel
    {
        public int FormId { get; set; }
        public string Category { get; set; } = string.Empty; // Default to empty string
        public string? ResponderName { get; set; }
        public List<FormElementResponseViewModel> Elements { get; set; } = new();
        public int? BranchId { get; set; }
        public string? StaffEmail { get; set; }
        public string? BranchAddress { get; set; }
        public bool BranchIsActive { get; set; }

        public int AuditId { get; set; }

        public int FormResponderId { get; set; }

        public bool IsEdit { get; set; }


    }
}
