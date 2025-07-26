using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace SmartComply.ViewModels
{
    public class FormBuilderViewModel
    {
        public int FormId { get; set; }
        public int CategoryId { get; set; }
        public List<FormElementViewModel> FormElements { get; set; }
        public IEnumerable<SelectListItem> Categories { get; set; }

        [Required(ErrorMessage = "Form name is required")]
        public string FormName { get; set; }
  }
}
