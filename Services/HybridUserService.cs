using RegistrationPortal.Models;
using RegistrationPortal.ViewModels;

namespace RegistrationPortal.Services
{
    public class HybridUserService : IUserService
    {
        private readonly IUserService _directUserService;
        private readonly IUserApiClientService _apiClientService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HybridUserService> _logger;

        public HybridUserService(
            UserService directUserService,
            IUserApiClientService apiClientService,
            IConfiguration configuration,
            ILogger<HybridUserService> logger)
        {
            _directUserService = directUserService;
            _apiClientService = apiClientService;
            _configuration = configuration;
            _logger = logger;
        }

        private bool UseApiMode => _configuration.GetValue<bool>("UseApiMode", false);

        // Store original password temporarily for API calls
        private static readonly Dictionary<string, string> _tempPasswordStorage = new Dictionary<string, string>();

        public async Task<int> CreateUserAsync(User user)
        {
            try
            {
                if (UseApiMode)
                {
                    _logger.LogInformation("Creating user via API: {Username}", user.Username);
                    // For API mode, we need the plain password.
                    // This is a workaround - the password should be passed separately
                    var password = _tempPasswordStorage.ContainsKey(user.Username)
                        ? _tempPasswordStorage[user.Username]
                        : "TempPassword@123"; // Fallback - should not happen in normal flow

                    var result = await _apiClientService.CreateUserAsync(user, password);

                    // Clean up temporary storage
                    if (_tempPasswordStorage.ContainsKey(user.Username))
                        _tempPasswordStorage.Remove(user.Username);

                    return result;
                }
                else
                {
                    _logger.LogInformation("Creating user via direct DB: {Username}", user.Username);
                    return await _directUserService.CreateUserAsync(user);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateUserAsync for user: {Username}", user.Username);
                // Clean up temporary storage on error
                if (_tempPasswordStorage.ContainsKey(user.Username))
                    _tempPasswordStorage.Remove(user.Username);

                // Fallback to direct service if API fails
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.CreateUserAsync(user);
                }
                throw;
            }
        }

        // Add method to store password temporarily for API mode
        public void StoreTemporaryPassword(string username, string password)
        {
            _tempPasswordStorage[username] = password;
        }

        public async Task<User?> AuthenticateUserAsync(string usernameOrEmail, string password)
        {
            try
            {
                if (UseApiMode)
                {
                    _logger.LogInformation("Authenticating user via API: {UsernameOrEmail}", usernameOrEmail);
                    return await _apiClientService.AuthenticateUserAsync(usernameOrEmail, password);
                }
                else
                {
                    _logger.LogInformation("Authenticating user via direct DB: {UsernameOrEmail}", usernameOrEmail);
                    return await _directUserService.AuthenticateUserAsync(usernameOrEmail, password);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AuthenticateUserAsync for user: {UsernameOrEmail}", usernameOrEmail);
                // Fallback to direct service if API fails
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.AuthenticateUserAsync(usernameOrEmail, password);
                }
                throw;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.GetUserByIdAsync(userId, false);
                }
                else
                {
                    return await _directUserService.GetUserByIdAsync(userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserByIdAsync for user: {UserId}", userId);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.GetUserByIdAsync(userId);
                }
                throw;
            }
        }

