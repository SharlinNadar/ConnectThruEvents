using System;
using System.ComponentModel.DataAnnotations;

public class Admin
{
    [Key] // Marks Id as the primary key
    public Guid Id { get; set; }

    [Required]
    public required string FullName { get; set; }

    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required]
    public required string PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
