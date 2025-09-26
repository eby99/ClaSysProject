using RegistrationPortal.Models;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace RegistrationPortal.Services
{
    public interface IValidationService
    {
        // Existing methods
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

        // New comprehensive validation methods
        ComprehensiveValidationResult ValidateUser(User user, bool isUpdate = false);
        ComprehensiveValidationResult ValidateUserForUpdate(User user);
        ComprehensiveValidationResult ValidatePasswordRequirements(string password);
        ComprehensiveValidationResult ValidateUniqueFields(User user, bool isUpdate = false);
    }

    public class ValidationService : IValidationService
    {
        private readonly IUserService _userService;
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(IUserService userService, ILogger<ValidationService> logger)
        {
            _userService = userService;
            _logger = logger;
        }
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

            // Registration page requires exactly 10 digits only
            return Regex.IsMatch(phoneNumber, @"^\d{10}$");
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

            // Registration page requires exactly 6 digits
            return Regex.IsMatch(zipCode, @"^\d{6}$");
        }

        public bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            // Registration page requires 3-30 characters, letters, numbers and underscore only
            return Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,30}$");
        }

        public bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            // Registration page requires only alphabets, no spaces for First/Last names
            return Regex.IsMatch(name, @"^[a-zA-Z]+$");
        }

        public bool IsValidAge(DateTime dateOfBirth, int minimumAge = 18)
        {
            int age = CalculateAge(dateOfBirth);
            // Registration page requires between 18 and 80 years old
            return age >= 18 && age <= 80;
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

        // New comprehensive validation methods
        public ComprehensiveValidationResult ValidateUser(User user, bool isUpdate = false)
        {
            var result = new ComprehensiveValidationResult();

            // Validate required fields
            result.Merge(ValidateRequiredFields(user));

            // Validate field formats and patterns
            result.Merge(ValidateFieldFormats(user));

            // Validate business rules
            result.Merge(ValidateBusinessRules(user));

            // Validate unique constraints (only if format validation passes)
            if (result.IsValid)
            {
                result.Merge(ValidateUniqueFields(user, isUpdate));
            }

            return result;
        }

        public ComprehensiveValidationResult ValidateUserForUpdate(User user)
        {
            return ValidateUser(user, isUpdate: true);
        }

        private ComprehensiveValidationResult ValidateRequiredFields(User user)
        {
            var result = new ComprehensiveValidationResult();

            if (string.IsNullOrWhiteSpace(user.FirstName))
                result.AddError("FirstName", "First name is required");

            if (string.IsNullOrWhiteSpace(user.LastName))
                result.AddError("LastName", "Last name is required");

            if (string.IsNullOrWhiteSpace(user.Username))
                result.AddError("Username", "Username is required");

            if (string.IsNullOrWhiteSpace(user.Email))
                result.AddError("Email", "Email address is required");

            if (string.IsNullOrWhiteSpace(user.Country))
                result.AddError("Country", "Country is required");

            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                result.AddError("PhoneNumber", "Phone number is required");

            if (string.IsNullOrWhiteSpace(user.SecurityQuestion))
                result.AddError("SecurityQuestion", "Security question is required");

            if (string.IsNullOrWhiteSpace(user.SecurityAnswer))
                result.AddError("SecurityAnswer", "Security answer is required");

            return result;
        }

        private ComprehensiveValidationResult ValidateFieldFormats(User user)
        {
            var result = new ComprehensiveValidationResult();

            // First Name - Only alphabets, no spaces
            if (!string.IsNullOrWhiteSpace(user.FirstName))
            {
                if (!Regex.IsMatch(user.FirstName, @"^[a-zA-Z]+$"))
                    result.AddError("FirstName", "First name can only contain alphabets (A-Z, a-z), no spaces allowed");

                if (user.FirstName.Length > 50)
                    result.AddError("FirstName", "First name cannot exceed 50 characters");
            }

            // Last Name - Only alphabets, no spaces
            if (!string.IsNullOrWhiteSpace(user.LastName))
            {
                if (!Regex.IsMatch(user.LastName, @"^[a-zA-Z]+$"))
                    result.AddError("LastName", "Last name can only contain alphabets (A-Z, a-z), no spaces allowed");

                if (user.LastName.Length > 50)
                    result.AddError("LastName", "Last name cannot exceed 50 characters");
            }

            // Username - 3-30 characters, letters, numbers and underscore only
            if (!string.IsNullOrWhiteSpace(user.Username))
            {
                if (!Regex.IsMatch(user.Username, @"^[a-zA-Z0-9_]{3,30}$"))
                    result.AddError("Username", "Username must be 3-30 characters long and contain only letters, numbers, and underscores");
            }

            // Email - Valid email format
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                if (!IsValidEmail(user.Email))
                    result.AddError("Email", "Please enter a valid email address");

                if (user.Email.Length > 100)
                    result.AddError("Email", "Email address cannot exceed 100 characters");
            }

            // Phone Number - Exactly 10 digits
            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                if (!Regex.IsMatch(user.PhoneNumber, @"^\d{10}$"))
                    result.AddError("PhoneNumber", "Phone number must be exactly 10 digits (no characters, spaces, or symbols)");
            }

            // City - Only alphabets and spaces (if provided)
            if (!string.IsNullOrWhiteSpace(user.City))
            {
                if (!Regex.IsMatch(user.City, @"^[a-zA-Z\s]+$"))
                    result.AddError("City", "City name can only contain alphabets and spaces");

                if (user.City.Length > 50)
                    result.AddError("City", "City name cannot exceed 50 characters");
            }

            // State - Only alphabets and spaces (if provided)
            if (!string.IsNullOrWhiteSpace(user.State))
            {
                if (!Regex.IsMatch(user.State, @"^[a-zA-Z\s]+$"))
                    result.AddError("State", "State name can only contain alphabets and spaces");

                if (user.State.Length > 50)
                    result.AddError("State", "State name cannot exceed 50 characters");
            }

            // ZIP Code - Exactly 6 digits (if provided)
            if (!string.IsNullOrWhiteSpace(user.ZipCode))
            {
                if (!Regex.IsMatch(user.ZipCode, @"^\d{6}$"))
                    result.AddError("ZipCode", "ZIP code must be exactly 6 digits");
            }

            // Gender validation (if provided)
            if (!string.IsNullOrWhiteSpace(user.Gender))
            {
                var validGenders = new[] { "Male", "Female", "Other", "PreferNot" };
                if (!validGenders.Contains(user.Gender))
                    result.AddError("Gender", "Invalid gender selection");
            }

            // Country validation
            if (!string.IsNullOrWhiteSpace(user.Country))
            {
                var validCountries = new[] { "US", "UK", "CA", "AU", "IN", "DE", "FR", "JP", "BR", "Other" };
                if (!validCountries.Contains(user.Country))
                    result.AddError("Country", "Invalid country selection");
            }

            // Security Question validation
            if (!string.IsNullOrWhiteSpace(user.SecurityQuestion))
            {
                var validQuestions = new[] { "pet", "school", "mother", "city", "friend" };
                if (!validQuestions.Contains(user.SecurityQuestion))
                    result.AddError("SecurityQuestion", "Invalid security question selection");
            }

            // Bio length check (if provided)
            if (!string.IsNullOrWhiteSpace(user.Bio) && user.Bio.Length > 500)
            {
                result.AddError("Bio", "Bio cannot exceed 500 characters");
            }

            // Street Address length check (if provided)
            if (!string.IsNullOrWhiteSpace(user.StreetAddress) && user.StreetAddress.Length > 200)
            {
                result.AddError("StreetAddress", "Street address cannot exceed 200 characters");
            }

            // Referral Code length check (if provided)
            if (!string.IsNullOrWhiteSpace(user.ReferralCode) && user.ReferralCode.Length > 20)
            {
                result.AddError("ReferralCode", "Referral code cannot exceed 20 characters");
            }

            return result;
        }

        private ComprehensiveValidationResult ValidateBusinessRules(User user)
        {
            var result = new ComprehensiveValidationResult();

            // Age validation - must be between 18 and 80
            var today = DateTime.Today;
            var age = today.Year - user.DateOfBirth.Year;

            // Adjust age if birthday hasn't occurred this year
            if (user.DateOfBirth.Date > today.AddYears(-age))
                age--;

            if (age < 18 || age > 80)
            {
                result.AddError("DateOfBirth", "You must be between 18 and 80 years old");
            }

            return result;
        }

        public ComprehensiveValidationResult ValidatePasswordRequirements(string password)
        {
            var result = new ComprehensiveValidationResult();

            if (string.IsNullOrWhiteSpace(password))
            {
                result.AddError("Password", "Password is required");
                return result;
            }

            var errors = new List<string>();

            // At least 8 characters
            if (password.Length < 8)
                errors.Add("Password must be at least 8 characters long");

            // At least one uppercase letter
            if (!Regex.IsMatch(password, @"[A-Z]"))
                errors.Add("Password must contain at least one uppercase letter");

            // At least one lowercase letter
            if (!Regex.IsMatch(password, @"[a-z]"))
                errors.Add("Password must contain at least one lowercase letter");

            // At least one number
            if (!Regex.IsMatch(password, @"\d"))
                errors.Add("Password must contain at least one number");

            // At least one special character
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?\"":{}|<>]"))
                errors.Add("Password must contain at least one special character");

            if (errors.Any())
            {
                result.AddError("Password", string.Join("; ", errors));
            }

            return result;
        }

        public ComprehensiveValidationResult ValidateUniqueFields(User user, bool isUpdate = false)
        {
            var result = new ComprehensiveValidationResult();

            try
            {
                // Check username uniqueness
                if (!string.IsNullOrWhiteSpace(user.Username))
                {
                    var existingUserByUsername = _userService.GetUserByUsernameAsync(user.Username).GetAwaiter().GetResult();
                    if (existingUserByUsername != null && (!isUpdate || existingUserByUsername.UserID != user.UserID))
                    {
                        result.AddError("Username", "This username is already taken");
                    }
                }

                // Check email uniqueness
                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    var existingUserByEmail = _userService.GetUserByEmailAsync(user.Email).GetAwaiter().GetResult();
                    if (existingUserByEmail != null && (!isUpdate || existingUserByEmail.UserID != user.UserID))
                    {
                        result.AddError("Email", "This email address is already registered");
                    }
                }

                // Check phone number uniqueness
                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    var existingUserByPhone = _userService.GetUserByPhoneAsync(user.PhoneNumber).GetAwaiter().GetResult();
                    if (existingUserByPhone != null && (!isUpdate || existingUserByPhone.UserID != user.UserID))
                    {
                        result.AddError("PhoneNumber", "This phone number is already registered");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating unique fields for user");
                result.AddError("Validation", "Unable to validate uniqueness at this time");
            }

            return result;
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class ComprehensiveValidationResult
    {
        public bool IsValid => !Errors.Any();
        public Dictionary<string, List<string>> Errors { get; } = new Dictionary<string, List<string>>();

        public void AddError(string field, string message)
        {
            if (!Errors.ContainsKey(field))
                Errors[field] = new List<string>();

            Errors[field].Add(message);
        }

        public void Merge(ComprehensiveValidationResult other)
        {
            foreach (var kvp in other.Errors)
            {
                foreach (var error in kvp.Value)
                {
                    AddError(kvp.Key, error);
                }
            }
        }

        public List<string> GetAllErrors()
        {
            return Errors.Values.SelectMany(x => x).ToList();
        }

        public string GetErrorsAsString()
        {
            return string.Join("; ", GetAllErrors());
        }

        public object ToApiResponse()
        {
            return new
            {
                IsValid = this.IsValid,
                Errors = this.Errors.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToArray()
                ),
                Message = this.IsValid ? "Validation successful" : "Validation failed"
            };
        }
    }
}