using System.Security.Cryptography;
using System.Text;

namespace RegistrationPortal.Services
{
    public interface IPasswordService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
        bool IsValidPassword(string password);
        int CalculatePasswordStrength(string password);
        string GenerateRandomPassword(int length = 12, bool includeSpecialChars = true);
        bool ContainsWeakPatterns(string password);
    }

    public class PasswordService : IPasswordService
    {
        private const int MinPasswordLength = 8;
        private const int MaxPasswordLength = 128;
        private const string SpecialCharacters = "@$!%*?&";

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            try
            {
                byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(password));
                StringBuilder sb = new();
                foreach (byte b in hash)
                {
                    sb.AppendFormat("{0:x2}", b);
                }
                return sb.ToString().ToUpper();
            }
            catch (Exception ex)
            {
                throw new Exception("Password hashing failed: " + ex.Message, ex);
            }
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                string hashOfInput = HashPassword(password);
                return StringComparer.OrdinalIgnoreCase.Compare(hashOfInput, hashedPassword) == 0;
            }
            catch
            {
                return false;
            }
        }

        public bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (password.Length < MinPasswordLength || password.Length > MaxPasswordLength)
                return false;

            bool hasUpper = false, hasLower = false, hasNumber = false, hasSpecial = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasNumber = true;
                else if (SpecialCharacters.Contains(c)) hasSpecial = true;
            }

            return hasUpper && hasLower && hasNumber && hasSpecial;
        }

        public int CalculatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return 0;

            int strength = 0;

            // Length scoring (up to 25%)
            if (password.Length >= 8) strength += 5;
            if (password.Length >= 10) strength += 5;
            if (password.Length >= 12) strength += 5;
            if (password.Length >= 15) strength += 5;
            if (password.Length >= 20) strength += 5;

            // Character type scoring (75% total)
            bool hasUpper = false, hasLower = false, hasNumber = false, hasSpecial = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasNumber = true;
                else if (SpecialCharacters.Contains(c)) hasSpecial = true;
            }

            if (hasUpper) strength += 18;
            if (hasLower) strength += 19;
            if (hasNumber) strength += 19;
            if (hasSpecial) strength += 19;

            return Math.Min(strength, 100);
        }

        public string GenerateRandomPassword(int length = 12, bool includeSpecialChars = true)
        {
            if (length < MinPasswordLength)
                length = MinPasswordLength;

            const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
            const string numberChars = "0123456789";
            
            string validChars = upperChars + lowerChars + numberChars;
            if (includeSpecialChars)
                validChars += SpecialCharacters;

            StringBuilder result = new();
            Random rng = new();
            
            // Ensure at least one character from each required category
            result.Append(upperChars[rng.Next(upperChars.Length)]);
            result.Append(lowerChars[rng.Next(lowerChars.Length)]);
            result.Append(numberChars[rng.Next(numberChars.Length)]);
            
            if (includeSpecialChars)
                result.Append(SpecialCharacters[rng.Next(SpecialCharacters.Length)]);

            // Fill remaining length with random characters
            int remainingLength = length - result.Length;
            for (int i = 0; i < remainingLength; i++)
            {
                result.Append(validChars[rng.Next(validChars.Length)]);
            }

            return ShuffleString(result.ToString());
        }

        public bool ContainsWeakPatterns(string password)
        {
            if (string.IsNullOrEmpty(password))
                return true;

            string lowerPassword = password.ToLower();

            string[] weakPatterns =
            [
                "password", "123456", "qwerty", "abc123", "admin", "letmein",
                "welcome", "monkey", "dragon", "master", "shadow", "12345",
                "111111", "123123", "654321"
            ];

            return weakPatterns.Any(lowerPassword.Contains);
        }

        private static string ShuffleString(string input)
        {
            char[] array = input.ToCharArray();
            Random rng = new();
            
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
            
            return new string(array);
        }
    }
}