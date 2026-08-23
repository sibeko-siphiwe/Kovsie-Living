using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace Kovsie_Living.Models.ViewModels
{
    public class CreateUserViewModel
    {
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
        public int VehicleNumber { get; set; } = 0;

        [Required(ErrorMessage = "Please select user type")]
        [DisplayName("I am a Student")]
        public bool IsStudent { get; set; } = true;

        [Required(ErrorMessage = "Temporary Password is required")]
        [DisplayName("Temporary Password")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        public string TemporaryPassword { get; set; } = GenerateTemporaryPassword();

        public static string GenerateTemporaryPassword()
        {
            const string uppercase = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string specials = "!@#$%^&*";

            char[] password = new char[8];

            password[0] = uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)];
            password[1] = lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)];
            password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            password[3] = specials[RandomNumberGenerator.GetInt32(specials.Length)];

            string allChars = uppercase + lowercase + digits + specials;
            for (int i = 4; i < 8; i++)
            {
                password[i] = allChars[RandomNumberGenerator.GetInt32(allChars.Length)];
            }

            return new string(password.OrderBy(_ => RandomNumberGenerator.GetInt32(100)).ToArray());
        }
    }
} 