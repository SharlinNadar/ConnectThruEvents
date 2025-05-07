using System.ComponentModel.DataAnnotations;

namespace ConnectThruEventsBackend.Models
{
    public class EventEnrollment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PublishEventId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual PublishEvent? PublishEvent { get; set; }
        public  virtual User? User { get; set; }
    }
}
