using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models.ViewModels
{
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Please enter your name")]
        [DisplayName("Full Name")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email")]
        [DisplayName("Email Address")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [StringLength(100)]
        [RegularExpression(@"^[0-9]+@ufs4life\.ac\.za$", ErrorMessage = "Email must be in the format: {studentnumber}@ufs4life.ac.za")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a subject")]
        [DisplayName("Subject")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Subject must be between 3 and 150 characters")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your message")]
        [DisplayName("Message")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 2000 characters")]
        public string Message { get; set; } = string.Empty;
    }
}