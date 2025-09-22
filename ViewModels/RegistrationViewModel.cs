using System;
using System.ComponentModel.DataAnnotations;

namespace RegistrationPortal.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [StringLength(50)]
        public required string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public required string LastName { get; set; }

        [Required]
        [StringLength(30, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9_]{2,29}$")]
        public required string Username { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        [Required]
        public required string Country { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public required string Email { get; set; }

        [Required]
        [Phone]
        [StringLength(20)]
        public required string PhoneNumber { get; set; }

        [StringLength(200)]
        public string? StreetAddress { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? ZipCode { get; set; }

        [Required]
        [StringLength(100)]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required]
        [StringLength(100)]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public required string ConfirmPassword { get; set; }

        [Required]
        public required string SecurityQuestion { get; set; }

        [Required]
        [StringLength(100)]
        public required string SecurityAnswer { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        [StringLength(20)]
        public string? ReferralCode { get; set; }

        public bool ReceiveNewsletter { get; set; }
        public bool ReceiveSMS { get; set; }

        [Required]
        public bool TermsAccepted { get; set; }
    }
}