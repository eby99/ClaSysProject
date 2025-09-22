using System.ComponentModel.DataAnnotations;

namespace RegistrationPortal.ViewModels
{
    public class RegistrationViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required")]
        [StringLength(30, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(128, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [StringLength(50)]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }

        [Required(ErrorMessage = "Security question is required")]
        [StringLength(200)]
        public string SecurityQuestion { get; set; } = string.Empty;

        [Required(ErrorMessage = "Security answer is required")]
        [StringLength(100)]
        public string SecurityAnswer { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Bio { get; set; }
        
        [StringLength(20)]
        public string? ReferralCode { get; set; }
        
        public bool ReceiveNewsletter { get; set; }
        public bool ReceiveSMS { get; set; }

        [Required(ErrorMessage = "You must accept the terms and conditions")]
        public bool AcceptTerms { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username or email is required")]
        public string UsernameEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Admin username is required")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Admin password is required")]
        public string Password { get; set; } = string.Empty;
    }

    public class EditUserViewModel
    {
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(50)]
        public string? Country { get; set; }

        [Phone]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [StringLength(200)]
        [Display(Name = "Street Address")]
        public string? StreetAddress { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(50)]
        [Display(Name = "State/Province")]
        public string? State { get; set; }

        [StringLength(20)]
        [Display(Name = "ZIP/Postal Code")]
        public string? ZipCode { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        [Display(Name = "Account Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Receive Newsletter")]
        public bool ReceiveNewsletter { get; set; }

        [Display(Name = "Receive SMS")]
        public bool ReceiveSMS { get; set; }
    }

    public class UserProfileViewModel
    {
        public int UserID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Country { get; set; }
        public string? PhoneNumber { get; set; }
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Bio { get; set; }
        public string? ReferralCode { get; set; }
        public bool ReceiveNewsletter { get; set; }
        public bool ReceiveSMS { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }

        public string FullName => $"{FirstName} {LastName}";
        public int Age => DateTime.Now.Year - DateOfBirth.Year;
        public int DaysSinceMember => (DateTime.Now - CreatedDate).Days;
    }

    public class AdminDashboardViewModel
    {
        public DashboardStats Stats { get; set; } = new();
        public IEnumerable<UserListItemViewModel> Users { get; set; } = new List<UserListItemViewModel>();
        public string SearchTerm { get; set; } = string.Empty;
    }

    public class UserListItemViewModel
    {
        public int UserID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Country { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public bool ReceiveNewsletter { get; set; }

        public string FullName => $"{FirstName} {LastName}";
    }

    public class DashboardStats
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersToday { get; set; }
        public int RecentUsers { get; set; }
        public int NewsletterSubscribers { get; set; }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}