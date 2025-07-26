using System.ComponentModel.DataAnnotations;

namespace SmartComply.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string StaffEmail { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string StaffPassword { get; set; }

  }
}
