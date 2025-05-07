using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectThruEventsBackend.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        public required string Name { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public required string Location { get; set; }

        public int PendingTasks { get; set; }
        public int AttendeesCount { get; set; }
        public double Rating { get; set; }
        public double FeedbackScore { get; set; }
        public decimal BudgetUtilization { get; set; }

        // Relationship with EventManager
        [ForeignKey("EventManagerId")] // Foreign key reference to the EventManagerId
        public Guid EventManagerId { get; set; }  // Change to Guid to match the EventManager's primary key type
        public EventManager? EventManager { get; set; }
    }
}
