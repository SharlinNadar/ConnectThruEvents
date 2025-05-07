using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ConnectThruEventsBackend.Models
{
    public class EventManager
    {
        [Key]
        public Guid EventManagerId { get; set; }  // Changed to Guid

        [Required]
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required, MaxLength(100)]
        public string Organization { get; set; } = string.Empty;

        [Required, Range(0, 50)]
        public int ExperienceYears { get; set; }

        public string? Certifications { get; set; }

        [Range(0, int.MaxValue)]
        public int EventsCompleted { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PricePerEvent { get; set; }

        // Relationships with Event and TaskItem models
        public List<Event> Events { get; set; } = new List<Event>();
        public ICollection<TaskItem> Tasks { get; set; }

        // Constructor to initialize the Tasks property
        public EventManager()
        {
            Tasks = new List<TaskItem>();
        }
    }
}
