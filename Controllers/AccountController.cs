using Microsoft.AspNetCore.Mvc;
using RegistrationPortal.Services;
using RegistrationPortal.ViewModels;
using RegistrationPortal.Models;
using System.Linq;

namespace RegistrationPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IPasswordService _passwordService;

        public AccountController(IUserService userService, IPasswordService passwordService)
        {
            _userService = userService;
            _passwordService = passwordService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserID").HasValue)
            {
                return RedirectToAction("Profile");
            }

            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
                ViewBag.NewUserName = TempData["NewUserName"];
            }

            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var user = await _userService.AuthenticateUserAsync(model.UsernameEmail.Trim(), model.Password);

                if (user != null)
                {
                    HttpContext.Session.SetInt32("UserID", user.UserID);
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("FirstName", user.FirstName);
                    HttpContext.Session.SetString("LastName", user.LastName);
                    HttpContext.Session.SetString("Email", user.Email);


                    TempData["LoginSuccess"] = true;
                    return RedirectToAction("Profile");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password. Please try again.");
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred during login. Please try again.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetInt32("UserID").HasValue)
            {
                return RedirectToAction("Profile");
            }

            return View(new RegistrationViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistrationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (!_passwordService.IsValidPassword(model.Password))
                {
                    ModelState.AddModelError("Password", "Password does not meet security requirements.");
                    return View(model);
                }

                if (model.DateOfBirth > DateTime.Now.AddYears(-18))
                {
                    ModelState.AddModelError("DateOfBirth", "You must be at least 18 years old to register.");
                    return View(model);
                }

                var user = new User
                {
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    Username = model.Username.Trim(),
                    Email = model.Email.Trim().ToLower(),
                    PasswordHash = _passwordService.HashPassword(model.Password),
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    Country = model.Country,
                    PhoneNumber = model.PhoneNumber.Trim(),
                    StreetAddress = model.StreetAddress?.Trim(),
                    City = model.City?.Trim(),
                    State = model.State?.Trim(),
                    ZipCode = model.ZipCode?.Trim(),
                    SecurityQuestion = model.SecurityQuestion,
                    SecurityAnswer = model.SecurityAnswer.Trim(),
                    Bio = model.Bio?.Trim(),
                    ReferralCode = model.ReferralCode?.Trim(),
                    ReceiveNewsletter = model.ReceiveNewsletter,
                    ReceiveSMS = model.ReceiveSMS,
                    TermsAcceptedDate = model.AcceptTerms ? DateTime.Now : null,
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString()
                };

                var result = await _userService.CreateUserAsync(user);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Registration successful! Please log in with your new account.";
                    TempData["NewUserName"] = model.FirstName;
                    return RedirectToAction("Login");
                }
                else if (result == -1)
                {
                    ModelState.AddModelError("Username", "Username is already taken. Please choose a different one.");
                }
                else if (result == -2)
                {
                    ModelState.AddModelError("Email", "Email address is already registered. Please use a different email or sign in.");
                }
                else if (result == -3)
                {
                    ModelState.AddModelError("PhoneNumber", "Phone number is already registered. Please use a different phone number.");
                }
                else
                {
                    ModelState.AddModelError("", "Registration failed. Please try again.");
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> CheckUsernameAvailability(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return Json(new { available = false, message = "Username is required" });

            // Get current user ID from session
            var currentUserId = HttpContext.Session.GetInt32("UserID");

            bool isAvailable;
            if (currentUserId.HasValue)
            {
                // For edit mode - exclude current user
                isAvailable = await _userService.IsUsernameAvailableForEditAsync(username, currentUserId.Value);
            }
            else
            {
                // For registration mode - check all users
                isAvailable = await _userService.IsUsernameAvailableAsync(username);
            }

            return Json(new {
                available = isAvailable,
                message = isAvailable ? "Username is available" : "Username is already taken"
            });
        }

        [HttpGet]
        public async Task<JsonResult> CheckEmailAvailability(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { available = false, message = "Email is required" });

            // Get current user ID from session
            var currentUserId = HttpContext.Session.GetInt32("UserID");

            bool isAvailable;
            if (currentUserId.HasValue)
            {
                // For edit mode - exclude current user
                isAvailable = await _userService.IsEmailAvailableForEditAsync(email, currentUserId.Value);
            }
            else
            {
                // For registration mode - check all users
                isAvailable = await _userService.IsEmailAvailableAsync(email);
            }

            return Json(new {
                available = isAvailable,
                message = isAvailable ? "Email is available" : "Email is already registered"
            });
        }

        [HttpGet]
        public async Task<JsonResult> CheckPhoneAvailability(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return Json(new { available = false, message = "Phone number is required" });

            // Get current user ID from session
            var currentUserId = HttpContext.Session.GetInt32("UserID");

            bool isAvailable;
            if (currentUserId.HasValue)
            {
                // For edit mode - exclude current user
                isAvailable = await _userService.IsPhoneAvailableForEditAsync(phoneNumber, currentUserId.Value);
            }
            else
            {
                // For registration mode - check all users
                isAvailable = await _userService.IsPhoneAvailableAsync(phoneNumber);
            }

            return Json(new {
                available = isAvailable,
                message = isAvailable ? "Phone number is available" : "Phone number is already registered"
            });
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> GetSecurityQuestion(string usernameOrEmail)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail))
                return Json(new { success = false, message = "Please enter username or email" });

            var user = await _userService.GetUserByUsernameAsync(usernameOrEmail) ??
                       await _userService.GetUserByEmailAsync(usernameOrEmail);

            if (user == null)
                return Json(new { success = false, message = "No account found with this username or email" });

            return Json(new {
                success = true,
                securityQuestion = user.SecurityQuestion,
                userId = user.UserID
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> VerifySecurityAnswer(int userId, string securityAnswer)
        {
            if (string.IsNullOrWhiteSpace(securityAnswer))
                return Json(new { success = false, message = "Please enter security answer" });

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            if (string.Equals(user.SecurityAnswer.Trim(), securityAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                // Generate reset token (simple approach - in production use proper token generation)
                var resetToken = Guid.NewGuid().ToString();
                TempData["ResetToken"] = resetToken;
                TempData["ResetUserId"] = userId;

                return Json(new { success = true, message = "Security answer verified", resetToken = resetToken });
            }

            return Json(new { success = false, message = "Incorrect security answer" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ResetPassword(string resetToken, string newPassword)
        {
            var storedToken = TempData["ResetToken"]?.ToString();
            var userIdObj = TempData["ResetUserId"];

            if (storedToken != resetToken || userIdObj == null)
                return Json(new { success = false, message = "Invalid or expired reset token" });

            if (!int.TryParse(userIdObj.ToString(), out int userId))
                return Json(new { success = false, message = "Invalid user data" });

            if (!_passwordService.IsValidPassword(newPassword))
                return Json(new { success = false, message = "Password does not meet security requirements" });

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            user.PasswordHash = _passwordService.HashPassword(newPassword);
            user.LastModified = DateTime.Now;

            var result = await _userService.UpdateUserAsync(user);
            if (result)
                return Json(new { success = true, message = "Password reset successfully" });

            return Json(new { success = false, message = "Failed to reset password" });
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(userId.Value);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                var viewModel = new UserProfileViewModel
                {
                    UserID = user.UserID,
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
                    Bio = user.Bio,
                    ReferralCode = user.ReferralCode,
                    ReceiveNewsletter = user.ReceiveNewsletter,
                    ReceiveSMS = user.ReceiveSMS,
                    IsActive = user.IsActive,
                    CreatedDate = user.CreatedDate,
                    LastModified = user.LastModified
                };

                if (TempData["LoginSuccess"] != null)
                {
                    ViewBag.LoginSuccess = true;
                }

                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading your profile.";
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(userId.Value);
                if (user == null)
                {
                    return RedirectToAction("Login");
                }

                var viewModel = new EditUserViewModel
                {
                    UserID = user.UserID,
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
                    Bio = user.Bio,
                    ReceiveNewsletter = user.ReceiveNewsletter,
                    ReceiveSMS = user.ReceiveSMS
                };

                return View(viewModel);
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while loading your profile.";
                return RedirectToAction("Profile");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditUserViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserID");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            // Ensure the UserID in the model matches the session user (security check)
            model.UserID = userId.Value;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Additional validation
                if (model.DateOfBirth.HasValue)
                {
                    var age = DateTime.Now.Year - model.DateOfBirth.Value.Year;
                    if (model.DateOfBirth.Value > DateTime.Now.AddYears(-age)) age--;

                    if (age < 18)
                    {
                        ModelState.AddModelError("DateOfBirth", "You must be at least 18 years old.");
                        return View(model);
                    }
                }

                // Get existing user
                var existingUser = await _userService.GetUserByIdAsync(userId.Value);
                if (existingUser == null)
                {
                    return RedirectToAction("Login");
                }

                // TEMPORARY: Disable uniqueness checks to test basic functionality
                // TODO: Re-implement proper uniqueness validation later

                // Update user entity
                existingUser.FirstName = model.FirstName.Trim();
                existingUser.LastName = model.LastName.Trim();
                existingUser.Username = model.Username.Trim();
                existingUser.Email = model.Email.Trim();
                existingUser.DateOfBirth = model.DateOfBirth ?? existingUser.DateOfBirth;
                existingUser.Gender = model.Gender;
                existingUser.Country = model.Country ?? existingUser.Country;
                existingUser.PhoneNumber = model.PhoneNumber?.Trim() ?? existingUser.PhoneNumber;
                existingUser.StreetAddress = model.StreetAddress?.Trim();
                existingUser.City = model.City?.Trim();
                existingUser.State = model.State?.Trim();
                existingUser.ZipCode = model.ZipCode?.Trim();
                existingUser.Bio = model.Bio?.Trim();
                existingUser.ReceiveNewsletter = model.ReceiveNewsletter;
                existingUser.ReceiveSMS = model.ReceiveSMS;

                bool updateResult = await _userService.UpdateUserAsync(existingUser);

                if (updateResult)
                {
                    TempData["ProfileUpdateSuccess"] = "Profile updated successfully!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    ModelState.AddModelError("", "Update failed. Please check your information and try again.");
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while updating your profile.");
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();


            return RedirectToAction("Login");
        }
    }
}