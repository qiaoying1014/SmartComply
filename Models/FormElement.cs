namespace SmartComply.Models
{
    public class FormElement
    {
        public int FormElementId { get; set; }
        public int FormId { get; set; }

        public string Label { get; set; }
        public string ElementType { get; set; }
        public string? Placeholder { get; set; }
        public bool IsRequired { get; set; }
        public string? Options { get; set; }
        public int Order { get; set; }

        public Form Form { get; set; }
    }
}