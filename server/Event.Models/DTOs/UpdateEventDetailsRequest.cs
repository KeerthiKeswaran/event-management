using System;
using System.ComponentModel.DataAnnotations;

namespace Event.Models.DTOs
{
    public class UpdateEventDetailsRequest
    {
        public string? Title { get; set; }
        public string? Description_Url { get; set; }
        public string? DescriptionText { get; set; }
    }
}
