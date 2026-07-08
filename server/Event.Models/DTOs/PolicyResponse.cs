using System;

namespace Event.Models.DTOs
{
    public class PolicyResponse
    {
        public string TermsId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }
}
