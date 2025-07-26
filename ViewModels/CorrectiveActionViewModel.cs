using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SmartComply.Models;

namespace SmartComply.ViewModels
{
  public class CorrectiveActionViewModel
  {
    // For “Edit”, carry the ID; for “Create”, this can remain null/0
    public int? CorrectiveActionId { get; set; }

    [Required]
    public int AuditId { get; set; }

    // If you want to display the Audit name in a dropdown, 
    // you can populate a SelectList in your controller.

    [Required]
    [StringLength(1000)]
    [Display(Name = "Description")]
    public string Description { get; set; }

    [StringLength(1000)]
    [Display(Name = "Root Cause")]
    public string? RootCause { get; set; }

    [Required]
    [StringLength(1000)]
    [Display(Name = "Proposed Action")]
    public string ProposedAction { get; set; }

    [StringLength(100)]
    [Display(Name = "Responsible Person")]
    public string? ResponsiblePerson { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Target Date")]
    public DateTime TargetDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Completion Date")]
    public DateTime? CompletionDate { get; set; }

    [Required]
    [Display(Name = "Status")]
    public CorrectiveActionStatus Status { get; set; }

    [StringLength(1000)]
    public string? Remarks { get; set; }

    // File uploads for photos:
    [Display(Name = "Before Photo")]
    public IFormFile? BeforePhotoUpload { get; set; }

    [Display(Name = "After Photo")]
    public IFormFile? AfterPhotoUpload { get; set; }

    // To show existing images when editing:
    public string? ExistingBeforePhotoPath { get; set; }
    public string? ExistingAfterPhotoPath { get; set; }

    public DateTime CreatedAt { get; set; }
  }
}
