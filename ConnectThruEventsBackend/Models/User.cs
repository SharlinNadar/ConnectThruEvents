using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ConnectThruEventsBackend.Models
{
   public class User
{
    [Key]
    public int UserId { get; set; }

    [Required, MaxLength(100)]
    public required string FullName { get; set; }

    [Required, EmailAddress, MaxLength(255)]
    public required string Email { get; set; }

    [Required, MaxLength(15)]
    public required string PhoneNumber { get; set; }

    [Required]
    public required string PasswordHash { get; set; }

    [Required]
    public required string Role { get; set; } // "participant" or "event_manager"

    public string? ProfilePicture { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    public string? SocialLinks { get; set; }

    // Explicit Foreign Key
    public int? EventManagerId { get; set; }

    // Navigation Property
    public virtual EventManager? EventManager { get; set; }

    public void SetSocialLinks(Dictionary<string, string>? links)
    {
        SocialLinks = links != null ? JsonSerializer.Serialize(links) : null;
    }

    public Dictionary<string, string>? GetSocialLinks()
    {
        return !string.IsNullOrEmpty(SocialLinks)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(SocialLinks)
            : null;
    }
}


}