        public async Task<User?> GetUserByIdForAdminAsync(int userId)
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.GetUserByIdAsync(userId, true);
                }
                else
                {
                    return await _directUserService.GetUserByIdForAdminAsync(userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserByIdForAdminAsync for user: {UserId}", userId);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.GetUserByIdForAdminAsync(userId);
                }
                throw;
            }
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            try
            {
                if (UseApiMode)
                {
                    var users = await _apiClientService.GetAllUsersAsync(true, username);
                    return users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    return await _directUserService.GetUserByUsernameAsync(username);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserByUsernameAsync for username: {Username}", username);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.GetUserByUsernameAsync(username);
                }
                throw;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            try
            {
                if (UseApiMode)
                {
                    var users = await _apiClientService.GetAllUsersAsync(true, email);
                    return users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    return await _directUserService.GetUserByEmailAsync(email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserByEmailAsync for email: {Email}", email);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.GetUserByEmailAsync(email);
                }
                throw;
            }
        }

        public async Task<User?> GetUserByPhoneAsync(string phoneNumber)
        {
            try
            {
                if (UseApiMode)
                {
                    var users = await _apiClientService.GetAllUsersAsync(true, phoneNumber);
                    return users.FirstOrDefault(u => u.PhoneNumber.Equals(phoneNumber, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    return await _directUserService.GetUserByPhoneAsync(phoneNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUserByPhoneAsync for phone: {PhoneNumber}", phoneNumber);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.GetUserByPhoneAsync(phoneNumber);
                }
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.UpdateUserAsync(user);
                }
                else
                {
                    return await _directUserService.UpdateUserAsync(user);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateUserAsync for user: {UserId}", user.UserID);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.UpdateUserAsync(user);
                }
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.DeleteUserAsync(userId);
                }
                else
                {
                    return await _directUserService.DeleteUserAsync(userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteUserAsync for user: {UserId}", userId);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.DeleteUserAsync(userId);
                }
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.GetAllUsersAsync(true);
                }
                else
                {
                    return await _directUserService.GetAllUsersAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllUsersAsync");
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.GetAllUsersAsync();
                }
                throw;
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(bool? isActive = null)
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.GetAllUsersAsync(isActive);
                }
                else
                {
                    return await _directUserService.GetAllUsersAsync(isActive);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllUsersAsync with isActive: {IsActive}", isActive);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.GetAllUsersAsync(isActive);
                }
                throw;
            }
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.GetAllUsersAsync(true, searchTerm);
                }
                else
                {
                    return await _directUserService.SearchUsersAsync(searchTerm);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SearchUsersAsync with term: {SearchTerm}", searchTerm);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.SearchUsersAsync(searchTerm);
                }
                throw;
            }
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.IsUsernameAvailableAsync(username);
                }
                else
                {
                    return await _directUserService.IsUsernameAvailableAsync(username);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsUsernameAvailableAsync for username: {Username}", username);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.IsUsernameAvailableAsync(username);
                }
                throw;
            }
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.IsEmailAvailableAsync(email);
                }
                else
                {
                    return await _directUserService.IsEmailAvailableAsync(email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsEmailAvailableAsync for email: {Email}", email);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.IsEmailAvailableAsync(email);
                }
                throw;
            }
        }

        public async Task<bool> IsPhoneAvailableAsync(string phoneNumber)
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.IsPhoneAvailableAsync(phoneNumber);
                }
                else
                {
                    return await _directUserService.IsPhoneAvailableAsync(phoneNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsPhoneAvailableAsync for phone: {PhoneNumber}", phoneNumber);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.IsPhoneAvailableAsync(phoneNumber);
                }
                throw;
            }
        }

        public async Task<bool> IsUsernameAvailableForEditAsync(string username, int currentUserId)
        {
            // For API mode, we'll use a workaround since the API doesn't have this specific method
            try
            {
                if (UseApiMode)
                {
                    var users = await _apiClientService.GetAllUsersAsync(null, username);
                    var conflictingUser = users.FirstOrDefault(u =>
                        u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                        u.UserID != currentUserId);
                    return conflictingUser == null;
                }
                else
                {
                    return await _directUserService.IsUsernameAvailableForEditAsync(username, currentUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsUsernameAvailableForEditAsync for username: {Username}, userId: {UserId}", username, currentUserId);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.IsUsernameAvailableForEditAsync(username, currentUserId);
                }
                throw;
            }
        }

        public async Task<bool> IsEmailAvailableForEditAsync(string email, int currentUserId)
        {
            try
            {
                if (UseApiMode)
                {
                    var users = await _apiClientService.GetAllUsersAsync(null, email);
                    var conflictingUser = users.FirstOrDefault(u =>
                        u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                        u.UserID != currentUserId);
                    return conflictingUser == null;
                }
                else
                {
                    return await _directUserService.IsEmailAvailableForEditAsync(email, currentUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsEmailAvailableForEditAsync for email: {Email}, userId: {UserId}", email, currentUserId);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.IsEmailAvailableForEditAsync(email, currentUserId);
                }
                throw;
            }
        }

        public async Task<bool> IsPhoneAvailableForEditAsync(string phoneNumber, int currentUserId)
        {
            try
            {
                if (UseApiMode)
                {
                    var users = await _apiClientService.GetAllUsersAsync(null, phoneNumber);
                    var conflictingUser = users.FirstOrDefault(u =>
                        u.PhoneNumber.Equals(phoneNumber, StringComparison.OrdinalIgnoreCase) &&
                        u.UserID != currentUserId);
                    return conflictingUser == null;
                }
                else
                {
                    return await _directUserService.IsPhoneAvailableForEditAsync(phoneNumber, currentUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsPhoneAvailableForEditAsync for phone: {PhoneNumber}, userId: {UserId}", phoneNumber, currentUserId);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.IsPhoneAvailableForEditAsync(phoneNumber, currentUserId);
                }
                throw;
            }
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.GetDashboardStatsAsync();
                }
                else
                {
                    return await _directUserService.GetDashboardStatsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDashboardStatsAsync");
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.GetDashboardStatsAsync();
                }
                throw;
            }
        }

        public async Task<List<User>> GetUnapprovedUsersAsync()
        {
            try
            {
                if (UseApiMode)
                {
                    var users = await _apiClientService.GetUnapprovedUsersAsync();
                    return users.ToList();
                }
                else
                {
                    return await _directUserService.GetUnapprovedUsersAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUnapprovedUsersAsync");
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    return await _directUserService.GetUnapprovedUsersAsync();
                }
                throw;
            }
        }

        // Forgot Password Methods
        public async Task<(bool Success, string? SecurityQuestion, int? UserId, string? Message)> GetSecurityQuestionAsync(string usernameOrEmail)
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.GetSecurityQuestionAsync(usernameOrEmail);
                }
                else
                {
                    // Direct DB implementation
                    var user = await _directUserService.GetUserByUsernameAsync(usernameOrEmail) ??
                               await _directUserService.GetUserByEmailAsync(usernameOrEmail);

                    if (user == null)
                        return (false, null, null, "No account found with this username or email");

                    return (true, user.SecurityQuestion, user.UserID, "Security question retrieved");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetSecurityQuestionAsync for: {UsernameOrEmail}", usernameOrEmail);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    var user = await _directUserService.GetUserByUsernameAsync(usernameOrEmail) ??
                               await _directUserService.GetUserByEmailAsync(usernameOrEmail);

                    if (user == null)
                        return (false, null, null, "No account found with this username or email");

                    return (true, user.SecurityQuestion, user.UserID, "Security question retrieved");
                }
                throw;
            }
        }

        public async Task<(bool Success, string? ResetToken, string? Message)> VerifySecurityAnswerAsync(int userId, string securityAnswer)
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.VerifySecurityAnswerAsync(userId, securityAnswer);
                }
                else
                {
                    // Direct DB implementation
                    var user = await _directUserService.GetUserByIdAsync(userId);
                    if (user == null)
                        return (false, null, "User not found");

                    if (string.Equals(user.SecurityAnswer.Trim(), securityAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        var resetToken = Guid.NewGuid().ToString();
                        // Store in temporary cache (same as API mode)
                        _tempPasswordStorage[resetToken] = $"RESET_{userId}";
                        return (true, resetToken, "Security answer verified");
                    }

                    return (false, null, "Incorrect security answer");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in VerifySecurityAnswerAsync for user: {UserId}", userId);
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    var user = await _directUserService.GetUserByIdAsync(userId);
                    if (user == null)
                        return (false, null, "User not found");

                    if (string.Equals(user.SecurityAnswer.Trim(), securityAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        var resetToken = Guid.NewGuid().ToString();
                        _tempPasswordStorage[resetToken] = $"RESET_{userId}";
                        return (true, resetToken, "Security answer verified");
                    }

                    return (false, null, "Incorrect security answer");
                }
                throw;
            }
        }

        public async Task<(bool Success, string? Message)> ResetPasswordAsync(string resetToken, string newPassword)
        {
            try
            {
                if (UseApiMode)
                {
                    return await _apiClientService.ResetPasswordAsync(resetToken, newPassword);
                }
                else
                {
                    // Direct DB implementation
                    if (!_tempPasswordStorage.TryGetValue(resetToken, out var tokenValue) || !tokenValue.StartsWith("RESET_"))
                        return (false, "Invalid or expired reset token");

                    if (!int.TryParse(tokenValue.Replace("RESET_", ""), out int userId))
                        return (false, "Invalid reset token");

                    var user = await _directUserService.GetUserByIdAsync(userId);
                    if (user == null)
                        return (false, "User not found");

                    // In direct mode, password reset should be handled by the direct service
                    // Since we don't have access to IPasswordService in this context,
                    // we'll return an error and let the controller handle the fallback
                    return (false, "Password reset not available in direct mode - use API mode");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ResetPasswordAsync with token: {ResetToken}", resetToken?.Substring(0, 8) + "...");
                if (UseApiMode)
                {
                    _logger.LogWarning("API call failed, falling back to direct DB access");
                    // Direct fallback implementation would go here
                    return (false, "An error occurred while resetting password");
                }
                throw;
            }
        }
    }
}