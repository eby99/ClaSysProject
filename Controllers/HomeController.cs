using Microsoft.AspNetCore.Mvc;
using RegistrationPortal.Models;
using RegistrationPortal.Services;
using RegistrationPortal.ViewModels;

namespace RegistrationPortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUserService _userService;
        private readonly IPasswordService _passwordService;
        private readonly IValidationService _validationService;

        public HomeController(
            IUserService userService, 
            IPasswordService passwordService, 
            IValidationService validationService)
        {
            _userService = userService;
            _passwordService = passwordService;
            _validationService = validationService;
        }

        public IActionResult Index()
        {
            // Check if user is logged in via session
            var userId = HttpContext.Session.GetInt32("UserID");
            var adminId = HttpContext.Session.GetInt32("AdminID");

            // If any user is logged in, clear their session to log them out
            if (userId.HasValue || adminId.HasValue)
            {
                HttpContext.Session.Clear();
                TempData["LogoutMessage"] = "You have been logged out successfully.";
            }

            return View(); // This will show the landing page
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegistrationViewModel()); // This will show the registration form
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistrationViewModel model)
        {
            // Your existing registration logic here - same as the old Index POST method
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (!_passwordService.IsValidPassword(model.Password))
                {
                    ModelState.AddModelError("Password", "Password does not meet security requirements");
                    return View(model);
                }

                if (!_validationService.IsValidAge(model.DateOfBirth, 18))
                {
                    ModelState.AddModelError("DateOfBirth", "You must be at least 18 years old");
                    return View(model);
                }

                if (!await _userService.IsUsernameAvailableAsync(model.Username))
                {
                    ModelState.AddModelError("Username", "Username already exists");
                    return View(model);
                }

                if (!await _userService.IsEmailAvailableAsync(model.Email))
                {
                    ModelState.AddModelError("Email", "Email address already exists");
                    return View(model);
                }

                var user = new User
                {
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    Username = model.Username.Trim(),
                    Email = model.Email.Trim(),
                    PasswordHash = _passwordService.HashPassword(model.Password),
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    Country = model.Country,
                    PhoneNumber = model.PhoneNumber.Trim(),
                    StreetAddress = string.IsNullOrWhiteSpace(model.StreetAddress) ? null : model.StreetAddress.Trim(),
                    City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim(),
                    State = string.IsNullOrWhiteSpace(model.State) ? null : model.State.Trim(),
                    ZipCode = string.IsNullOrWhiteSpace(model.ZipCode) ? null : model.ZipCode.Trim(),
                    SecurityQuestion = model.SecurityQuestion,
                    SecurityAnswer = model.SecurityAnswer.Trim(),
                    Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim(),
                    ReferralCode = string.IsNullOrWhiteSpace(model.ReferralCode) ? null : model.ReferralCode.Trim(),
                    ReceiveNewsletter = model.ReceiveNewsletter,
                    ReceiveSMS = model.ReceiveSMS,
                    TermsAcceptedDate = DateTime.Now,
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString()
                };

                int result = await _userService.CreateUserAsync(user);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = $"Welcome {model.FirstName}! Your account has been created successfully.";
                    TempData["NewUserName"] = model.FirstName;
                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    string errorMessage = GetErrorMessage(result);
                    ModelState.AddModelError("", errorMessage);
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred during registration. Please try again.");
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Reset()
        {
            return RedirectToAction("Register");
        }

        private static string GetErrorMessage(int errorCode)
        {
            return errorCode switch
            {
                -1 => "Username already exists. Please choose a different username.",
                -2 => "Email address already exists. Please use a different email.",
                -999 => "An unexpected error occurred. Please try again.",
                _ => "Registration failed. Please check your information and try again."
            };
        }
    }
}