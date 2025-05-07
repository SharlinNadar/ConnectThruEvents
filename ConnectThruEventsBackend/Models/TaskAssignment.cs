using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectThruEventsBackend.Models
{
    public class TaskAssignment
{
    [Key]
    public int TaskAssignmentId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string TaskName { get; set; }

    [Required]
    public decimal TaskCost { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Status { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Priority { get; set; }

    public int EventDetailId { get; set; }

    [ForeignKey("EventDetailId")]
    public EventDetail? EventDetail { get; set; }

    public int EmployeeAssignmentId { get; set; }

    [ForeignKey("EmployeeAssignmentId")]
    public EmployeeAssignment? EmployeeAssignment { get; set; }
}

}
