using Newtonsoft.Json;

namespace SmartComply.ViewModels
{
    public class FormElementResponseViewModel
    {
        public int FormElementId { get; set; }
        public List<string>? Answer { get; set; }

        // Metadata (for redisplay)
        public string Label { get; set; } = string.Empty; // Default to empty string
        public string ElementType { get; set; } = string.Empty; // Default to empty string
        public string? Placeholder { get; set; }
        public bool IsRequired { get; set; }
        public string? Options { get; set; }
        public int Order { get; set; }


        [JsonIgnore] 
       public List<IFormFile> Files { get; set; } = new();


  }

}
