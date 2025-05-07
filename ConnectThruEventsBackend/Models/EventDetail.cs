using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectThruEventsBackend.Models
{
    public class EventDetail
    {
        [Key]
        public int EventDetailId { get; set; } // Unique ID for each event

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty; // Event title

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty; // Event location

        public DateTime Date { get; set; } // Event date

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty; // Event description

        public int Rating { get; set; } = 0; // Event rating (1-5)

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Upcoming"; // Event status (e.g., "Upcoming", "Completed")

        // New: Optional field to store event notes
        [MaxLength(5000)]
        public string? Notes { get; set; }

        // Foreign key property to relate to a specific Event Manager
        public Guid EventManagerId { get; set; }

        // Navigation property to EventManager
        [ForeignKey("EventManagerId")]
        public EventManager? EventManager { get; set; }

        // Navigation property to EmployeeAssignments
        public ICollection<EmployeeAssignment> EmployeeAssignments { get; set; } = new List<EmployeeAssignment>();

        // Navigation property to TaskAssignments
        public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
    }
}
