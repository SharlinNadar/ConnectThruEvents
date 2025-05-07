using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectThruEventsBackend.Models
{
    public class FavoriteEvent
    {
        [Key]
        public int FavoriteEventId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int PublishEventId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("PublishEventId")]
        public virtual PublishEvent? PublishEvent { get; set; }
    }
}
