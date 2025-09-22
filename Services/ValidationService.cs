using System.Net.Mail;
using System.Text.RegularExpressions;

namespace RegistrationPortal.Services
{
    public interface IValidationService
    {
        bool IsValidEmail(string email);
        bool IsValidPhoneNumber(string phoneNumber);
        string FormatPhoneNumber(string phoneNumber);
        bool IsValidZipCode(string zipCode);
        bool IsValidUsername(string username);
        bool IsValidName(string name);
        bool IsValidAge(DateTime dateOfBirth, int minimumAge = 18);
        int CalculateAge(DateTime dateOfBirth);
        string SanitizeInput(string input);
        bool IsSafeFromSQLInjection(string input);
        ValidationResult ValidateRequired(string value, string fieldName);
        ValidationResult ValidateLength(string value, int minLength, int maxLength, string fieldName);
        ValidationResult ValidateRange(int value, int min, int max, string fieldName);
    }

    public class ValidationService : IValidationService
    {
        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email.Trim();
            }
            catch
            {
                return false;
            }
        }

        public bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            string cleanNumber = Regex.Replace(phoneNumber, @"[^\d]", "");
            return cleanNumber.Length == 10;
        }

        public string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            string cleanNumber = Regex.Replace(phoneNumber, @"[^\d]", "");
            
            if (cleanNumber.Length == 10)
            {
                return string.Format("({0}) {1}-{2}", 
                    cleanNumber[..3],
                    cleanNumber.Substring(3, 3),
                    cleanNumber.Substring(6, 4));
            }
            
            return phoneNumber;
        }

        public bool IsValidZipCode(string zipCode)
        {
            if (string.IsNullOrWhiteSpace(zipCode))
                return false;

            // US ZIP code format
            return Regex.IsMatch(zipCode.Trim(), @"^\d{5}(-\d{4})?$");
        }

        public bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return Regex.IsMatch(username.Trim(), @"^[a-zA-Z][a-zA-Z0-9_]{2,29}$");
        }

        public bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return Regex.IsMatch(name.Trim(), @"^[a-zA-Z\s]+$");
        }

        public bool IsValidAge(DateTime dateOfBirth, int minimumAge = 18)
        {
            int age = CalculateAge(dateOfBirth);
            return age >= minimumAge;
        }

        public int CalculateAge(DateTime dateOfBirth)
        {
            int age = DateTime.Now.Year - dateOfBirth.Year;
            if (DateTime.Now < dateOfBirth.AddYears(age)) age--;
            return age;
        }

        public string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            input = input.Replace("<", "&lt;")
                        .Replace(">", "&gt;")
                        .Replace("\"", "&quot;")
                        .Replace("'", "&#x27;")
                        .Replace("/", "&#x2F;")
                        .Replace("\\", "&#x5C;");

            return input.Trim();
        }

        public bool IsSafeFromSQLInjection(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            string[] sqlKeywords =
            [
                "select", "insert", "update", "delete", "drop", "create", "alter",
                "exec", "execute", "sp_", "xp_", "union", "and", "or", "where",
                "script", "javascript", "vbscript", "onload", "onerror"
            ];

            string lowerInput = input.ToLower();
            if (sqlKeywords.Any(lowerInput.Contains))
                return false;

            char[] dangerousChars = { '\'', '"', ';', '-', '(', ')', '[', ']', '{', '}' };
            return !dangerousChars.Any(input.Contains);
        }

        public ValidationResult ValidateRequired(string value, string fieldName)
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrWhiteSpace(value))
            {
                result.IsValid = false;
                result.ErrorMessage = $"{fieldName} is required";
            }
            else
            {
                result.IsValid = true;
            }

            return result;
        }

        public ValidationResult ValidateLength(string value, int minLength, int maxLength, string fieldName)
        {
            var result = new ValidationResult();
            
            if (string.IsNullOrEmpty(value))
            {
                result.IsValid = minLength == 0;
                result.ErrorMessage = result.IsValid ? string.Empty : $"{fieldName} is required";
                return result;
            }

            if (value.Length < minLength)
            {
                result.IsValid = false;
                result.ErrorMessage = $"{fieldName} must be at least {minLength} characters long";
            }
            else if (value.Length > maxLength)
            {
                result.IsValid = false;
                result.ErrorMessage = $"{fieldName} must not exceed {maxLength} characters";
            }
            else
            {
                result.IsValid = true;
            }

            return result;
        }

        public ValidationResult ValidateRange(int value, int min, int max, string fieldName)
        {
            var result = new ValidationResult();
            
            if (value < min || value > max)
            {
                result.IsValid = false;
                result.ErrorMessage = $"{fieldName} must be between {min} and {max}";
            }
            else
            {
                result.IsValid = true;
            }

            return result;
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}