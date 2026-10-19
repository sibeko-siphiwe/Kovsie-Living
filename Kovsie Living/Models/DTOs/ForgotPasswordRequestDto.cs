using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models.DTOs
{
    public class ForgotPasswordRequestDto
    {
        [Required(ErrorMessage = "Student/Staff number is required")]
        public string UserName { get; set; }
    }
}