using System.ComponentModel.DataAnnotations;

namespace Event.Models.DTOs
{
    public class UploadDescriptionRequest
    {
        [Required]
        public string Text { get; set; } = string.Empty;
    }
}
