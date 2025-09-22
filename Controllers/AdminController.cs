using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public AdminController(IAdminService adminService, IUserService userService, RegistrationDbContext context)
        {
            _adminService = adminService;
            _userService = userService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string filter = "active")
        {
            var adminId = HttpContext.Session.GetInt32("AdminID");
            if (adminId.HasValue)
            {
                return await ShowDashboard(filter);
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

        private async Task<IActionResult> ShowDashboard(string filter = "active")
        {
            ViewBag.ShowLogin = false;
            ViewBag.ShowDashboard = true;

            try
            {
                var stats = await _userService.GetDashboardStatsAsync();

                // Determine filter value
                bool? isActiveFilter = filter?.ToLower() switch
                {
                    "all" => null,
                    "inactive" => false,
                    _ => true // default to active
                };

                var users = await _userService.GetAllUsersAsync(isActiveFilter);

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
                        ReceiveNewsletter = u.ReceiveNewsletter
                    }).ToList()
                };

                ViewBag.AdminUsername = HttpContext.Session.GetString("AdminUsername");
                ViewBag.CurrentFilter = filter;
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
                    return await ShowDashboard();
                }
                else
                {
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
                var result = await _userService.DeleteUserAsync(id);
                if (result)
                {
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
                            TempData["SuccessMessage"] = $"User '{user.Username}' {(user.IsActive ? "activated" : "deactivated")} successfully.";
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