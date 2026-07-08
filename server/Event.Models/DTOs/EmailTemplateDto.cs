using System.Collections.Generic;

namespace Event.Models.DTOs
{
    public class EmailTemplateDto
    {
        public string TemplateName { get; set; } = string.Empty;
        public Dictionary<string, string> Placeholders { get; set; } = new();
    }
}
