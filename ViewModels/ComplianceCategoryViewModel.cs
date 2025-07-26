using System.ComponentModel.DataAnnotations;

namespace SmartComply.ViewModels
{
  public class ComplianceCategoryViewModel
  {
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [Display(Name = "Name")]
    public string CategoryName { get; set; }

    [Required(ErrorMessage = "{0} is required.")]
    [Display(Name = "Description")]
    public string CategoryDescription { get; set; }

    public bool CategoryIsEnabled { get; set; } = true;
  }
}
