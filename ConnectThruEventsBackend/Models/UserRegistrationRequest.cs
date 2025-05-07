using System.ComponentModel.DataAnnotations;

namespace ConnectThruEventsBackend.Models
{
    public class UserRegistrationRequest
    {
        [Required(ErrorMessage = "Full Name is required")]
        public required string FullName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number must be 10 digits")]
        public required string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public required string Role { get; set; } // "participant" or "event_manager"

        // ✅ Fix Event Manager Details
        public EventManagerRequest? EventManager { get; set; }
    }

    public class EventManagerRequest
    {
        [Required(ErrorMessage = "Organization is required")]
        public required string Organization { get; set; }

        [Required(ErrorMessage = "Experience years is required")]
        public required int ExperienceYears { get; set; }

        public string? Certifications { get; set; }
    }
}
