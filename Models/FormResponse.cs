namespace SmartComply.Models
{
    public class FormResponse
    {
        public int FormResponseId { get; set; }

        public int FormResponderId { get; set; }
        public FormResponder FormResponder { get; set; }

        public int FormElementId { get; set; }
        public FormElement FormElement { get; set; }

        public string? Answer { get; set; }
    }

}
