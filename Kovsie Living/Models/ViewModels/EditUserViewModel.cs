using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "First Name is required")]
        [DisplayName("First Name")]
        [StringLength(50, ErrorMessage = "First Name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required")]
        [DisplayName("Last Name")]
        [StringLength(50, ErrorMessage = "Last Name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student/Staff Number is required")]
        [DisplayName("Student/Staff Number")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Only numbers are allowed")]
        public string UserName { get; set; } = string.Empty;

        [DisplayName("Unit Number")]
        [StringLength(10, ErrorMessage = "Unit Number cannot exceed 10 characters")]
        public string UnitNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Number of Vehicles is required")]
        [DisplayName("Number of Vehicles")]
        [Range(0, 10, ErrorMessage = "Vehicle number must be between 0 and 10")]
        public int VehicleNumber { get; set; }

        public bool IsStudent { get; set; }

        [DisplayName("Profile Picture")]
        public IFormFile ProfilePictureFile { get; set; }

        public byte[] CurrentProfilePicture { get; set; }
        public string CurrentProfilePictureContentType { get; set; }
    }
}
