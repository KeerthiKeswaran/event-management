namespace Event.Models.DTOs
{
    public class AdminProfileResponse
    {
        public string Admin_Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class UpdateAdminProfileRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class HelpdeskMetadataResponse
    {
        public List<HelpdeskAction> Actions { get; set; } = new();
        public List<HelpdeskTargetType> TargetTypes { get; set; } = new();
    }

    public class HelpdeskAction
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class HelpdeskTargetType
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
