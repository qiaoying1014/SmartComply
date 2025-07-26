using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SmartComply.ViewModels
{
  public class StaffViewModel
  {
    public int StaffId { get; set; }

    // This is used to store the existing password when editing
    public string? ExistingPassword { get; set; }

    [Required]
    [Display(Name = "Full Name")]
    public string StaffName { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string StaffEmail { get; set; }

    // Password fields, only shown during Create, not Edit
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string? ConfirmPassword { get; set; }

    [Required]
    [Display(Name = "Branch")]
    public int? StaffBranchId { get; set; }

    [Required]
    [Display(Name = "Role")]
    public string StaffRole { get; set; }

    // Hidden inputs
    [HiddenInput]
    public bool StaffIsActive { get; set; } = true;
  }
}
