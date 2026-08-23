using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Kovsie_Living.Models.ViewModels
{
    public class ClientProfileViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [DisplayName("First Name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First Name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "First Name can only contain letters, spaces, hyphens and apostrophes")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [DisplayName("Last Name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last Name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Last Name can only contain letters, spaces, hyphens and apostrophes")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [DisplayName("Username")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Only letters and numbers are allowed")]
        public string UserName { get; set; }

        [DisplayName("Email")]
        public string Email { get; set; }

        [DisplayName("Phone Number")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone Number must be between 10 and 15 characters")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Phone number can only contain digits")]
        public string PhoneNumber { get; set; }

        public bool IsStudent { get; set; }
        public bool IsStaff { get; set; }

        [Required(ErrorMessage = "Unit Number is required")]
        [DisplayName("Unit Number")]
        [StringLength(20, MinimumLength = 1, ErrorMessage = "Unit Number must be between 1 and 20 characters")]
        public string UnitNumber { get; set; }

        [DisplayName("Number of Vehicles")]
        [Range(0, 10, ErrorMessage = "Vehicle number must be between 0 and 10")]
        public int? VehicleNumber { get; set; }

        public bool IsApproved { get; set; }
    }
}
