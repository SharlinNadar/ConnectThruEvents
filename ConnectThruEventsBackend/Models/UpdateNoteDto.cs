using System.ComponentModel.DataAnnotations;

public class UpdateNotesDto
{
    [Required]
    public required string Notes { get; set; }
}
