using Microsoft.EntityFrameworkCore;
using RegistrationPortal.Data;
using RegistrationPortal.Models;

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

        public AdminService(RegistrationDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<Admin?> AuthenticateAdminAsync(string username, string password)
        {
            try
            {
                string hashedPassword = _passwordService.HashPassword(password);

                var admin = await _context.Admins
                    .Where(a => a.Username == username 
                               && a.PasswordHash == hashedPassword 
                               && a.IsActive)
                    .FirstOrDefaultAsync();

                return admin;
            }
            catch
            {
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