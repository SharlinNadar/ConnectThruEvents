using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ConnectThruEventsBackend.Models;

public class EventTimeline
{
    [Key]
    public int EventTimelineId { get; set; } // Unique ID for each timeline entry

    [Required]
    [MaxLength(500)]
    public required string EventDescription { get; set; } // A short description of the event entry

    public DateTime Timestamp { get; set; } // Date and time of the event entry

    // Foreign key for EventDetail
    public int EventDetailId { get; set; }

    // Navigation property to EventDetail
    [ForeignKey("EventDetailId")]
    public required EventDetail EventDetail { get; set; }
}
