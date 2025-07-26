using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartComply.Models
{
  public class ActivityLog
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int LogId { get; set; }

    [Required]
    public int StaffId { get; set; } // Reference to Staff entity

    [ForeignKey("StaffId")]
    public Staff Staff { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime ActionTimestamp { get; set; } = DateTime.UtcNow; // Timestamp of the action

    [Required]
    public ActionTypeEnum ActionType { get; set; } // Use enum for ActionType

    [StringLength(1000)]
    public string ActionDescription { get; set; } // Detailed description of the action (e.g., 'Logged in from IP address X')

    // Enum for ActionType
    public enum ActionTypeEnum
    {
      [Display(Name = "Login")]
      Login = 1,

      [Display(Name = "Logout")]
      Logout = 2,

      [Display(Name = "Add")]
      Add = 3,

      [Display(Name = "Update")]
      Update = 4,

      [Display(Name = "Delete")]
      Delete = 5
    }
  }
}
