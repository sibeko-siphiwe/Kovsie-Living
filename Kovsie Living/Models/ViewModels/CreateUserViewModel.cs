using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace Kovsie_Living.Models.ViewModels
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "First Name is required")]
        [DisplayName("First Name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "First Name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "First Name can only contain letters, spaces, hyphens and apostrophes")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last Name is required")]
        [DisplayName("Last Name")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Last Name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-']+$", ErrorMessage = "Last Name can only contain letters, spaces, hyphens and apostrophes")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student/Staff Number is required")]
        [DisplayName("Student/Staff Number")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Number must be between 3 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Only letters and numbers are allowed")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Temporary Password is required")]
        [DisplayName("Temporary Password")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$",
            ErrorMessage = "Password must have at least one uppercase, one lowercase, one number, and one special character (@$!%*?&)")]
        public string TemporaryPassword { get; set; } = GenerateTemporaryPassword();

        [Required(ErrorMessage = "Please select user type")]
        [DisplayName("I am a Student")]
        public bool IsStudent { get; set; } = true;

        public bool IsStaff => !IsStudent;
        public string Email => $"{UserName?.Trim()}@ufs.ac.za";

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