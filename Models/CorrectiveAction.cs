using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartComply.Models
{
  public enum CorrectiveActionStatus
  {
    Pending,
    InProgress,
    Completed,
    Overdue
  }

  public class CorrectiveAction
  {
    [Key]
    public int CorrectiveActionId { get; set; }

    [Required]
    public int AuditId { get; set; }

    [ForeignKey(nameof(AuditId))]
    public virtual Audit Audit { get; set; }  // Navigation property

    [Required]
    [StringLength(1000)]
    public string Description { get; set; }

    [StringLength(1000)]
    public string? RootCause { get; set; }

    [Required]
    [StringLength(1000)]
    public string ProposedAction { get; set; }

    [StringLength(100)]
    public string? ResponsiblePerson { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime TargetDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? CompletionDate { get; set; }

    [Required]
    public CorrectiveActionStatus Status { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [DataType(DataType.DateTime)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Paths to uploaded images; null if no image yet
    [StringLength(255)]
    public string? BeforeActionPhotoPath { get; set; }

    [StringLength(255)]
    public string? AfterActionPhotoPath { get; set; }

    // ← new soft-delete flag (default = false)
    public bool IsDeleted { get; set; } = false;
  }
}
