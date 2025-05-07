using System;
using System.ComponentModel.DataAnnotations;

namespace ConnectThruEventsBackend.Models
{
    public class CreatedEvent
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public int UserId { get; set; }

        // Nullable User property to be assigned later
        public virtual User? User { get; set; }

        [Required]
        public Guid EventManagerId { get; set; }

        // Nullable EventManager property, can be assigned later
        public virtual EventManager? EventManager { get; set; }

        // Event Title
        [Required, MaxLength(100)]
        public string EventTitle { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Budget { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal ServiceChargePercentage { get; set; }

        public decimal ServiceCharge { get; set; }

        public decimal TotalPrice { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Constructor to initialize the properties
        public CreatedEvent(int userId, string eventTitle = "")
        {
            UserId = userId;  // The ID of the User who is creating the event
            EventTitle = eventTitle;  // Event Title provided by the user
            Status = "Pending";  // Default status
            CreatedAt = DateTime.UtcNow;  // Set the created timestamp
        }

        // Method to calculate service charge and total price
        public void CalculateServiceChargeAndTotalPrice()
        {
             this.ServiceCharge = (this.Budget * this.ServiceChargePercentage) / 100;
             this.TotalPrice = this.Budget + this.ServiceCharge;
             }


        // Optional: You can also add a method to set the service charge percentage based on the budget range
        public void SetServiceChargePercentage()
        {
            if (Budget < 10000)
            {
                ServiceChargePercentage = 5;  // 5% for budgets under 10,000
            }
            else if (Budget >= 10000 && Budget < 50000)
            {
                ServiceChargePercentage = 3;  // 3% for budgets between 10,000 and 50,000
            }
            else if (Budget >= 50000)
            {
                ServiceChargePercentage = 2;  // 2% for budgets over 50,000
            }
            else
            {
                ServiceChargePercentage = 0;  // 0% if no budget is provided or it's invalid
            }

            // After setting the service charge percentage, we should recalculate the service charge and total price
            CalculateServiceChargeAndTotalPrice();
        }
    }
}
