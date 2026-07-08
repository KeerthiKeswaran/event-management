using System.ComponentModel.DataAnnotations;

namespace Event.Models.DTOs
{
    public class AllocateStaffRequest
    {
        [Required]
        public int EmployeeId { get; set; }
    }
}
