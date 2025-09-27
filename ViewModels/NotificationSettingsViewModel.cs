using System.ComponentModel.DataAnnotations;

namespace RegistrationPortal.ViewModels
{
    public class NotificationSettingsViewModel
    {
        [Display(Name = "Enable Notifications")]
        public bool Enabled { get; set; } = true;

        [Display(Name = "Admin Email Address")]
        [Required]
        [EmailAddress]
        public string AdminEmail { get; set; } = "ebymathew142@gmail.com";

        [Display(Name = "Check Interval (Minutes)")]
        [Range(15, 1440, ErrorMessage = "Check interval must be between 15 minutes and 24 hours")]
        public int CheckIntervalMinutes { get; set; } = 60;

        [Display(Name = "Notification Threshold (Hours)")]
        [Range(1, 168, ErrorMessage = "Threshold must be between 1 hour and 7 days")]
        public int NotificationThresholdHours { get; set; } = 24;

        [Display(Name = "Notification Interval (Hours)")]
        [Range(1, 72, ErrorMessage = "Notification interval must be between 1 and 72 hours")]
        public int NotificationIntervalHours { get; set; } = 24;

        [Display(Name = "SMTP Host")]
        public string SmtpHost { get; set; } = "smtp.gmail.com";

        [Display(Name = "SMTP Port")]
        [Range(1, 65535)]
        public int SmtpPort { get; set; } = 587;

        [Display(Name = "SMTP Username")]
        public string SmtpUsername { get; set; } = "ebymathew142@gmail.com";

        [Display(Name = "SMTP Password")]
        [DataType(DataType.Password)]
        public string SmtpPassword { get; set; } = "gbjx maui jido iyrf";

        [Display(Name = "From Email")]
        [EmailAddress]
        public string FromEmail { get; set; } = "ebymathew142@gmail.com";

        [Display(Name = "From Name")]
        public string FromName { get; set; } = "Registration Portal System";

        [Display(Name = "Enable SSL")]
        public bool EnableSsl { get; set; } = true;

        public string? StatusMessage { get; set; }
        public bool? TestEmailSuccess { get; set; }
    }
}