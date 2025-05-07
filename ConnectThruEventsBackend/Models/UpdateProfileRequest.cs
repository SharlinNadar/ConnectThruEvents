using System.ComponentModel.DataAnnotations;

namespace ConnectThruEventsBackend.Models
{
    public class UpdateProfileRequest
    {
        [MaxLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string? FullName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string? PhoneNumber { get; set; }

        [MaxLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
        public string? Bio { get; set; }

        [MaxLength(100, ErrorMessage = "Location cannot exceed 100 characters.")]
        public string? Location { get; set; }

        // Dictionary to store social links, initialized directly
        public Dictionary<string, string> SocialLinks { get; set; } = new Dictionary<string, string>
        {
            { "LinkedIn", "" },
            { "Twitter", "" }
        };

        public IFormFile? ProfilePicture { get; set; }
    }
}
