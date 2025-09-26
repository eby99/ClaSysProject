using Microsoft.EntityFrameworkCore;
using RegistrationPortal.Data;
using RegistrationPortal.Models;
using Microsoft.Extensions.Logging;

namespace RegistrationPortal.Services
{
    public interface IAdminService
    {
        Task<Admin?> AuthenticateAdminAsync(string username, string password);
        Task<Admin?> GetAdminByIdAsync(int adminId);
    }

    public class AdminService : IAdminService
    {
        private readonly RegistrationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<AdminService> _logger;

        public AdminService(RegistrationDbContext context, IPasswordService passwordService, ILogger<AdminService> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _logger = logger;
        }

        public async Task<Admin?> AuthenticateAdminAsync(string username, string password)
        {
            try
            {
                _logger.LogInformation("Attempting admin authentication for username: {Username}", username);

                var admin = await _context.Admins
                    .Where(a => a.Username == username && a.IsActive)
                    .FirstOrDefaultAsync();

                if (admin == null)
                {
                    _logger.LogWarning("No admin found with username: {Username}", username);
                    return null;
                }

                _logger.LogInformation("Admin found: ID={AdminId}, Username={Username}, IsActive={IsActive}", admin.AdminID, admin.Username, admin.IsActive);
                _logger.LogInformation("Stored password hash: {StoredHash}", admin.PasswordHash);

                bool passwordValid = _passwordService.VerifyPassword(password, admin.PasswordHash);
                _logger.LogInformation("Password verification result: {PasswordValid}", passwordValid);

                if (passwordValid)
                {
                    _logger.LogInformation("Authentication successful for admin: {Username}", username);
                    return admin;
                }

                _logger.LogWarning("Authentication failed for admin: {Username} - invalid password", username);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during admin authentication for username: {Username}", username);
                return null;
            }
        }

        public async Task<Admin?> GetAdminByIdAsync(int adminId)
        {
            return await _context.Admins
                .Where(a => a.AdminID == adminId && a.IsActive)
                .FirstOrDefaultAsync();
        }
    }
}