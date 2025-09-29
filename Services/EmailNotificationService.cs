using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace RegistrationPortal.Services
{
    public interface IEmailNotificationService
    {
        Task<bool> SendPendingApprovalNotificationAsync(int pendingCount, DateTime oldestPendingDate);
        Task<bool> SendTestEmailAsync(string recipientEmail, string subject, string message);
    }

    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailNotificationService> _logger;
        private readonly IEventLoggerService _eventLogger;

        public EmailNotificationService(IConfiguration configuration, ILogger<EmailNotificationService> logger, IEventLoggerService eventLogger)
        {
            _configuration = configuration;
            _logger = logger;
            _eventLogger = eventLogger;
        }

        public async Task<bool> SendPendingApprovalNotificationAsync(int pendingCount, DateTime oldestPendingDate)
        {
            try
            {
                var adminEmail = _configuration["NotificationService:AdminEmail"] ?? "ebymathew142@gmail.com";
                var subject = $"Registration Portal: {pendingCount} User(s) Pending Approval";

                var timeWaiting = DateTime.Now - oldestPendingDate;
                var timeWaitingText = timeWaiting.Days > 0
                    ? $"{timeWaiting.Days} days"
                    : timeWaiting.Hours > 0
                        ? $"{timeWaiting.Hours} hours"
                        : $"{timeWaiting.Minutes} minutes";

                var message = $@"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; border-radius: 8px; color: white; margin-bottom: 20px;'>
        <h2 style='margin: 0; text-align: center;'>🔔 Registration Portal Notification</h2>
    </div>

    <div style='padding: 20px; background: #f8f9fa; border-radius: 8px; margin-bottom: 20px;'>
        <h3 style='color: #dc3545; margin-top: 0;'>⚠️ Pending User Approvals Required</h3>
        <p><strong>Number of pending users:</strong> {pendingCount}</p>
        <p><strong>Oldest pending registration:</strong> {oldestPendingDate:MMMM dd, yyyy 'at' h:mm tt}</p>
        <p><strong>Time waiting:</strong> {timeWaitingText}</p>
    </div>

    <div style='background: #e9ecef; padding: 15px; border-radius: 8px; margin-bottom: 20px;'>
        <h4 style='margin-top: 0; color: #495057;'>📋 Action Required:</h4>
        <p>Please log in to the admin dashboard to review and approve pending user registrations.</p>

        <div style='text-align: center; margin: 20px 0;'>
            <a href='http://registrationportal.local/Admin'
               style='display: inline-block; background: linear-gradient(45deg, #28a745, #20c997);
                      color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px;
                      font-weight: bold;'>
                🔗 Access Admin Dashboard
            </a>
        </div>
    </div>

    <div style='background: #d1ecf1; border: 1px solid #bee5eb; padding: 15px; border-radius: 8px; margin-bottom: 20px;'>
        <h4 style='margin-top: 0; color: #0c5460;'>ℹ️ System Information:</h4>
        <p><strong>Server:</strong> {Environment.MachineName}</p>
        <p><strong>Database:</strong> Registration Portal Database</p>
        <p><strong>Notification sent:</strong> {DateTime.Now:MMMM dd, yyyy 'at' h:mm tt}</p>
    </div>

    <hr style='border: none; border-top: 1px solid #dee2e6; margin: 30px 0;'>

    <div style='text-align: center; color: #6c757d; font-size: 14px;'>
        <p>This is an automated notification from the Registration Portal System.</p>
        <p>To modify notification settings, please contact your system administrator.</p>
        <p style='margin-top: 20px;'>
            <strong>Registration Portal</strong><br>
            Automated Notification Service
        </p>
    </div>
</body>
</html>";

                return await SendEmailAsync(adminEmail, subject, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send pending approval notification");
                _eventLogger.LogUserAction("Email Notification Error", "System",
                    $"Failed to send pending approval notification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendTestEmailAsync(string recipientEmail, string subject, string message)
        {
            try
            {
                var htmlMessage = $@"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 20px; border-radius: 8px; color: white; margin-bottom: 20px;'>
        <h2 style='margin: 0; text-align: center;'>✅ Test Email Notification</h2>
    </div>

    <div style='padding: 20px; background: #f8f9fa; border-radius: 8px; margin-bottom: 20px;'>
        <h3 style='color: #28a745; margin-top: 0;'>📧 Email Service Test</h3>
        <p><strong>Message:</strong> {message}</p>
        <p><strong>Sent to:</strong> {recipientEmail}</p>
        <p><strong>Sent at:</strong> {DateTime.Now:MMMM dd, yyyy 'at' h:mm tt}</p>
        <p><strong>Server:</strong> {Environment.MachineName}</p>
    </div>

    <div style='background: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 8px;'>
        <p style='margin: 0; color: #155724;'><strong>✅ Success!</strong> The email notification service is working correctly.</p>
    </div>
</body>
</html>";

                return await SendEmailAsync(recipientEmail, subject, htmlMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test email to {EmailAddress}", recipientEmail);
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string recipientEmail, string subject, string htmlBody)
        {
            try
            {
                // Get SMTP configuration from appsettings
                var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:Username"] ?? "";
                var smtpPassword = _configuration["EmailSettings:Password"] ?? "";
                var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUsername;
                var fromName = _configuration["EmailSettings:FromName"] ?? "Registration Portal System";
                var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

                if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("SMTP credentials not configured. Email notification skipped.");
                    _eventLogger.LogUserAction("Email Configuration Warning", "System",
                        "SMTP credentials not configured in appsettings");
                    return false;
                }

                using var client = new SmtpClient(smtpHost, smtpPort);
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                client.EnableSsl = enableSsl;

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail, fromName);
                message.To.Add(recipientEmail);
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;
                message.Priority = MailPriority.High;

                await client.SendMailAsync(message);

                _logger.LogInformation("Email notification sent successfully to {EmailAddress}", recipientEmail);
                _eventLogger.LogUserAction("Email Sent", "System",
                    $"Notification email sent to {recipientEmail} with subject: {subject}");

                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error sending email to {EmailAddress}", recipientEmail);
                _eventLogger.LogUserAction("Email SMTP Error", "System",
                    $"SMTP error sending email to {recipientEmail}: {smtpEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error sending email to {EmailAddress}", recipientEmail);
                _eventLogger.LogUserAction("Email General Error", "System",
                    $"Error sending email to {recipientEmail}: {ex.Message}");
                return false;
            }
        }
    }
}