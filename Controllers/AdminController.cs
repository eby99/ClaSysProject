using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RegistrationPortal.Data;
using RegistrationPortal.Services;
using RegistrationPortal.ViewModels;

namespace RegistrationPortal.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IUserService _userService;
        private readonly RegistrationDbContext _context;
        private readonly IEventLoggerService _eventLogger;
        private readonly IConfiguration _configuration;

        public AdminController(IAdminService adminService, IUserService userService, RegistrationDbContext context, IEventLoggerService eventLogger, IConfiguration configuration)
        {
            _adminService = adminService;
            _userService = userService;
            _context = context;
            _eventLogger = eventLogger;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string filter = "active", string search = "", string fromDate = "", string toDate = "", string sortBy = "", string sortOrder = "asc")
        {
            var adminId = HttpContext.Session.GetInt32("AdminID");
            if (adminId.HasValue)
            {
                return await ShowDashboard(filter, search, fromDate, toDate, sortBy, sortOrder);
            }
            else
            {
                return ShowLogin();
            }
        }

        [HttpGet]
        public IActionResult AdminLogin()
        {
            return ShowLogin();
        }

        [HttpGet]
        public async Task<IActionResult> CreateDefaultAdmin()
        {
            try
            {
                // Check if admin already exists
                var existingAdmin = await _context.Admins.FirstOrDefaultAsync();
                if (existingAdmin != null)
                {
                    ViewBag.Message = $"Admin account already exists with username: {existingAdmin.Username}";
                    ViewBag.Success = true;
                    return View();
                }

                // Create default admin
                var passwordService = HttpContext.RequestServices.GetRequiredService<IPasswordService>();
                var defaultAdmin = new Models.Admin
                {
                    Username = "admin",
                    PasswordHash = passwordService.HashPassword("Admin@123"),
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                _context.Admins.Add(defaultAdmin);
                await _context.SaveChangesAsync();

                ViewBag.Message = "Default admin account created successfully! Username: admin, Password: Admin@123";
                ViewBag.Success = true;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Error creating admin account: {ex.Message}";
                ViewBag.Success = false;
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugAdmin()
        {
            try
            {
                var passwordService = HttpContext.RequestServices.GetRequiredService<IPasswordService>();
                var admin = await _context.Admins.FirstOrDefaultAsync();

                if (admin == null)
                {
                    return Json(new { error = "No admin found" });
                }

                var testPassword = "Admin@123";
                var generatedHash = passwordService.HashPassword(testPassword);
                var storedHash = admin.PasswordHash;
                var passwordMatch = passwordService.VerifyPassword(testPassword, storedHash);

                return Json(new
                {
                    username = admin.Username,
                    storedHash = storedHash,
                    generatedHash = generatedHash,
                    testPassword = testPassword,
                    passwordMatch = passwordMatch,
                    hashesMatch = storedHash == generatedHash
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> FixAdminPassword()
        {
            try
            {
                var passwordService = HttpContext.RequestServices.GetRequiredService<IPasswordService>();
                var admin = await _context.Admins.FirstOrDefaultAsync();

                if (admin == null)
                {
                    return Json(new { success = false, message = "No admin found" });
                }

                // Update the admin password to the correct hash
                var newPassword = "Admin@123";
                admin.PasswordHash = passwordService.HashPassword(newPassword);

                await _context.SaveChangesAsync();

                // Verify the fix worked
                var verifyPassword = passwordService.VerifyPassword(newPassword, admin.PasswordHash);

                return Json(new
                {
                    success = true,
                    message = "Admin password updated successfully",
                    username = admin.Username,
                    password = newPassword,
                    newHash = admin.PasswordHash,
                    verificationWorking = verifyPassword
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private IActionResult ShowLogin()
        {
            ViewBag.ShowLogin = true;
            ViewBag.ShowDashboard = false;
            return View("Index", new AdminLoginViewModel());
        }

        private async Task<IActionResult> ShowDashboard(string filter = "active", string search = "", string fromDate = "", string toDate = "", string sortBy = "", string sortOrder = "asc")
        {
            ViewBag.ShowLogin = false;
            ViewBag.ShowDashboard = true;

            try
            {
                var stats = await _userService.GetDashboardStatsAsync();

                List<Models.User> users;
                switch (filter?.ToLower())
                {
                    case "pending":
                        // Show only unapproved users
                        users = await _userService.GetUnapprovedUsersAsync();
                        break;
                    case "active":
                        // Show only active AND approved users
                        var activeUsers = await _userService.GetAllUsersAsync(true);
                        users = activeUsers.Where(u => u.IsApproved).ToList();
                        break;
                    case "inactive":
                        // Show only inactive users (regardless of approval status)
                        var inactiveUsers = await _userService.GetAllUsersAsync(false);
                        users = inactiveUsers.ToList();
                        break;
                    case "all":
                        // Show all APPROVED users regardless of active/inactive status (but exclude pending)
                        var allUsers = await _userService.GetAllUsersAsync(null);
                        users = allUsers.Where(u => u.IsApproved).ToList();
                        break;
                    default:
                        // Default to active approved users
                        var defaultUsers = await _userService.GetAllUsersAsync(true);
                        users = defaultUsers.Where(u => u.IsApproved).ToList();
                        break;
                }

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(search))
                {
                    users = users.Where(u =>
                        u.Username.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.LastName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(search, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // Apply date filter if provided
                if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out DateTime from))
                {
                    users = users.Where(u => u.CreatedDate.Date >= from.Date).ToList();
                }

                if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out DateTime to))
                {
                    users = users.Where(u => u.CreatedDate.Date <= to.Date).ToList();
                }

                // Apply sorting
                if (!string.IsNullOrWhiteSpace(sortBy))
                {
                    bool isDescending = sortOrder?.ToLower() == "desc";

                    switch (sortBy.ToLower())
                    {
                        case "id":
                            users = isDescending ? users.OrderByDescending(u => u.UserID).ToList()
                                                 : users.OrderBy(u => u.UserID).ToList();
                            break;
                        case "name":
                            users = isDescending ? users.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName).ToList()
                                                 : users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
                            break;
                        case "username":
                            users = isDescending ? users.OrderByDescending(u => u.Username).ToList()
                                                 : users.OrderBy(u => u.Username).ToList();
                            break;
                        case "country":
                            users = isDescending ? users.OrderByDescending(u => u.Country).ToList()
                                                 : users.OrderBy(u => u.Country).ToList();
                            break;
                        case "status":
                            users = isDescending ? users.OrderByDescending(u => u.IsActive).ThenByDescending(u => u.IsApproved).ToList()
                                                 : users.OrderBy(u => u.IsActive).ThenBy(u => u.IsApproved).ToList();
                            break;
                        case "joined":
                            users = isDescending ? users.OrderByDescending(u => u.CreatedDate).ToList()
                                                 : users.OrderBy(u => u.CreatedDate).ToList();
                            break;
                        default:
                            // Default sort by ID ascending
                            users = users.OrderBy(u => u.UserID).ToList();
                            break;
                    }
                }

                var viewModel = new AdminDashboardViewModel
                {
                    Stats = stats,
                    Users = users.Select(u => new UserListItemViewModel
                    {
                        UserID = u.UserID,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Username = u.Username,
                        Email = u.Email,
                        Country = u.Country,
                        CreatedDate = u.CreatedDate,
                        IsActive = u.IsActive,
                        IsApproved = u.IsApproved,
                        ReceiveNewsletter = u.ReceiveNewsletter
                    }).ToList()
                };

                ViewBag.AdminUsername = HttpContext.Session.GetString("AdminUsername");
                ViewBag.CurrentFilter = filter;
                ViewBag.SearchTerm = search;
                ViewBag.FromDate = fromDate;
                ViewBag.ToDate = toDate;
                ViewBag.SortBy = sortBy;
                ViewBag.SortOrder = sortOrder;
                return View("Index", viewModel);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading dashboard data.";
                return ShowLogin();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ShowLogin = true;
                ViewBag.ShowDashboard = false;
                return View("Index", model);
            }

            try
            {
                var admin = await _adminService.AuthenticateAdminAsync(model.Username.Trim(), model.Password);

                if (admin != null)
                {
                    HttpContext.Session.SetInt32("AdminID", admin.AdminID);
                    HttpContext.Session.SetString("AdminUsername", admin.Username);

                    // Log admin login
                    _eventLogger.LogUserAction("Admin Login", admin.Username,
                        $"Admin successful login from IP: {HttpContext.Connection.RemoteIpAddress}");
                    _eventLogger.LogSecurityEvent("Admin Authentication", admin.Username,
                        HttpContext.Connection.RemoteIpAddress?.ToString(), "Admin login successful");

                    return await ShowDashboard();
                }
                else
                {
                    // Log failed admin login
                    _eventLogger.LogSecurityEvent("Admin Login Failed", model.Username,
                        HttpContext.Connection.RemoteIpAddress?.ToString(), "Invalid admin credentials");

                    ModelState.AddModelError("", "Invalid admin credentials. Please try again.");
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
            }

            ViewBag.ShowLogin = true;
            ViewBag.ShowDashboard = false;
            return View("Index", model);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            var adminUsername = HttpContext.Session.GetString("AdminUsername");

            // Log admin logout
            if (!string.IsNullOrEmpty(adminUsername))
            {
                _eventLogger.LogUserAction("Admin Logout", adminUsername,
                    $"Admin logged out from IP: {HttpContext.Connection.RemoteIpAddress}");
            }

            HttpContext.Session.Remove("AdminID");
            HttpContext.Session.Remove("AdminUsername");
            return RedirectToAction("Index");
        }

        // CRUD Operations for User Management

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var adminId = HttpContext.Session.GetInt32("AdminID");
            if (!adminId.HasValue)
            {
                return RedirectToAction("AdminLogin");
            }

            try
            {
                var user = await _userService.GetUserByIdForAdminAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                var viewModel = new EditUserViewModel
                {
                    UserID = user.UserID,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.Username,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    Country = user.Country,
                    StreetAddress = user.StreetAddress,
                    City = user.City,
                    State = user.State,
                    ZipCode = user.ZipCode,
                    Bio = user.Bio,
                    IsActive = user.IsActive,
                    ReceiveNewsletter = user.ReceiveNewsletter,
                    ReceiveSMS = user.ReceiveSMS
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Error loading user data.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            var adminId = HttpContext.Session.GetInt32("AdminID");
            if (!adminId.HasValue)
            {
                return RedirectToAction("AdminLogin");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(model.UserID);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                // Update user properties
                user.FirstName = model.FirstName?.Trim() ?? user.FirstName;
                user.LastName = model.LastName?.Trim() ?? user.LastName;
                user.Email = model.Email?.Trim() ?? user.Email;
                user.PhoneNumber = model.PhoneNumber?.Trim() ?? user.PhoneNumber;
                user.DateOfBirth = model.DateOfBirth ?? user.DateOfBirth;
                user.Gender = model.Gender ?? user.Gender;
                user.Country = model.Country ?? user.Country;
                user.StreetAddress = model.StreetAddress?.Trim();
                user.City = model.City?.Trim();
                user.State = model.State?.Trim();
                user.ZipCode = model.ZipCode?.Trim();
                user.Bio = model.Bio?.Trim();
                user.IsActive = model.IsActive;
                user.ReceiveNewsletter = model.ReceiveNewsletter;
                user.ReceiveSMS = model.ReceiveSMS;

                var result = await _userService.UpdateUserAsync(user);
                if (result)
                {
                    // Log admin action
                    var adminUsername = HttpContext.Session.GetString("AdminUsername");
                    _eventLogger.LogUserAction("Admin Edit User", adminUsername,
                        $"Admin {adminUsername} edited user {user.Username} (ID: {user.UserID})");

                    TempData["SuccessMessage"] = "User updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update user.";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the user.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var adminId = HttpContext.Session.GetInt32("AdminID");
            if (!adminId.HasValue)
            {
                return RedirectToAction("AdminLogin");
            }

            try
            {
                var userToDelete = await _userService.GetUserByIdAsync(id);
                var result = await _userService.DeleteUserAsync(id);

                if (result)
                {
                    // Log admin action
                    var adminUsername = HttpContext.Session.GetString("AdminUsername");
                    _eventLogger.LogUserAction("Admin Delete User", adminUsername,
                        $"Admin {adminUsername} deleted user {userToDelete?.Username ?? "Unknown"} (ID: {id})");
                    _eventLogger.LogSecurityEvent("User Deletion", adminUsername,
                        HttpContext.Connection.RemoteIpAddress?.ToString(),
                        $"User {userToDelete?.Username ?? "Unknown"} (ID: {id}) was deleted by admin");

                    TempData["SuccessMessage"] = "User deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete user or user not found.";
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the user.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var adminId = HttpContext.Session.GetInt32("AdminID");
            if (!adminId.HasValue)
            {
                return RedirectToAction("AdminLogin");
            }

            try
            {
                var user = await _userService.GetUserByIdForAdminAsync(id);
                if (user != null)
                {
                    var originalStatus = user.IsActive;
                    user.IsActive = !user.IsActive;

                    var result = await _userService.UpdateUserAsync(user);
                    if (result)
                    {
                        // Double-check the update by re-fetching the user
                        var updatedUser = await _userService.GetUserByIdForAdminAsync(id);
                        if (updatedUser != null && updatedUser.IsActive == user.IsActive)
                        {
                            // Log admin action
                            var adminUsername = HttpContext.Session.GetString("AdminUsername");
                            var action = user.IsActive ? "activated" : "deactivated";
                            _eventLogger.LogUserAction($"Admin {action.Substring(0, 1).ToUpper()}{action.Substring(1)} User",
                                adminUsername, $"Admin {adminUsername} {action} user {user.Username} (ID: {id})");

                            TempData["SuccessMessage"] = $"User '{user.Username}' {action} successfully.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "User status update was not properly saved. Please try again.";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to update user status. The user may have duplicate username or email.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "User not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating user status: {ex.Message}";
            }

            // Preserve the current filter when redirecting back
            var currentFilter = Request.Form["filter"].ToString();
            if (string.IsNullOrEmpty(currentFilter))
            {
                currentFilter = Request.Query["filter"].ToString();
            }

            if (!string.IsNullOrEmpty(currentFilter))
            {
                return RedirectToAction("Index", new { filter = currentFilter, t = DateTime.Now.Ticks });
            }

            return RedirectToAction("Index", new { t = DateTime.Now.Ticks });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var adminId = HttpContext.Session.GetInt32("AdminID");
            if (!adminId.HasValue)
            {
                return RedirectToAction("AdminLogin");
            }

            try
            {
                // For API mode, use HttpClient to call the approve endpoint directly
                if (_configuration.GetValue<bool>("UseApiMode", false))
                {
                    using var httpClient = new HttpClient();
                    var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl", "http://localhost:8080").TrimEnd('/');

                    var response = await httpClient.PostAsync($"{apiBaseUrl}/api/UsersApi/{id}/approve", null);

                    if (response.IsSuccessStatusCode)
                    {
                        var user = await _userService.GetUserByIdForAdminAsync(id);
                        var username = user?.Username ?? "Unknown";

                        // Log admin action
                        var adminUsername = HttpContext.Session.GetString("AdminUsername");
                        _eventLogger.LogUserAction("Admin User Approval", adminUsername,
                            $"Admin {adminUsername} approved user {username} (ID: {id})");
                        _eventLogger.LogSecurityEvent("User Approval", adminUsername,
                            HttpContext.Connection.RemoteIpAddress?.ToString(),
                            $"User {username} (ID: {id}) was approved by admin");

                        TempData["SuccessMessage"] = $"User {username} has been approved successfully.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to approve user via API.";
                    }
                }
                else
                {
                    // Direct database mode
                    var user = await _userService.GetUserByIdForAdminAsync(id);
                    if (user != null)
                    {
                        user.IsApproved = true;

                        var result = await _userService.UpdateUserAsync(user);
                        if (result)
                        {
                            // Log admin action
                            var adminUsername = HttpContext.Session.GetString("AdminUsername");
                            _eventLogger.LogUserAction("Admin User Approval", adminUsername,
                                $"Admin {adminUsername} approved user {user.Username} (ID: {id})");
                            _eventLogger.LogSecurityEvent("User Approval", adminUsername,
                                HttpContext.Connection.RemoteIpAddress?.ToString(),
                                $"User {user.Username} (ID: {id}) was approved by admin");

                            TempData["SuccessMessage"] = $"User {user.Username} has been approved successfully.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to approve user.";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "User not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while approving user: {ex.Message}";
            }

            return RedirectToAction("Index", new { filter = "pending", t = DateTime.Now.Ticks });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(int id)
        {
            var adminId = HttpContext.Session.GetInt32("AdminID");
            if (!adminId.HasValue)
            {
                return RedirectToAction("AdminLogin");
            }

            try
            {
                // For API mode, use HttpClient to call the reject endpoint directly
                if (_configuration.GetValue<bool>("UseApiMode", false))
                {
                    var user = await _userService.GetUserByIdForAdminAsync(id);
                    var username = user?.Username ?? "Unknown";

                    using var httpClient = new HttpClient();
                    var apiBaseUrl = _configuration.GetValue<string>("ApiBaseUrl", "http://localhost:8080").TrimEnd('/');

                    var response = await httpClient.PostAsync($"{apiBaseUrl}/api/UsersApi/{id}/reject", null);

                    if (response.IsSuccessStatusCode)
                    {
                        // Log admin action
                        var adminUsername = HttpContext.Session.GetString("AdminUsername");
                        _eventLogger.LogUserAction("Admin User Rejection", adminUsername,
                            $"Admin {adminUsername} rejected user {username} (ID: {id})");
                        _eventLogger.LogSecurityEvent("User Rejection", adminUsername,
                            HttpContext.Connection.RemoteIpAddress?.ToString(),
                            $"User {username} (ID: {id}) was rejected and deleted by admin");

                        TempData["SuccessMessage"] = $"User {username} has been rejected and their account deleted.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Failed to reject user via API.";
                    }
                }
                else
                {
                    // Direct database mode
                    var user = await _userService.GetUserByIdForAdminAsync(id);
                    if (user != null)
                    {
                        // Log admin action before deletion
                        var adminUsername = HttpContext.Session.GetString("AdminUsername");
                        _eventLogger.LogUserAction("Admin User Rejection", adminUsername,
                            $"Admin {adminUsername} rejected user {user.Username} (ID: {id})");
                        _eventLogger.LogSecurityEvent("User Rejection", adminUsername,
                            HttpContext.Connection.RemoteIpAddress?.ToString(),
                            $"User {user.Username} (ID: {id}) was rejected and deleted by admin");

                        // Delete the user account
                        var result = await _userService.DeleteUserAsync(id);
                        if (result)
                        {
                            TempData["SuccessMessage"] = $"User {user.Username} has been rejected and their account deleted.";
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Failed to reject user.";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "User not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while rejecting user: {ex.Message}";
            }

            return RedirectToAction("Index", new { filter = "pending", t = DateTime.Now.Ticks });
        }

        [HttpPost]
        public async Task<IActionResult> CheckUniqueness(string field, string value, int currentUserId)
        {
            var adminId = HttpContext.Session.GetInt32("AdminID");
            if (!adminId.HasValue)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            try
            {
                bool isUnique = false;

                switch (field.ToLower())
                {
                    case "username":
                        isUnique = !await _context.Users.AnyAsync(u => u.Username == value && u.UserID != currentUserId);
                        break;
                    case "email":
                        isUnique = !await _context.Users.AnyAsync(u => u.Email == value && u.UserID != currentUserId);
                        break;
                    case "phonenumber":
                        isUnique = !await _context.Users.AnyAsync(u => u.PhoneNumber == value && u.UserID != currentUserId);
                        break;
                    default:
                        return Json(new { success = false, message = "Invalid field" });
                }

                return Json(new { success = true, isUnique = isUnique });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error checking uniqueness" });
            }
        }
    }
}