using System.Security.Cryptography;

namespace Kovsie_Living.Helpers
{
    public static class TemporaryPasswordGenerator
    {
        public static string Generate()
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

            string all = uppercase + lowercase + digits + specials;
            for (int i = 4; i < 8; i++)
            {
                password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            }

            return new string(password.OrderBy(_ => RandomNumberGenerator.GetInt32(100)).ToArray());
        }
    }
}