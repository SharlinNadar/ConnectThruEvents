using System.ComponentModel.DataAnnotations;

namespace ConnectThruEventsBackend.Models
{
    public class PublishEvent
    {
        [Key]
        public int PublishEventId { get; set; }

        [Required]
        public required string EventTitle { get; set; }

        [Required]
        public required string EventCategory { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        public required string EventTime { get; set; }

        [Required]
        public required string EventLocation { get; set; }

        [Required]
        public required string TicketType { get; set; } // Free, Standard, VIP, Early Bird

        public string? SpeakerName { get; set; }

        public string? ExternalUrl { get; set; }

        public string? EventImageUrl { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation: Users who enrolled
        public virtual ICollection<EventEnrollment> Enrollments { get; set; } = new List<EventEnrollment>();
    }
}
