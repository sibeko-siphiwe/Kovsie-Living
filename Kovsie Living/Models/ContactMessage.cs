using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models
{
    public class ContactMessage
    {
        [Key]
        public int ContactMessageId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(150)]
        public string Subject { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string Message { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        // Optional: if logged in, link to the user
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}