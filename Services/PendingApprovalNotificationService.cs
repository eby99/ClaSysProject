using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace RegistrationPortal.Services
{
    public class PendingApprovalNotificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<PendingApprovalNotificationService> _logger;
        private readonly IConfiguration _configuration;
        private Timer? _timer;

        public PendingApprovalNotificationService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<PendingApprovalNotificationService> logger,
            IConfiguration configuration)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Pending Approval Notification Service started at: {Time}", DateTimeOffset.Now);

            // Get configuration values
            var checkIntervalMinutes = GetConfigValue("NotificationService:CheckIntervalMinutes", 60); // Check every hour by default
            var notificationThresholdHours = GetConfigValue("NotificationService:NotificationThresholdHours", 24); // Notify after 1 day by default
            var enabled = GetConfigValue("NotificationService:Enabled", true);

            if (!enabled)
            {
                _logger.LogInformation("Pending Approval Notification Service is disabled in configuration");
                return;
            }

            _logger.LogInformation("Service Configuration: Check Interval: {CheckInterval} minutes, Threshold: {Threshold} hours",
                checkIntervalMinutes, notificationThresholdHours);

            // Create timer that runs every configured interval
            var interval = TimeSpan.FromMinutes(checkIntervalMinutes);
            _timer = new Timer(async _ => await CheckPendingApprovals(notificationThresholdHours),
                null, TimeSpan.Zero, interval);

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task CheckPendingApprovals(int thresholdHours)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
                var eventLogger = scope.ServiceProvider.GetRequiredService<IEventLoggerService>();

                _logger.LogDebug("Checking for pending approvals...");

                // Get all unapproved users
                var unapprovedUsers = await userService.GetUnapprovedUsersAsync();

                if (unapprovedUsers?.Any() != true)
                {
                    _logger.LogDebug("No pending approvals found");
                    return;
                }

                // Filter users that have been pending longer than threshold
                var thresholdTime = DateTime.Now.AddHours(-thresholdHours);
                var overdueUsers = unapprovedUsers.Where(u => u.CreatedDate <= thresholdTime).ToList();

                if (overdueUsers.Any())
                {
                    var oldestUser = overdueUsers.OrderBy(u => u.CreatedDate).First();
                    var overdue = DateTime.Now - oldestUser.CreatedDate;

                    _logger.LogInformation("Found {Count} users pending approval for more than {ThresholdHours} hours. Oldest: {OldestDate}",
                        overdueUsers.Count, thresholdHours, oldestUser.CreatedDate);

                    // Check if we've already sent a notification recently for this oldest user
                    var lastNotificationKey = $"LastNotification_{oldestUser.UserID}";
                    var lastNotificationTime = GetLastNotificationTime(lastNotificationKey);
                    var notificationIntervalHours = GetConfigValue("NotificationService:NotificationIntervalHours", 24); // Don't spam - send max once per day

                    if (lastNotificationTime.HasValue &&
                        DateTime.Now - lastNotificationTime.Value < TimeSpan.FromHours(notificationIntervalHours))
                    {
                        _logger.LogDebug("Notification already sent recently for oldest pending user (ID: {UserId}). Skipping.",
                            oldestUser.UserID);
                        return;
                    }

                    // Send notification
                    var success = await emailService.SendPendingApprovalNotificationAsync(overdueUsers.Count, oldestUser.CreatedDate);

                    if (success)
                    {
                        // Record that we sent the notification
                        SetLastNotificationTime(lastNotificationKey, DateTime.Now);

                        _logger.LogInformation("Notification sent successfully for {Count} pending users", overdueUsers.Count);
                        eventLogger.LogUserAction("Pending Approval Notification", "NotificationService",
                            $"Email notification sent for {overdueUsers.Count} users pending approval. Oldest pending: {oldestUser.CreatedDate:yyyy-MM-dd HH:mm}");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to send notification for pending approvals");
                        eventLogger.LogUserAction("Notification Failed", "NotificationService",
                            $"Failed to send email notification for {overdueUsers.Count} pending users");
                    }
                }
                else
                {
                    _logger.LogDebug("Found {TotalCount} pending users, but none are overdue (threshold: {ThresholdHours} hours)",
                        unapprovedUsers.Count(), thresholdHours);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking pending approvals");

                using var scope = _serviceScopeFactory.CreateScope();
                var eventLogger = scope.ServiceProvider.GetRequiredService<IEventLoggerService>();
                eventLogger.LogUserAction("Notification Service Error", "NotificationService",
                    $"Error checking pending approvals: {ex.Message}");
            }
        }

        private T GetConfigValue<T>(string key, T defaultValue)
        {
            try
            {
                var value = _configuration[key];
                if (string.IsNullOrEmpty(value))
                    return defaultValue;

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        private DateTime? GetLastNotificationTime(string key)
        {
            try
            {
                var value = _configuration[$"NotificationService:LastNotifications:{key}"];
                if (DateTime.TryParse(value, out var result))
                    return result;
            }
            catch { }

            return null;
        }

        private void SetLastNotificationTime(string key, DateTime time)
        {
            try
            {
                // In a real implementation, this would persist to a database or file
                // For now, we'll just log it
                _logger.LogDebug("Last notification time set for {Key}: {Time}", key, time);

                // You could implement persistence here, for example:
                // - Save to a simple JSON file
                // - Save to database
                // - Use IMemoryCache with sliding expiration
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist last notification time for key: {Key}", key);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Pending Approval Notification Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);
            _timer?.Dispose();

            await base.StopAsync(stoppingToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }
}