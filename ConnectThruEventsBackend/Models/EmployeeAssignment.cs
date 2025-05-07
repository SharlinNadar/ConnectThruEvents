using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectThruEventsBackend.Models
{
    public class EmployeeAssignment
{
    [Key]
    public int EmployeeAssignmentId { get; set; }

    [Required]
    [MaxLength(100)]
    public required string EmployeeName { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Role { get; set; }

    // Foreign key for EventDetail
    [Required]
    public int EventDetailId { get; set; }

    [ForeignKey("EventDetailId")]
    public EventDetail? EventDetail { get; set; } = null!;
    
    public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
}


}
