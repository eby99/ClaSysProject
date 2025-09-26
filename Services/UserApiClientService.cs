using RegistrationPortal.Controllers.Api;
using RegistrationPortal.Models;
using RegistrationPortal.ViewModels;
using System.Text;
using System.Text.Json;

namespace RegistrationPortal.Services
{
    public interface IUserApiClientService
    {
        Task<User?> AuthenticateUserAsync(string usernameOrEmail, string password);
        Task<User?> GetUserByIdAsync(int userId, bool includeInactive = false);
        Task<IEnumerable<User>> GetAllUsersAsync(bool? isActive = null, string? searchTerm = null);
        Task<int> CreateUserAsync(User user, string password);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);
        Task<DashboardStats> GetDashboardStatsAsync();
        Task<bool> IsUsernameAvailableAsync(string username);
        Task<bool> IsEmailAvailableAsync(string email);
        Task<bool> IsPhoneAvailableAsync(string phoneNumber);
        Task<(bool Success, string? SecurityQuestion, int? UserId, string? Message)> GetSecurityQuestionAsync(string usernameOrEmail);
        Task<(bool Success, string? ResetToken, string? Message)> VerifySecurityAnswerAsync(int userId, string securityAnswer);
        Task<(bool Success, string? Message)> ResetPasswordAsync(string resetToken, string newPassword);
        Task<IEnumerable<User>> GetUnapprovedUsersAsync();
    }

    public class UserApiClientService : IUserApiClientService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UserApiClientService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public UserApiClientService(HttpClient httpClient, ILogger<UserApiClientService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null, // Keep PascalCase
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<User?> AuthenticateUserAsync(string usernameOrEmail, string password)
        {
            try
            {
                var loginDto = new LoginDto
                {
                    UsernameOrEmail = usernameOrEmail,
                    Password = password
                };

                var json = JsonSerializer.Serialize(loginDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/UsersApi/authenticate", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var userDto = JsonSerializer.Deserialize<UserResponseDto>(responseContent, _jsonOptions);
                    return ConvertToUser(userDto);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return null; // Invalid credentials
                }

                _logger.LogWarning("Authentication failed with status code: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating user: {UsernameOrEmail}", usernameOrEmail);
                return null;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId, bool includeInactive = false)
        {
            try
            {
                var url = $"/api/UsersApi/{userId}";
                if (includeInactive)
                {
                    url += "?includeInactive=true";
                }

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var userDto = JsonSerializer.Deserialize<UserResponseDto>(responseContent, _jsonOptions);
                    return ConvertToUser(userDto);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                _logger.LogWarning("Get user by ID failed with status code: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(bool? isActive = null, string? searchTerm = null)
        {
            try
            {
                var url = "/api/UsersApi";
                var queryParams = new List<string>();

                if (isActive.HasValue)
                    queryParams.Add($"isActive={isActive.Value}");

                if (!string.IsNullOrWhiteSpace(searchTerm))
                    queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");

                if (queryParams.Any())
                    url += "?" + string.Join("&", queryParams);


                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var userDtos = JsonSerializer.Deserialize<IEnumerable<UserResponseDto>>(responseContent, _jsonOptions);
                    return userDtos?.Select(ConvertToUser) ?? new List<User>();
                }

                _logger.LogWarning("Get all users failed with status code: {StatusCode}", response.StatusCode);
                return new List<User>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return new List<User>();
            }
        }

        public async Task<int> CreateUserAsync(User user, string password)
        {
            try
            {
                var createUserDto = new CreateUserDto
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.Username,
                    Email = user.Email,
                    Password = password,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Country = user.Country,
                    PhoneNumber = user.PhoneNumber,
                    StreetAddress = user.StreetAddress,
                    City = user.City,
                    State = user.State,
                    ZipCode = user.ZipCode,
                    SecurityQuestion = user.SecurityQuestion,
                    SecurityAnswer = user.SecurityAnswer,
                    Bio = user.Bio,
                    ReferralCode = user.ReferralCode,
                    ReceiveNewsletter = user.ReceiveNewsletter,
                    ReceiveSMS = user.ReceiveSMS,
                    AcceptTerms = user.TermsAcceptedDate.HasValue
                };

                var json = JsonSerializer.Serialize(createUserDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/UsersApi", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var createdResponse = JsonSerializer.Deserialize<UserCreatedResponse>(responseContent, _jsonOptions);
                    return createdResponse?.UserID ?? -999;
                }

                // Handle specific error cases
                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, _jsonOptions);

                    if (errorResponse?.Message?.Contains("Username") == true)
                        return -1; // Username exists
                    if (errorResponse?.Message?.Contains("Email") == true)
                        return -2; // Email exists
                    if (errorResponse?.Message?.Contains("Phone") == true)
                        return -3; // Phone exists
                }

                _logger.LogWarning("Create user failed with status code: {StatusCode}", response.StatusCode);
                return -999;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", user.Username);
                return -999;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                // If SecurityQuestion or SecurityAnswer are empty, fetch existing values from the API
                var securityQuestion = user.SecurityQuestion;
                var securityAnswer = user.SecurityAnswer;

                if (string.IsNullOrEmpty(securityQuestion) || string.IsNullOrEmpty(securityAnswer))
                {
                    _logger.LogInformation("SecurityQuestion or SecurityAnswer is empty, fetching existing values for user: {UserId}", user.UserID);

                    // Get the existing user data to preserve security fields
                    var existingUsers = await GetAllUsersAsync(true);
                    var existingUser = existingUsers.FirstOrDefault(u => u.UserID == user.UserID);

                    if (existingUser != null)
                    {
                        securityQuestion = string.IsNullOrEmpty(securityQuestion) ? existingUser.SecurityQuestion : securityQuestion;
                        securityAnswer = string.IsNullOrEmpty(securityAnswer) ? existingUser.SecurityAnswer : securityAnswer;
                    }

                    // If still empty after checking existing user, provide default values to satisfy API requirements
                    if (string.IsNullOrEmpty(securityQuestion))
                        securityQuestion = "What is your favorite color?";
                    if (string.IsNullOrEmpty(securityAnswer))
                        securityAnswer = "Blue";
                }

                var updateUserDto = new UpdateUserDto
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.Username,
                    Email = user.Email,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Country = user.Country,
                    PhoneNumber = user.PhoneNumber,
                    StreetAddress = user.StreetAddress,
                    City = user.City,
                    State = user.State,
                    ZipCode = user.ZipCode,
                    SecurityQuestion = securityQuestion,
                    SecurityAnswer = securityAnswer,
                    Bio = user.Bio,
                    ReferralCode = user.ReferralCode,
                    ReceiveNewsletter = user.ReceiveNewsletter,
                    ReceiveSMS = user.ReceiveSMS,
                    IsActive = user.IsActive
                };

                var json = JsonSerializer.Serialize(updateUserDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Updating user {UserId} with SecurityQuestion: '{SecurityQuestion}'", user.UserID, securityQuestion);

                var response = await _httpClient.PutAsync($"/api/UsersApi/{user.UserID}", content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", user.UserID);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/UsersApi/{userId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                return false;
            }
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/UsersApi/dashboard-stats");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var stats = JsonSerializer.Deserialize<DashboardStats>(responseContent, _jsonOptions);
                    return stats ?? new DashboardStats();
                }

                _logger.LogWarning("Get dashboard stats failed with status code: {StatusCode}", response.StatusCode);
                return new DashboardStats();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return new DashboardStats();
            }
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            try
            {
                // Get all users and check for exact username match (case insensitive)
                var users = await GetAllUsersAsync();
                return !users.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking username availability: {Username}", username);
                return false; // Assume unavailable on error
            }
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            try
            {
                // Get all users and check for exact email match (case insensitive)
                var users = await GetAllUsersAsync();
                return !users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email availability: {Email}", email);
                return false; // Assume unavailable on error
            }
        }

        public async Task<bool> IsPhoneAvailableAsync(string phoneNumber)
        {
            try
            {
                // Get all users and check for exact phone number match
                var users = await GetAllUsersAsync();
                return !users.Any(u => !string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber.Equals(phoneNumber, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking phone availability: {PhoneNumber}", phoneNumber);
                return false; // Assume unavailable on error
            }
        }

        public async Task<(bool Success, string? SecurityQuestion, int? UserId, string? Message)> GetSecurityQuestionAsync(string usernameOrEmail)
        {
            try
            {
                var requestDto = new { UsernameOrEmail = usernameOrEmail };
                var json = JsonSerializer.Serialize(requestDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/UsersApi/forgot-password/security-question", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<SecurityQuestionResponse>(responseContent, _jsonOptions);
                    return (true, result?.SecurityQuestion, result?.UserId, "Security question retrieved");
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (false, null, null, "No account found with this username or email");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, _jsonOptions);
                return (false, null, null, errorResponse?.Message ?? "Failed to get security question");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting security question for: {UsernameOrEmail}", usernameOrEmail);
                return (false, null, null, "An error occurred while getting security question");
            }
        }

        public async Task<(bool Success, string? ResetToken, string? Message)> VerifySecurityAnswerAsync(int userId, string securityAnswer)
        {
            try
            {
                var requestDto = new { UserId = userId, SecurityAnswer = securityAnswer };
                var json = JsonSerializer.Serialize(requestDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/UsersApi/forgot-password/verify-answer", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<SecurityVerificationResponse>(responseContent, _jsonOptions);
                    return (true, result?.ResetToken, result?.Message ?? "Security answer verified");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, _jsonOptions);
                return (false, null, errorResponse?.Message ?? "Failed to verify security answer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying security answer for user: {UserId}", userId);
                return (false, null, "An error occurred while verifying security answer");
            }
        }

        public async Task<(bool Success, string? Message)> ResetPasswordAsync(string resetToken, string newPassword)
        {
            try
            {
                var requestDto = new { ResetToken = resetToken, NewPassword = newPassword };
                var json = JsonSerializer.Serialize(requestDto, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/UsersApi/forgot-password/reset", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<SuccessResponse>(responseContent, _jsonOptions);
                    return (true, result?.Message ?? "Password reset successfully");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, _jsonOptions);
                return (false, errorResponse?.Message ?? "Failed to reset password");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password with token: {ResetToken}", resetToken?.Substring(0, 8) + "...");
                return (false, "An error occurred while resetting password");
            }
        }

        public async Task<IEnumerable<User>> GetUnapprovedUsersAsync()
        {
            try
            {
                _logger.LogInformation("Getting unapproved users via API");
                var response = await _httpClient.GetAsync("/api/UsersApi/unapproved");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var dtos = JsonSerializer.Deserialize<List<UserResponseDto>>(json, _jsonOptions) ?? new List<UserResponseDto>();
                    return dtos.Select(ConvertToUser).Where(u => u != null);
                }
                else
                {
                    _logger.LogWarning("Failed to get unapproved users. Status: {StatusCode}", response.StatusCode);
                    return new List<User>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unapproved users via API");
                return new List<User>();
            }
        }

        private static User ConvertToUser(UserResponseDto? dto)
        {
            if (dto == null) return null!;

            return new User
            {
                UserID = dto.UserID,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Username = dto.Username,
                Email = dto.Email,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                Country = dto.Country,
                PhoneNumber = dto.PhoneNumber,
                StreetAddress = dto.StreetAddress,
                City = dto.City,
                State = dto.State,
                ZipCode = dto.ZipCode,
                Bio = dto.Bio,
                ReferralCode = dto.ReferralCode,
                ReceiveNewsletter = dto.ReceiveNewsletter,
                ReceiveSMS = dto.ReceiveSMS,
                TermsAcceptedDate = dto.TermsAcceptedDate,
                CreatedDate = dto.CreatedDate,
                LastModified = dto.LastModified,
                IsActive = dto.IsActive,
                IsApproved = dto.IsApproved
            };
        }
    }

    // Response DTOs for forgot password functionality
    public record SecurityQuestionResponse
    {
        public string SecurityQuestion { get; init; } = string.Empty;
        public int UserId { get; init; }
    }

    public record SecurityVerificationResponse
    {
        public string ResetToken { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    public record SuccessResponse
    {
        public string Message { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; }
    }

    public record ErrorResponse
    {
        public string Message { get; init; } = string.Empty;
        public IEnumerable<string>? Errors { get; init; }
        public DateTime Timestamp { get; init; }
    }
}