using Microsoft.EntityFrameworkCore;
using RegistrationPortal.Data;
using RegistrationPortal.Models;
using RegistrationPortal.ViewModels;

namespace RegistrationPortal.Services
{
    public interface IUserService
    {
        Task<int> CreateUserAsync(User user);
        Task<User?> AuthenticateUserAsync(string usernameOrEmail, string password);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByIdForAdminAsync(int userId);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByPhoneAsync(string phoneNumber);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int userId);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<IEnumerable<User>> GetAllUsersAsync(bool? isActive = null);
        Task<IEnumerable<User>> SearchUsersAsync(string searchTerm);
        Task<bool> IsUsernameAvailableAsync(string username);
        Task<bool> IsEmailAvailableAsync(string email);
        Task<bool> IsPhoneAvailableAsync(string phoneNumber);
        Task<bool> IsUsernameAvailableForEditAsync(string username, int currentUserId);
        Task<bool> IsEmailAvailableForEditAsync(string email, int currentUserId);
        Task<bool> IsPhoneAvailableForEditAsync(string phoneNumber, int currentUserId);
        Task<DashboardStats> GetDashboardStatsAsync();
        Task<List<User>> GetUnapprovedUsersAsync();
    }

    public class UserService : IUserService
    {
        private readonly RegistrationDbContext _context;
        private readonly IPasswordService _passwordService;

        public UserService(RegistrationDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<int> CreateUserAsync(User user)
        {
            try
            {
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                    return -1; // Username already exists

                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                    return -2; // Email already exists

                if (await _context.Users.AnyAsync(u => u.PhoneNumber == user.PhoneNumber))
                    return -3; // Phone number already exists

                user.CreatedDate = DateTime.Now;
                user.LastModified = DateTime.Now;

                // Ensure new users need approval
                user.IsApproved = false;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return user.UserID;
            }
            catch (Exception)
            {
                return -999; // General error
            }
        }

        public async Task<User?> AuthenticateUserAsync(string usernameOrEmail, string password)
        {
            try
            {
                string hashedPassword = _passwordService.HashPassword(password);
                var user = await _context.Users
                    .Where(u => (u.Username == usernameOrEmail || u.Email == usernameOrEmail) 
                               && u.PasswordHash == hashedPassword 
                               && u.IsActive)
                    .FirstOrDefaultAsync();
                return user;
            }
            catch
            {
                return null;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Where(u => u.UserID == userId && u.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserByIdForAdminAsync(int userId)
        {
            return await _context.Users
                .Where(u => u.UserID == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Where(u => u.Username == username && u.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Where(u => u.Email == email && u.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetUserByPhoneAsync(string phoneNumber)
        {
            return await _context.Users
                .Where(u => u.PhoneNumber == phoneNumber && u.IsActive)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(user.UserID);
                if (existingUser == null)
                {
                    return false;
                }

                // Only check for duplicates if username or email changed
                if (existingUser.Username != user.Username &&
                    await _context.Users.AnyAsync(u => u.Username == user.Username && u.UserID != user.UserID))
                {
                    return false;
                }

                if (existingUser.Email != user.Email &&
                    await _context.Users.AnyAsync(u => u.Email == user.Email && u.UserID != user.UserID))
                {
                    return false;
                }


                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.Username = user.Username;
                existingUser.Email = user.Email;
                existingUser.DateOfBirth = user.DateOfBirth;
                existingUser.Gender = user.Gender;
                existingUser.Country = user.Country;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.StreetAddress = user.StreetAddress;
                existingUser.City = user.City;
                existingUser.State = user.State;
                existingUser.ZipCode = user.ZipCode;
                existingUser.Bio = user.Bio;
                existingUser.IsActive = user.IsActive;
                existingUser.ReceiveNewsletter = user.ReceiveNewsletter;
                existingUser.ReceiveSMS = user.ReceiveSMS;
                existingUser.LastModified = DateTime.Now;

                var changesCount = await _context.SaveChangesAsync();

                return changesCount > 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null) return false;

                // Permanently delete the user from the database
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync(bool? isActive = null)
        {
            var query = _context.Users.AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            return await query
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllUsersAsync();

            return await _context.Users
                .Where(u => u.IsActive && 
                           (u.FirstName.Contains(searchTerm) ||
                            u.LastName.Contains(searchTerm) ||
                            u.Username.Contains(searchTerm) ||
                            u.Email.Contains(searchTerm)))
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> IsUsernameAvailableAsync(string username)
        {
            return !await _context.Users.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> IsEmailAvailableAsync(string email)
        {
            return !await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsPhoneAvailableAsync(string phoneNumber)
        {
            return !await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task<bool> IsUsernameAvailableForEditAsync(string username, int currentUserId)
        {
            var conflictingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.UserID != currentUserId);
            return conflictingUser == null;
        }

        public async Task<bool> IsEmailAvailableForEditAsync(string email, int currentUserId)
        {
            return !await _context.Users.AnyAsync(u => u.Email == email && u.UserID != currentUserId);
        }

        public async Task<bool> IsPhoneAvailableForEditAsync(string phoneNumber, int currentUserId)
        {
            return !await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber && u.UserID != currentUserId);
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
            var newUsersToday = await _context.Users.CountAsync(u => u.CreatedDate.Date == DateTime.Today);
            var recentUsers = await _context.Users.CountAsync(u => u.CreatedDate >= DateTime.Today.AddDays(-7));
            var newsletterSubscribers = await _context.Users.CountAsync(u => u.ReceiveNewsletter && u.IsActive);
            var pendingApproval = await _context.Users.CountAsync(u => !u.IsApproved);

            return new DashboardStats
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                NewUsersToday = newUsersToday,
                RecentUsers = recentUsers,
                NewsletterSubscribers = newsletterSubscribers,
                PendingApproval = pendingApproval
            };
        }

        public async Task<List<User>> GetUnapprovedUsersAsync()
        {
            return await _context.Users
                .Where(u => !u.IsApproved)
                .OrderBy(u => u.CreatedDate)
                .ToListAsync();
        }
    }
}