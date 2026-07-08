using System;

namespace Event.Models.DTOs
{
    public class RegionResponse
    {
        public string Region_Id { get; set; } = string.Empty;
        public int No_Of_Staffs { get; set; }
        public string Region_Name { get; set; } = string.Empty;
    }
}
