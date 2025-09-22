using Microsoft.AspNetCore.Mvc;
using RegistrationPortal.Models;
using RegistrationPortal.Services;
using RegistrationPortal.ViewModels;

namespace RegistrationPortal.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IValidationService _validationService;

        public UserController(IUserService userService, IValidationService validationService)
        {
            _userService = userService;
            _validationService = validationService;
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            // Check if admin is logged in
            var adminId = HttpContext.Session.GetInt32("AdminID");
            if (!adminId.HasValue)
            {
                return RedirectToAction("Index", "Admin");
            }

            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index", "Admin");
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
                TempData["ErrorMessage"] = "Error loading user data.";
                return RedirectToAction("Index", "Admin");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            // Check if admin is logged in
            var adminId = HttpContext.Session.GetInt32("AdminID");
            if (!adminId.HasValue)
            {
                return RedirectToAction("Index", "Admin");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Additional validation
                if (model.DateOfBirth.HasValue && !_validationService.IsValidAge(model.DateOfBirth.Value, 18))
                {
                    ModelState.AddModelError("DateOfBirth", "User must be at least 18 years old.");
                    return View(model);
                }

                // Get existing user
                var existingUser = await _userService.GetUserByIdAsync(model.UserID);
                if (existingUser == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index", "Admin");
                }

                // Check for username conflicts (excluding current user)
                var userWithSameUsername = await _userService.GetUserByUsernameAsync(model.Username);
                if (userWithSameUsername != null && userWithSameUsername.UserID != model.UserID)
                {
                    ModelState.AddModelError("Username", "Username already exists. Please choose a different username.");
                    return View(model);
                }

                // Check for email conflicts (excluding current user)
                var userWithSameEmail = await _userService.GetUserByEmailAsync(model.Email);
                if (userWithSameEmail != null && userWithSameEmail.UserID != model.UserID)
                {
                    ModelState.AddModelError("Email", "Email address already exists. Please use a different email.");
                    return View(model);
                }

                    // Update user entity
                    existingUser.FirstName = model.FirstName.Trim();
                    existingUser.LastName = model.LastName.Trim();
                    existingUser.Username = model.Username.Trim();
                    existingUser.Email = model.Email.Trim();
                    existingUser.DateOfBirth = model.DateOfBirth ?? existingUser.DateOfBirth; // Keep existing if null
                    existingUser.Gender = model.Gender;
                    existingUser.Country = model.Country ?? existingUser.Country; // Keep existing if null
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
                    TempData["SuccessMessage"] = "User updated successfully.";
                    return RedirectToAction("Index", "Admin");
                }
                else
                {
                    ModelState.AddModelError("", "Update failed. Please check your information and try again.");
                }
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while updating the user.");
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Cancel()
        {
            return RedirectToAction("Index", "Admin");
        }
    }
}