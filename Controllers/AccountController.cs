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
        private readonly HybridUserService? _hybridUserService;
        private readonly IEventLoggerService _eventLogger;

        public AccountController(IUserService userService, IPasswordService passwordService, IEventLoggerService eventLogger)
        {
            _userService = userService;
            _passwordService = passwordService;
            _hybridUserService = userService as HybridUserService;
            _eventLogger = eventLogger;
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
                    // Check if user is approved
                    if (!user.IsApproved)
                    {
                        _eventLogger.LogSecurityEvent("Login Attempt - Unapproved", user.Username,
                            HttpContext.Connection.RemoteIpAddress?.ToString(), "Login attempt by unapproved user");

                        ModelState.AddModelError("", "Your account is pending admin approval. Please wait for approval before logging in.");
                        return View(model);
                    }

                    HttpContext.Session.SetInt32("UserID", user.UserID);
                    HttpContext.Session.SetString("Username", user.Username);
                    HttpContext.Session.SetString("FirstName", user.FirstName);
                    HttpContext.Session.SetString("LastName", user.LastName);
                    HttpContext.Session.SetString("Email", user.Email);

                    // Log successful login
                    _eventLogger.LogUserAction("Login", user.Username,
                        $"Successful login from IP: {HttpContext.Connection.RemoteIpAddress}");
                    _eventLogger.LogSecurityEvent("User Login", user.Username,
                        HttpContext.Connection.RemoteIpAddress?.ToString(), "Successful authentication");

                    TempData["LoginSuccess"] = true;
                    return RedirectToAction("Profile");
                }
                else
                {
                    // Log failed login attempt
                    _eventLogger.LogSecurityEvent("Login Failed", model.UsernameEmail,
                        HttpContext.Connection.RemoteIpAddress?.ToString(), "Invalid credentials provided");

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

                // Store plain password temporarily for API mode
                _hybridUserService?.StoreTemporaryPassword(user.Username, model.Password);

                var result = await _userService.CreateUserAsync(user);

                if (result > 0)
                {
                    // Log successful registration
                    _eventLogger.LogUserAction("Registration", model.Username,
                        $"New user registered from IP: {HttpContext.Connection.RemoteIpAddress}");
                    _eventLogger.LogSecurityEvent("User Registration", model.Username,
                        HttpContext.Connection.RemoteIpAddress?.ToString(), "New account created successfully");

                    // Check if this is an AJAX request
                    if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    {
                        return Json(new {
                            success = true,
                            message = "Registration successful! Your account is pending admin approval. You will be notified once approved.",
                            userName = model.FirstName
                        });
                    }

                    TempData["SuccessMessage"] = "Registration successful! Your account is pending admin approval. You will be notified once approved.";
                    TempData["NewUserName"] = model.FirstName;
                    return RedirectToAction("Login");
                }
                else if (result == -1)
                {
                    var errorMessage = "Username is already taken. Please choose a different one.";
                    ModelState.AddModelError("Username", errorMessage);

                    // For AJAX requests, return JSON with field-specific errors
                    if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    {
                        return Json(new {
                            success = false,
                            message = "Registration failed due to validation errors.",
                            errors = new Dictionary<string, string>
                            {
                                ["Username"] = errorMessage
                            }
                        });
                    }
                }
                else if (result == -2)
                {
                    var errorMessage = "Email address is already registered. Please use a different email or sign in.";
                    ModelState.AddModelError("Email", errorMessage);

                    // For AJAX requests, return JSON with field-specific errors
                    if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    {
                        return Json(new {
                            success = false,
                            message = "Registration failed due to validation errors.",
                            errors = new Dictionary<string, string>
                            {
                                ["Email"] = errorMessage
                            }
                        });
                    }
                }
                else if (result == -3)
                {
                    var errorMessage = "Phone number is already registered. Please use a different phone number.";
                    ModelState.AddModelError("PhoneNumber", errorMessage);

                    // For AJAX requests, return JSON with field-specific errors
                    if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    {
                        return Json(new {
                            success = false,
                            message = "Registration failed due to validation errors.",
                            errors = new Dictionary<string, string>
                            {
                                ["PhoneNumber"] = errorMessage
                            }
                        });
                    }
                }
                else
                {
                    var errorMessage = "Registration failed. Please try again.";
                    ModelState.AddModelError("", errorMessage);

                    // For AJAX requests, return JSON error
                    if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                    {
                        return Json(new {
                            success = false,
                            message = errorMessage
                        });
                    }
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
            }

            // Handle AJAX request errors
            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Errors.First().ErrorMessage
                    );

                return Json(new {
                    success = false,
                    errors = errors
                });
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

            var result = _hybridUserService != null
                ? await _hybridUserService.GetSecurityQuestionAsync(usernameOrEmail)
                : (false, null, null, "Service not available");

            if (!result.Success)
                return Json(new { success = false, message = result.Message ?? "No account found with this username or email" });

            return Json(new {
                success = true,
                securityQuestion = result.SecurityQuestion,
                userId = result.UserId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> VerifySecurityAnswer(int userId, string securityAnswer)
        {
            if (string.IsNullOrWhiteSpace(securityAnswer))
                return Json(new { success = false, message = "Please enter security answer" });

            var result = _hybridUserService != null
                ? await _hybridUserService.VerifySecurityAnswerAsync(userId, securityAnswer)
                : (false, null, "Service not available");

            if (!result.Success)
                return Json(new { success = false, message = result.Message ?? "Incorrect security answer" });

            // Store token in TempData for backward compatibility
            TempData["ResetToken"] = result.ResetToken;
            TempData["ResetUserId"] = userId;

            return Json(new { success = true, message = result.Message ?? "Security answer verified", resetToken = result.ResetToken });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ResetPassword(string resetToken, string newPassword)
        {
            if (!_passwordService.IsValidPassword(newPassword))
                return Json(new { success = false, message = "Password does not meet security requirements" });

            var result = _hybridUserService != null
                ? await _hybridUserService.ResetPasswordAsync(resetToken, newPassword)
                : (false, "Service not available");

            if (!result.Item1) // Access first item (Success) of tuple
            {
                // Fallback to TempData approach for backward compatibility
                var storedToken = TempData["ResetToken"]?.ToString();
                var userIdObj = TempData["ResetUserId"];

                if (storedToken == resetToken && userIdObj != null && int.TryParse(userIdObj.ToString(), out int userId))
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    if (user != null)
                    {
                        user.PasswordHash = _passwordService.HashPassword(newPassword);
                        user.LastModified = DateTime.Now;

                        var updateResult = await _userService.UpdateUserAsync(user);
                        if (updateResult)
                            return Json(new { success = true, message = "Password reset successfully" });
                    }
                }

                return Json(new { success = false, message = result.Item2 ?? "Failed to reset password" }); // Access second item (Message)
            }

            return Json(new { success = true, message = result.Item2 ?? "Password reset successfully" }); // Access second item (Message)
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
                    ReceiveSMS = user.ReceiveSMS,
                    SecurityQuestion = user.SecurityQuestion,
                    SecurityAnswer = user.SecurityAnswer
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
                if (Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = "Session expired. Please login again." });
                }
                return RedirectToAction("Login");
            }

            // Ensure the UserID in the model matches the session user (security check)
            model.UserID = userId.Value;

            if (!ModelState.IsValid)
            {
                if (Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                        .ToList();
                    return Json(new { success = false, message = "Validation failed.", errors = errors });
                }
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
                        var errorMessage = "You must be at least 18 years old.";
                        if (Request.Headers.Accept.ToString().Contains("application/json"))
                        {
                            return Json(new { success = false, message = errorMessage });
                        }
                        ModelState.AddModelError("DateOfBirth", errorMessage);
                        return View(model);
                    }
                }

                // Get existing user through API service
                var existingUser = await _userService.GetUserByIdAsync(userId.Value);
                if (existingUser == null)
                {
                    var errorMessage = "User not found. Please login again.";
                    if (Request.Headers.Accept.ToString().Contains("application/json"))
                    {
                        return Json(new { success = false, message = errorMessage });
                    }
                    return RedirectToAction("Login");
                }

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

                // Update password if provided
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    if (model.NewPassword != model.ConfirmNewPassword)
                    {
                        var errorMessage = "Password confirmation does not match.";
                        if (Request.Headers.Accept.ToString().Contains("application/json"))
                        {
                            return Json(new { success = false, message = errorMessage });
                        }
                        ModelState.AddModelError("ConfirmNewPassword", errorMessage);
                        return View(model);
                    }

                    if (model.NewPassword.Length < 6)
                    {
                        var errorMessage = "Password must be at least 6 characters long.";
                        if (Request.Headers.Accept.ToString().Contains("application/json"))
                        {
                            return Json(new { success = false, message = errorMessage });
                        }
                        ModelState.AddModelError("NewPassword", errorMessage);
                        return View(model);
                    }

                    existingUser.PasswordHash = _passwordService.HashPassword(model.NewPassword);
                }

                // Update security question and answer if provided
                if (!string.IsNullOrEmpty(model.SecurityQuestion))
                {
                    existingUser.SecurityQuestion = model.SecurityQuestion.Trim();
                }

                if (!string.IsNullOrEmpty(model.SecurityAnswer))
                {
                    existingUser.SecurityAnswer = model.SecurityAnswer.Trim();
                }

                // Update through API service (HybridUserService will route appropriately)
                bool updateResult = await _userService.UpdateUserAsync(existingUser);

                if (updateResult)
                {
                    var successMessage = "Profile updated successfully!";
                    if (Request.Headers.Accept.ToString().Contains("application/json"))
                    {
                        return Json(new { success = true, message = successMessage });
                    }
                    TempData["ProfileUpdateSuccess"] = successMessage;
                    return RedirectToAction("Profile");
                }
                else
                {
                    var errorMessage = "Update failed. Please check your information and try again.";
                    if (Request.Headers.Accept.ToString().Contains("application/json"))
                    {
                        return Json(new { success = false, message = errorMessage });
                    }
                    ModelState.AddModelError("", errorMessage);
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                _eventLogger.LogError("Profile update error", ex, $"User ID: {userId.Value}");

                var errorMessage = "An error occurred while updating your profile.";
                if (Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return Json(new { success = false, message = errorMessage });
                }
                ModelState.AddModelError("", errorMessage);
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Logout()
        {
            var username = HttpContext.Session.GetString("Username");

            // Log logout
            if (!string.IsNullOrEmpty(username))
            {
                _eventLogger.LogUserAction("Logout", username,
                    $"User logged out from IP: {HttpContext.Connection.RemoteIpAddress}");
            }

            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }

        // AJAX endpoints for real-time validation
        [HttpPost]
        public async Task<IActionResult> CheckUsername(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return Json(new { available = false });
            }

            try
            {
                var isAvailable = await _userService.IsUsernameAvailableAsync(username);
                return Json(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                _eventLogger.LogError($"Error checking username availability: {username}", ex);
                return Json(new { available = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { available = false });
            }

            try
            {
                var isAvailable = await _userService.IsEmailAvailableAsync(email);
                return Json(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                _eventLogger.LogError($"Error checking email availability: {email}", ex);
                return Json(new { available = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return Json(new { available = false });
            }

            try
            {
                var isAvailable = await _userService.IsPhoneAvailableAsync(phoneNumber);
                return Json(new { available = isAvailable });
            }
            catch (Exception ex)
            {
                _eventLogger.LogError($"Error checking phone number availability: {phoneNumber}", ex);
                return Json(new { available = false });
            }
        }
    }
}