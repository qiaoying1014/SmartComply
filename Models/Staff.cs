using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartComply.Models
{
  public class Staff
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int StaffId { get; set; }
    public string StaffName { get; set; }
    public string StaffEmail { get; set; }
    public string StaffPassword { get; set; }
    public string StaffRole { get; set; } 
    public bool StaffIsActive { get; set; } = true;
    public int? StaffBranchId { get; set; }

    [ForeignKey("StaffBranchId")]
    public Branch? StaffBranch { get; set; }

    [NotMapped]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string? NewPassword { get; set; }

    [NotMapped]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    public string? ConfirmPassword { get; set; }
  }
}
