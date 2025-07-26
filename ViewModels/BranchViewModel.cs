using System.ComponentModel.DataAnnotations;

namespace SmartComply.ViewModels
{
  public class BranchViewModel
  {
    public int BranchId { get; set; }

    [Required]
    [Display(Name = "Branch Address")]
    public string BranchAddress { get; set; }

    [Display(Name = "Status")]
    public bool BranchIsActive { get; set; }

    [Required]
    [Display(Name = "Branch Name")]
    public string BranchName { get; set; }

  }
}
