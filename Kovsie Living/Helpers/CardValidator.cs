namespace Kovsie_Living.Helpers
{
    public static class CardValidator
    {
        // Validates the credit card number using the Luhn Algorithm
        public static bool IsValidCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber)) return false;

            cardNumber = cardNumber.Replace(" ", "");
            if (cardNumber.Length != 16) return false;

            int sum = 0;
            bool alternate = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(cardNumber[i])) return false;

                int n = int.Parse(cardNumber[i].ToString());

                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n = (n % 10) + 1;
                }
                sum += n;
                alternate = !alternate;
            }
            return (sum % 10 == 0);
        }

        // Validates if the card is expired
        public static bool IsValidExpiryDate(string expiryDate)
        {
            if (string.IsNullOrWhiteSpace(expiryDate)) return false;

            var parts = expiryDate.Split('/');
            if (parts.Length != 2) return false;

            if (!int.TryParse(parts[0], out int month) || !int.TryParse(parts[1], out int year))
                return false;

            if (month < 1 || month > 12) return false;

            // Assuming YY format (e.g., 25 = 2025)
            int fullYear = 2000 + year;
            var expiry = new DateTime(fullYear, month, 1).AddMonths(1).AddDays(-1); // Last day of month

            return expiry >= DateTime.UtcNow.Date;
        }
    }
}