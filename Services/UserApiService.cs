using Microsoft.EntityFrameworkCore;
using RegistrationPortal.Data;
using RegistrationPortal.Models;
using RegistrationPortal.ViewModels;

namespace RegistrationPortal.Services
{
    public interface IUserApiService
    {
        Task<int> CreateUserAsync(User user);
        Task<User?> AuthenticateUserAsync(string usernameOrEmail, string password);
        Task<User?> GetUserByIdAsync(int userId, bool includeInactive = false);
        Task<IEnumerable<User>> GetAllUsersAsync(bool? isActive = null, string? searchTerm = null);
        Task<int> UpdateUserAsync(User user);
        Task<int> DeleteUserAsync(int userId);
        Task<DashboardStats> GetDashboardStatsAsync();
        Task<List<User>> GetUnapprovedUsersAsync();
    }

    public class UserApiService : IUserApiService
    {
        private readonly RegistrationDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly ILogger<UserApiService> _logger;

        public UserApiService(RegistrationDbContext context, IPasswordService passwordService, ILogger<UserApiService> logger)
        {
            _context = context;
            _passwordService = passwordService;
            _logger = logger;
        }

        public async Task<int> CreateUserAsync(User user)
        {
            try
            {
                // Check for existing username
                if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                    return -1; // Username already exists

                // Check for existing email
                if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                    return -2; // Email already exists

                // Check for existing phone number
                if (await _context.Users.AnyAsync(u => u.PhoneNumber == user.PhoneNumber))
                    return -3; // Phone number already exists

                // Set timestamps
                user.CreatedDate = DateTime.Now;
                user.LastModified = DateTime.Now;

                // Add user to context
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return user.UserID;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", user.Username);
                return -999;
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
                var query = _context.Users.Where(u => u.UserID == userId);

                if (!includeInactive)
                {
                    query = query.Where(u => u.IsActive);
                }

                return await query.FirstOrDefaultAsync();
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
                var query = _context.Users.AsQueryable();

                // Filter by active status if specified
                if (isActive.HasValue)
                {
                    query = query.Where(u => u.IsActive == isActive.Value);
                }

                // Apply search term if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchTermLower = searchTerm.ToLower();
                    query = query.Where(u =>
                        u.FirstName.ToLower().Contains(searchTermLower) ||
                        u.LastName.ToLower().Contains(searchTermLower) ||
                        u.Username.ToLower().Contains(searchTermLower) ||
                        u.Email.ToLower().Contains(searchTermLower) ||
                        (u.PhoneNumber != null && u.PhoneNumber.Contains(searchTerm)) ||
                        (u.City != null && u.City.ToLower().Contains(searchTermLower)) ||
                        (u.State != null && u.State.ToLower().Contains(searchTermLower))
                    );
                }

                var users = await query
                    .OrderByDescending(u => u.CreatedDate)
                    .ToListAsync();

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users. IsActive: {IsActive}, SearchTerm: {SearchTerm}", isActive, searchTerm);
                return new List<User>();
            }
        }

        public async Task<int> UpdateUserAsync(User user)
        {
            try
            {
                var existingUser = await _context.Users.FindAsync(user.UserID);
                if (existingUser == null)
                {
                    return -1; // User not found
                }

                // Check for duplicate username (excluding current user)
                if (existingUser.Username != user.Username &&
                    await _context.Users.AnyAsync(u => u.Username == user.Username && u.UserID != user.UserID))
                {
                    return -2; // Username already exists
                }

                // Check for duplicate email (excluding current user)
                if (existingUser.Email != user.Email &&
                    await _context.Users.AnyAsync(u => u.Email == user.Email && u.UserID != user.UserID))
                {
                    return -3; // Email already exists
                }

                // Check for duplicate phone (excluding current user)
                if (existingUser.PhoneNumber != user.PhoneNumber &&
                    await _context.Users.AnyAsync(u => u.PhoneNumber == user.PhoneNumber && u.UserID != user.UserID))
                {
                    return -4; // Phone already exists
                }

                // Update user properties
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
                existingUser.SecurityQuestion = user.SecurityQuestion;
                existingUser.SecurityAnswer = user.SecurityAnswer;
                existingUser.Bio = user.Bio;
                existingUser.ReferralCode = user.ReferralCode;
                existingUser.ReceiveNewsletter = user.ReceiveNewsletter;
                existingUser.ReceiveSMS = user.ReceiveSMS;
                existingUser.IsActive = user.IsActive;
                existingUser.LastModified = DateTime.Now;

                var result = await _context.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", user.UserID);
                return -999;
            }
        }

        public async Task<int> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return -1; // User not found
                }

                // Permanently delete the user from the database
                _context.Users.Remove(user);

                var result = await _context.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                return -999;
            }
        }

        public async Task<DashboardStats> GetDashboardStatsAsync()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return new DashboardStats();
            }
        }

        public async Task<List<User>> GetUnapprovedUsersAsync()
        {
            try
            {
                _logger.LogInformation("Getting unapproved users from database");
                return await _context.Users
                    .Where(u => !u.IsApproved)
                    .OrderBy(u => u.CreatedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unapproved users");
                return new List<User>();
            }
        }
    }
}