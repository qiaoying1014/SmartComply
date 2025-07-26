using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartComply.Models
{
  public class Branch
  {
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BranchId { get; set; }

    [Required]
    public string BranchAddress { get; set; }
    public bool BranchIsActive { get; set; } = true;

    [Required]
    public string BranchName { get; set; }
  }
}
