using Microsoft.AspNetCore.Mvc;
using RegistrationPortal.Models;
using RegistrationPortal.Services;
using RegistrationPortal.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace RegistrationPortal.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class UsersApiController : ControllerBase
    {
        private readonly IUserApiService _userApiService;
        private readonly IPasswordService _passwordService;
        private readonly IValidationService _validationService;
        private readonly ILogger<UsersApiController> _logger;

        public UsersApiController(
            IUserApiService userApiService,
            IPasswordService passwordService,
            IValidationService validationService,
            ILogger<UsersApiController> logger)
        {
            _userApiService = userApiService;
            _passwordService = passwordService;
            _validationService = validationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all users with optional filtering
        /// </summary>
        /// <param name="isActive">Filter by active status</param>
        /// <param name="searchTerm">Search term for filtering users</param>
        /// <returns>List of users</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers(
            [FromQuery] bool? isActive = null,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                _logger.LogInformation("Getting all users. IsActive: {IsActive}, SearchTerm: {SearchTerm}", isActive, searchTerm);

                var users = await _userApiService.GetAllUsersAsync(isActive, searchTerm);
                var userDtos = users.Select(u => new UserResponseDto(u));

                _logger.LogInformation("Retrieved {Count} users", userDtos.Count());

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return StatusCode(500, new ErrorResponse("An error occurred while retrieving users"));
            }
        }

        /// <summary>
        /// Get a specific user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="includeInactive">Include inactive users in search</param>
        /// <returns>User details</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserResponseDto>> GetUserById(
            int id,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ErrorResponse("Invalid user ID"));
                }

                _logger.LogInformation("Getting user by ID: {UserId}", id);

                var user = await _userApiService.GetUserByIdAsync(id, includeInactive);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", id);
                    return NotFound(new ErrorResponse($"User with ID {id} not found"));
                }

                return Ok(new UserResponseDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", id);
                return StatusCode(500, new ErrorResponse("An error occurred while retrieving the user"));
            }
        }

        /// <summary>
        /// Get all unapproved users
        /// </summary>
        /// <returns>List of unapproved users</returns>
        [HttpGet("unapproved")]
        [ProducesResponseType(typeof(IEnumerable<UserResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUnapprovedUsers()
        {
            try
            {
                _logger.LogInformation("Getting all unapproved users");
                var users = await _userApiService.GetUnapprovedUsersAsync();
                var userDtos = users.Select(u => new UserResponseDto(u));

                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unapproved users");
                return StatusCode(500, new ErrorResponse("An error occurred while retrieving unapproved users"));
            }
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        /// <param name="userDto">User creation data</param>
        /// <returns>Created user details</returns>
        [HttpPost]
        [ProducesResponseType(typeof(UserCreatedResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserCreatedResponse>> CreateUser([FromBody] CreateUserDto userDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new ErrorResponse("Validation failed", errors));
                }

                _logger.LogInformation("Creating new user: {Username}", userDto.Username);

                // Validate password requirements
                var passwordValidation = _validationService.ValidatePasswordRequirements(userDto.Password);
                if (!passwordValidation.IsValid)
                {
                    _logger.LogWarning("Password validation failed for user: {Username}", userDto.Username);
                    return BadRequest(passwordValidation.ToApiResponse());
                }

                var user = new User
                {
                    FirstName = userDto.FirstName,
                    LastName = userDto.LastName,
                    Username = userDto.Username,
                    Email = userDto.Email,
                    PasswordHash = _passwordService.HashPassword(userDto.Password),
                    DateOfBirth = userDto.DateOfBirth,
                    Gender = userDto.Gender,
                    Country = userDto.Country,
                    PhoneNumber = userDto.PhoneNumber,
                    StreetAddress = userDto.StreetAddress,
                    City = userDto.City,
                    State = userDto.State,
                    ZipCode = userDto.ZipCode,
                    SecurityQuestion = userDto.SecurityQuestion,
                    SecurityAnswer = userDto.SecurityAnswer,
                    Bio = userDto.Bio,
                    ReferralCode = userDto.ReferralCode,
                    ReceiveNewsletter = userDto.ReceiveNewsletter,
                    ReceiveSMS = userDto.ReceiveSMS,
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers.UserAgent.ToString(),
                    IsApproved = false,  // CRITICAL: New users need approval
                    IsActive = true,
                    TermsAcceptedDate = userDto.AcceptTerms ? DateTime.Now : null
                };

                // Comprehensive validation including uniqueness checks
                var validationResult = _validationService.ValidateUser(user, isUpdate: false);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("User validation failed for: {Username}. Errors: {Errors}",
                        userDto.Username, validationResult.GetErrorsAsString());
                    return BadRequest(validationResult.ToApiResponse());
                }

                var result = await _userApiService.CreateUserAsync(user);

                return result switch
                {
                    -1 => Conflict(new ErrorResponse("Username already exists")),
                    -2 => Conflict(new ErrorResponse("Email already exists")),
                    -3 => Conflict(new ErrorResponse("Phone number already exists")),
                    -999 => StatusCode(500, new ErrorResponse("An error occurred while creating the user")),
                    > 0 => Created($"/api/users/{result}", new UserCreatedResponse(result, "User created successfully")),
                    _ => StatusCode(500, new ErrorResponse("Unexpected error occurred"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Username}", userDto.Username);
                return StatusCode(500, new ErrorResponse("An error occurred while creating the user"));
            }
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="userDto">User update data</param>
        /// <returns>Update result</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] UpdateUserDto userDto)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ErrorResponse("Invalid user ID"));
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new ErrorResponse("Validation failed", errors));
                }

                _logger.LogInformation("Updating user: {UserId}", id);

                var existingUser = await _userApiService.GetUserByIdAsync(id, true);
                if (existingUser == null)
                {
                    return NotFound(new ErrorResponse($"User with ID {id} not found"));
                }

                existingUser.FirstName = userDto.FirstName;
                existingUser.LastName = userDto.LastName;
                existingUser.Username = userDto.Username;
                existingUser.Email = userDto.Email;
                existingUser.DateOfBirth = userDto.DateOfBirth;
                existingUser.Gender = userDto.Gender;
                existingUser.Country = userDto.Country;
                existingUser.PhoneNumber = userDto.PhoneNumber;
                existingUser.StreetAddress = userDto.StreetAddress;
                existingUser.City = userDto.City;
                existingUser.State = userDto.State;
                existingUser.ZipCode = userDto.ZipCode;
                existingUser.SecurityQuestion = userDto.SecurityQuestion;
                existingUser.SecurityAnswer = userDto.SecurityAnswer;
                existingUser.Bio = userDto.Bio;
                existingUser.ReferralCode = userDto.ReferralCode;
                existingUser.ReceiveNewsletter = userDto.ReceiveNewsletter;
                existingUser.ReceiveSMS = userDto.ReceiveSMS;
                existingUser.IsActive = userDto.IsActive;

                // Skip comprehensive validation for updates to avoid unique constraint conflicts
                // The update process should handle basic validation, but not uniqueness checks
                // since the user might be keeping their own data unchanged
                _logger.LogInformation("Skipping comprehensive validation for update of user: {UserId}", id);

                var result = await _userApiService.UpdateUserAsync(existingUser);

                return result switch
                {
                    -1 => NotFound(new ErrorResponse("User not found")),
                    -2 => Conflict(new ErrorResponse("Username already exists for another user")),
                    -3 => Conflict(new ErrorResponse("Email already exists for another user")),
                    -4 => Conflict(new ErrorResponse("Phone number already exists for another user")),
                    -999 => StatusCode(500, new ErrorResponse("An error occurred while updating the user")),
                    > 0 => Ok(new SuccessResponse("User updated successfully")),
                    _ => BadRequest(new ErrorResponse("No changes were made"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", id);
                return StatusCode(500, new ErrorResponse("An error occurred while updating the user"));
            }
        }

        /// <summary>
        /// Delete a user (soft delete)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Delete result</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteUser(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ErrorResponse("Invalid user ID"));
                }

                _logger.LogInformation("Deleting user: {UserId}", id);

                var result = await _userApiService.DeleteUserAsync(id);

                return result switch
                {
                    -1 => NotFound(new ErrorResponse("User not found")),
                    -999 => StatusCode(500, new ErrorResponse("An error occurred while deleting the user")),
                    > 0 => Ok(new SuccessResponse("User deleted successfully")),
                    _ => BadRequest(new ErrorResponse("No changes were made"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                return StatusCode(500, new ErrorResponse("An error occurred while deleting the user"));
            }
        }

        /// <summary>
        /// Authenticate a user
        /// </summary>
        /// <param name="loginDto">Login credentials</param>
        /// <returns>Authentication result</returns>
        [HttpPost("authenticate")]
        [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<UserResponseDto>> Authenticate([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new ErrorResponse("Validation failed", errors));
                }

                _logger.LogInformation("Authenticating user: {UsernameOrEmail}", loginDto.UsernameOrEmail);

                var user = await _userApiService.AuthenticateUserAsync(loginDto.UsernameOrEmail, loginDto.Password);

                if (user == null)
                {
                    _logger.LogWarning("Failed authentication attempt for: {UsernameOrEmail}", loginDto.UsernameOrEmail);
                    return Unauthorized(new ErrorResponse("Invalid username/email or password"));
                }

                _logger.LogInformation("Successful authentication for user: {UserId}", user.UserID);
                return Ok(new UserResponseDto(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating user: {UsernameOrEmail}", loginDto.UsernameOrEmail);
                return StatusCode(500, new ErrorResponse("An error occurred during authentication"));
            }
        }

        /// <summary>
        /// Get dashboard statistics
        /// </summary>
        /// <returns>Dashboard statistics</returns>
        [HttpGet("dashboard-stats")]
        [ProducesResponseType(typeof(DashboardStats), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DashboardStats>> GetDashboardStats()
        {
            try
            {
                _logger.LogInformation("Getting dashboard statistics");

                var stats = await _userApiService.GetDashboardStatsAsync();

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard statistics");
                return StatusCode(500, new ErrorResponse("An error occurred while retrieving dashboard statistics"));
            }
        }

        /// <summary>
        /// Get security question for forgot password
        /// </summary>
        /// <param name="usernameOrEmail">Username or email</param>
        /// <returns>Security question and user ID</returns>
        [HttpPost("forgot-password/security-question")]
        [ProducesResponseType(typeof(SecurityQuestionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SecurityQuestionResponse>> GetSecurityQuestion([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UsernameOrEmail))
                {
                    return BadRequest(new ErrorResponse("Username or email is required"));
                }

                _logger.LogInformation("Getting security question for: {UsernameOrEmail}", request.UsernameOrEmail);

                // Get all users (including inactive) to find the account for password reset
                var users = await _userApiService.GetAllUsersAsync(true);
                var user = users.FirstOrDefault(u =>
                    u.Username.Equals(request.UsernameOrEmail, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Equals(request.UsernameOrEmail, StringComparison.OrdinalIgnoreCase));

                if (user == null)
                {
                    _logger.LogWarning("No user found for forgot password request: {UsernameOrEmail}", request.UsernameOrEmail);
                    return NotFound(new ErrorResponse("No account found with this username or email"));
                }

                return Ok(new SecurityQuestionResponse
                {
                    SecurityQuestion = user.SecurityQuestion,
                    UserId = user.UserID
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting security question for: {UsernameOrEmail}", request.UsernameOrEmail);
                return StatusCode(500, new ErrorResponse("An error occurred while retrieving security question"));
            }
        }

        /// <summary>
        /// Verify security answer for forgot password
        /// </summary>
        /// <param name="request">Security answer verification request</param>
        /// <returns>Verification result with reset token</returns>
        [HttpPost("forgot-password/verify-answer")]
        [ProducesResponseType(typeof(SecurityVerificationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SecurityVerificationResponse>> VerifySecurityAnswer([FromBody] VerifySecurityAnswerDto request)
        {
            try
            {
                if (request.UserId <= 0 || string.IsNullOrWhiteSpace(request.SecurityAnswer))
                {
                    return BadRequest(new ErrorResponse("User ID and security answer are required"));
                }

                _logger.LogInformation("Verifying security answer for user: {UserId}", request.UserId);

                var user = await _userApiService.GetUserByIdAsync(request.UserId, true);
                if (user == null)
                {
                    return NotFound(new ErrorResponse("User not found"));
                }

                if (string.Equals(user.SecurityAnswer.Trim(), request.SecurityAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    var resetToken = Guid.NewGuid().ToString();

                    // In a real implementation, you'd store this token in the database with expiration
                    // For now, we'll use a simple in-memory cache
                    PasswordResetTokenCache.AddToken(resetToken, request.UserId);

                    return Ok(new SecurityVerificationResponse
                    {
                        ResetToken = resetToken,
                        Message = "Security answer verified"
                    });
                }

                _logger.LogWarning("Invalid security answer for user: {UserId}", request.UserId);
                return BadRequest(new ErrorResponse("Incorrect security answer"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying security answer for user: {UserId}", request.UserId);
                return StatusCode(500, new ErrorResponse("An error occurred while verifying security answer"));
            }
        }

        /// <summary>
        /// Reset password using reset token
        /// </summary>
        /// <param name="request">Password reset request</param>
        /// <returns>Reset result</returns>
        [HttpPost("forgot-password/reset")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ResetToken) || string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return BadRequest(new ErrorResponse("Reset token and new password are required"));
                }

                _logger.LogInformation("Resetting password with token: {ResetToken}", request.ResetToken.Substring(0, 8) + "...");

                var userId = PasswordResetTokenCache.GetUserId(request.ResetToken);
                if (userId == null)
                {
                    return BadRequest(new ErrorResponse("Invalid or expired reset token"));
                }

                // Validate password requirements
                var passwordValidation = _validationService.ValidatePasswordRequirements(request.NewPassword);
                if (!passwordValidation.IsValid)
                {
                    return BadRequest(passwordValidation.ToApiResponse());
                }

                var user = await _userApiService.GetUserByIdAsync(userId.Value, true);
                if (user == null)
                {
                    return NotFound(new ErrorResponse("User not found"));
                }

                user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
                user.LastModified = DateTime.Now;

                var result = await _userApiService.UpdateUserAsync(user);

                if (result > 0)
                {
                    // Remove the used token
                    PasswordResetTokenCache.RemoveToken(request.ResetToken);

                    _logger.LogInformation("Password reset successful for user: {UserId}", userId.Value);
                    return Ok(new SuccessResponse("Password reset successfully"));
                }

                return BadRequest(new ErrorResponse("Failed to reset password"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password with token: {ResetToken}", request.ResetToken?.Substring(0, 8) + "...");
                return StatusCode(500, new ErrorResponse("An error occurred while resetting password"));
            }
        }

        /// <summary>
        /// Update user with UserID in the request body
        /// </summary>
        /// <param name="userDto">User update data with UserID</param>
        /// <returns>Update result</returns>
        [HttpPut]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> UpdateUserByBody([FromBody] UpdateUserWithIdDto userDto)
        {
            try
            {
                if (userDto.UserID <= 0)
                {
                    return BadRequest(new ErrorResponse("Invalid user ID"));
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return BadRequest(new ErrorResponse("Validation failed", errors));
                }

                _logger.LogInformation("Updating user via body: {UserId}", userDto.UserID);

                var existingUser = await _userApiService.GetUserByIdAsync(userDto.UserID, true);
                if (existingUser == null)
                {
                    return NotFound(new ErrorResponse($"User with ID {userDto.UserID} not found"));
                }

                existingUser.FirstName = userDto.FirstName;
                existingUser.LastName = userDto.LastName;
                existingUser.Username = userDto.Username;
                existingUser.Email = userDto.Email;
                existingUser.DateOfBirth = userDto.DateOfBirth;
                existingUser.Gender = userDto.Gender;
                existingUser.Country = userDto.Country;
                existingUser.PhoneNumber = userDto.PhoneNumber;
                existingUser.StreetAddress = userDto.StreetAddress;
                existingUser.City = userDto.City;
                existingUser.State = userDto.State;
                existingUser.ZipCode = userDto.ZipCode;
                existingUser.SecurityQuestion = userDto.SecurityQuestion;
                existingUser.SecurityAnswer = userDto.SecurityAnswer;
                existingUser.Bio = userDto.Bio;
                existingUser.ReferralCode = userDto.ReferralCode;
                existingUser.ReceiveNewsletter = userDto.ReceiveNewsletter;
                existingUser.ReceiveSMS = userDto.ReceiveSMS;
                existingUser.IsActive = userDto.IsActive;

                // Skip comprehensive validation for updates to avoid unique constraint conflicts
                // The update process should handle basic validation, but not uniqueness checks
                // since the user might be keeping their own data unchanged
                _logger.LogInformation("Skipping comprehensive validation for update via body of user: {UserId}", userDto.UserID);

                var result = await _userApiService.UpdateUserAsync(existingUser);

                return result switch
                {
                    -1 => NotFound(new ErrorResponse("User not found")),
                    -2 => Conflict(new ErrorResponse("Username already exists")),
                    -3 => Conflict(new ErrorResponse("Email already exists")),
                    -4 => Conflict(new ErrorResponse("Phone number already exists")),
                    -999 => StatusCode(500, new ErrorResponse("An error occurred while updating the user")),
                    > 0 => Ok(new SuccessResponse("User updated successfully")),
                    _ => BadRequest(new ErrorResponse("No changes were made"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user via body: {UserId}", userDto.UserID);
                return StatusCode(500, new ErrorResponse("An error occurred while updating the user"));
            }
        }

        /// <summary>
        /// Approve a pending user
        /// </summary>
        /// <param name="id">User ID to approve</param>
        /// <returns>Approval result</returns>
        [HttpPost("{id}/approve")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ApproveUser(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ErrorResponse("Invalid user ID"));
                }

                _logger.LogInformation("Approving user: {UserId}", id);

                var user = await _userApiService.GetUserByIdAsync(id, true);
                if (user == null)
                {
                    return NotFound(new ErrorResponse("User not found"));
                }

                if (user.IsApproved)
                {
                    return BadRequest(new ErrorResponse("User is already approved"));
                }

                user.IsApproved = true;
                user.LastModified = DateTime.Now;

                var result = await _userApiService.UpdateUserAsync(user);

                return result switch
                {
                    -1 => NotFound(new ErrorResponse("User not found")),
                    -999 => StatusCode(500, new ErrorResponse("An error occurred while approving the user")),
                    > 0 => Ok(new SuccessResponse($"User {user.Username} has been approved successfully")),
                    _ => BadRequest(new ErrorResponse("No changes were made"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving user: {UserId}", id);
                return StatusCode(500, new ErrorResponse("An error occurred while approving the user"));
            }
        }

        /// <summary>
        /// Reject and delete a pending user
        /// </summary>
        /// <param name="id">User ID to reject</param>
        /// <returns>Rejection result</returns>
        [HttpPost("{id}/reject")]
        [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> RejectUser(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ErrorResponse("Invalid user ID"));
                }

                _logger.LogInformation("Rejecting user: {UserId}", id);

                var user = await _userApiService.GetUserByIdAsync(id, true);
                if (user == null)
                {
                    return NotFound(new ErrorResponse("User not found"));
                }

                if (user.IsApproved)
                {
                    return BadRequest(new ErrorResponse("Cannot reject an already approved user"));
                }

                var result = await _userApiService.DeleteUserAsync(id);

                return result switch
                {
                    -1 => NotFound(new ErrorResponse("User not found")),
                    -999 => StatusCode(500, new ErrorResponse("An error occurred while rejecting the user")),
                    > 0 => Ok(new SuccessResponse($"User {user.Username} has been rejected and deleted successfully")),
                    _ => BadRequest(new ErrorResponse("No changes were made"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting user: {UserId}", id);
                return StatusCode(500, new ErrorResponse("An error occurred while rejecting the user"));
            }
        }
    }

    // DTOs for API responses and requests
    public record UserResponseDto
    {
        public int UserID { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public DateTime DateOfBirth { get; init; }
        public string? Gender { get; init; }
        public string Country { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string? StreetAddress { get; init; }
        public string? City { get; init; }
        public string? State { get; init; }
        public string? ZipCode { get; init; }
        public string? Bio { get; init; }
        public string? ReferralCode { get; init; }
        public bool ReceiveNewsletter { get; init; }
        public bool ReceiveSMS { get; init; }
        public DateTime? TermsAcceptedDate { get; init; }
        public DateTime CreatedDate { get; init; }
        public DateTime LastModified { get; init; }
        public bool IsActive { get; init; }
        public bool IsApproved { get; init; }
        public string FullName { get; init; } = string.Empty;
        public int Age { get; init; }

        public UserResponseDto() { }

        public UserResponseDto(User user)
        {
            UserID = user.UserID;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Username = user.Username;
            Email = user.Email;
            DateOfBirth = user.DateOfBirth;
            Gender = user.Gender;
            Country = user.Country;
            PhoneNumber = user.PhoneNumber;
            StreetAddress = user.StreetAddress;
            City = user.City;
            State = user.State;
            ZipCode = user.ZipCode;
            Bio = user.Bio;
            ReferralCode = user.ReferralCode;
            ReceiveNewsletter = user.ReceiveNewsletter;
            ReceiveSMS = user.ReceiveSMS;
            TermsAcceptedDate = user.TermsAcceptedDate;
            CreatedDate = user.CreatedDate;
            LastModified = user.LastModified;
            IsActive = user.IsActive;
            IsApproved = user.IsApproved;
            FullName = user.FullName;
            Age = user.Age;
        }
    }

    public record CreateUserDto
    {
        [Required, StringLength(50)]
        public string FirstName { get; init; } = string.Empty;

        [Required, StringLength(50)]
        public string LastName { get; init; } = string.Empty;

        [Required, StringLength(30)]
        public string Username { get; init; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; init; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 6)]
        public string Password { get; init; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; init; }

        [StringLength(10)]
        public string? Gender { get; init; }

        [Required, StringLength(50)]
        public string Country { get; init; } = string.Empty;

        [Required, StringLength(20)]
        public string PhoneNumber { get; init; } = string.Empty;

        [StringLength(200)]
        public string? StreetAddress { get; init; }

        [StringLength(50)]
        public string? City { get; init; }

        [StringLength(50)]
        public string? State { get; init; }

        [StringLength(20)]
        public string? ZipCode { get; init; }

        [Required, StringLength(200)]
        public string SecurityQuestion { get; init; } = string.Empty;

        [Required, StringLength(100)]
        public string SecurityAnswer { get; init; } = string.Empty;

        [StringLength(500)]
        public string? Bio { get; init; }

        [StringLength(20)]
        public string? ReferralCode { get; init; }

        public bool ReceiveNewsletter { get; init; } = false;
        public bool ReceiveSMS { get; init; } = false;
        public bool AcceptTerms { get; init; } = false;
    }

    public record UpdateUserDto
    {
        [Required, StringLength(50)]
        public string FirstName { get; init; } = string.Empty;

        [Required, StringLength(50)]
        public string LastName { get; init; } = string.Empty;

        [Required, StringLength(30)]
        public string Username { get; init; } = string.Empty;

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; init; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; init; }

        [StringLength(10)]
        public string? Gender { get; init; }

        [Required, StringLength(50)]
        public string Country { get; init; } = string.Empty;

        [Required, StringLength(20)]
        public string PhoneNumber { get; init; } = string.Empty;

        [StringLength(200)]
        public string? StreetAddress { get; init; }

        [StringLength(50)]
        public string? City { get; init; }

        [StringLength(50)]
        public string? State { get; init; }

        [StringLength(20)]
        public string? ZipCode { get; init; }

        [Required, StringLength(200)]
        public string SecurityQuestion { get; init; } = string.Empty;

        [Required, StringLength(100)]
        public string SecurityAnswer { get; init; } = string.Empty;

        [StringLength(500)]
        public string? Bio { get; init; }

        [StringLength(20)]
        public string? ReferralCode { get; init; }

        public bool ReceiveNewsletter { get; init; } = false;
        public bool ReceiveSMS { get; init; } = false;
        public bool IsActive { get; init; } = true;
    }

    public record UpdateUserWithIdDto
    {
        [Required]
        public int UserID { get; init; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; init; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; init; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Username { get; init; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; init; } = string.Empty;

        [Required]
        public DateTime DateOfBirth { get; init; }

        [StringLength(10)]
        public string? Gender { get; init; }

        [Required]
        [StringLength(50)]
        public string Country { get; init; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; init; } = string.Empty;

        [StringLength(200)]
        public string? StreetAddress { get; init; }

        [StringLength(50)]
        public string? City { get; init; }

        [StringLength(50)]
        public string? State { get; init; }

        [StringLength(20)]
        public string? ZipCode { get; init; }

        [Required]
        [StringLength(200)]
        public string SecurityQuestion { get; init; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string SecurityAnswer { get; init; } = string.Empty;

        [StringLength(500)]
        public string? Bio { get; init; }

        [StringLength(20)]
        public string? ReferralCode { get; init; }

        public bool ReceiveNewsletter { get; init; } = false;
        public bool ReceiveSMS { get; init; } = false;
        public bool IsActive { get; init; } = true;
    }

    public record LoginDto
    {
        [Required]
        public string UsernameOrEmail { get; init; } = string.Empty;

        [Required]
        public string Password { get; init; } = string.Empty;
    }

    public record ErrorResponse
    {
        public string Message { get; init; }
        public IEnumerable<string>? Errors { get; init; }
        public DateTime Timestamp { get; init; }

        public ErrorResponse(string message, IEnumerable<string>? errors = null)
        {
            Message = message;
            Errors = errors;
            Timestamp = DateTime.UtcNow;
        }
    }

    public record SuccessResponse
    {
        public string Message { get; init; }
        public DateTime Timestamp { get; init; }

        public SuccessResponse(string message)
        {
            Message = message;
            Timestamp = DateTime.UtcNow;
        }
    }

    public record UserCreatedResponse
    {
        public int UserID { get; init; }
        public string Message { get; init; }
        public DateTime Timestamp { get; init; }

        public UserCreatedResponse(int userId, string message)
        {
            UserID = userId;
            Message = message;
            Timestamp = DateTime.UtcNow;
        }
    }

    // Forgot Password DTOs
    public record ForgotPasswordRequestDto
    {
        [Required]
        public string UsernameOrEmail { get; init; } = string.Empty;
    }

    public record VerifySecurityAnswerDto
    {
        [Required]
        public int UserId { get; init; }

        [Required]
        public string SecurityAnswer { get; init; } = string.Empty;
    }

    public record ResetPasswordDto
    {
        [Required]
        public string ResetToken { get; init; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; init; } = string.Empty;
    }

    public record SecurityQuestionResponse
    {
        public string SecurityQuestion { get; init; } = string.Empty;
        public int UserId { get; init; }
    }

    public record SecurityVerificationResponse
    {
        public string ResetToken { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }

    // Simple in-memory cache for password reset tokens
    // In production, this should be stored in database or distributed cache
    public static class PasswordResetTokenCache
    {
        private static readonly Dictionary<string, (int UserId, DateTime Expiry)> _tokens = new();
        private static readonly TimeSpan _tokenExpiry = TimeSpan.FromMinutes(15);

        public static void AddToken(string token, int userId)
        {
            _tokens[token] = (userId, DateTime.UtcNow.Add(_tokenExpiry));
        }

        public static int? GetUserId(string token)
        {
            if (_tokens.TryGetValue(token, out var tokenInfo))
            {
                if (tokenInfo.Expiry > DateTime.UtcNow)
                {
                    return tokenInfo.UserId;
                }
                else
                {
                    // Token expired, remove it
                    _tokens.Remove(token);
                }
            }
            return null;
        }

        public static void RemoveToken(string token)
        {
            _tokens.Remove(token);
        }

        // Cleanup method to remove expired tokens
        public static void CleanupExpiredTokens()
        {
            var expiredTokens = _tokens.Where(kvp => kvp.Value.Expiry <= DateTime.UtcNow).Select(kvp => kvp.Key).ToList();
            foreach (var token in expiredTokens)
            {
                _tokens.Remove(token);
            }
        }
    }
}