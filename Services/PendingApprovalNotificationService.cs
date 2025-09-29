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
            _logger.LogInformation("🚀 Pending Approval Notification Service started at: {Time}", DateTimeOffset.Now);

            // Get configuration values
            var checkIntervalMinutes = GetConfigValue("NotificationService:CheckIntervalMinutes", 60); // Check every hour by default
            var notificationThresholdHours = GetConfigValue("NotificationService:NotificationThresholdHours", 24); // Notify after 1 day by default
            var enabled = GetConfigValue("NotificationService:Enabled", true);
            var debugMode = Environment.GetEnvironmentVariable("DEBUG_EMAIL_SERVICE") == "true";

            if (!enabled)
            {
                _logger.LogInformation("⚠️ Pending Approval Notification Service is disabled in configuration");
                return;
            }

            if (debugMode)
            {
                _logger.LogWarning("🐛 DEBUG MODE: Email Service running in debug mode with enhanced logging");
                checkIntervalMinutes = Math.Max(1, checkIntervalMinutes); // Minimum 1 minute in debug mode
            }

            _logger.LogInformation("⚙️ Service Configuration: Check Interval: {CheckInterval} minutes, Threshold: {Threshold} hours, Debug: {DebugMode}",
                checkIntervalMinutes, notificationThresholdHours, debugMode);

            // Create timer that runs every configured interval
            var interval = TimeSpan.FromMinutes(checkIntervalMinutes);
            _timer = new Timer(async _ => await CheckPendingApprovals(notificationThresholdHours, debugMode),
                null, TimeSpan.Zero, interval);

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task CheckPendingApprovals(int thresholdHours, bool debugMode = false)
        {
            try
            {
                if (debugMode) _logger.LogWarning("🔍 DEBUG: Starting pending approvals check at {Time}", DateTime.Now);

                using var scope = _serviceScopeFactory.CreateScope();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
                var eventLogger = scope.ServiceProvider.GetRequiredService<IEventLoggerService>();

                _logger.LogDebug("Checking for pending approvals...");

                // Get all unapproved users
                var unapprovedUsers = await userService.GetUnapprovedUsersAsync();

                if (debugMode) _logger.LogWarning("🔍 DEBUG: Found {Count} unapproved users", unapprovedUsers?.Count() ?? 0);

                if (unapprovedUsers?.Any() != true)
                {
                    _logger.LogDebug("No pending approvals found");
                    if (debugMode) _logger.LogWarning("🔍 DEBUG: No pending approvals - check completed");
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

                    // Check if we've already sent a notification recently (check global, not per user)
                    var lastNotificationKey = "LastNotification_Global";
                    var lastNotificationTime = GetLastNotificationTime(lastNotificationKey);
                    var notificationIntervalHours = GetConfigValue("NotificationService:NotificationIntervalHours", 120); // Default to 5 days

                    if (lastNotificationTime.HasValue &&
                        DateTime.Now - lastNotificationTime.Value < TimeSpan.FromHours(notificationIntervalHours))
                    {
                        _logger.LogDebug("Notification already sent recently (last sent: {LastTime}). Skipping until {NextTime}.",
                            lastNotificationTime.Value, lastNotificationTime.Value.AddHours(notificationIntervalHours));
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
                // Write to appsettings.json to persist notification times
                var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (!config.ContainsKey("NotificationService"))
                        config["NotificationService"] = new Dictionary<string, object>();

                    var notificationService = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(config["NotificationService"].ToString());

                    if (!notificationService.ContainsKey("LastNotifications"))
                        notificationService["LastNotifications"] = new Dictionary<string, object>();

                    var lastNotifications = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(notificationService["LastNotifications"].ToString());
                    lastNotifications[key] = time.ToString("yyyy-MM-ddTHH:mm:ss");

                    notificationService["LastNotifications"] = lastNotifications;
                    config["NotificationService"] = notificationService;

                    var updatedJson = System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(configPath, updatedJson);

                    _logger.LogDebug("Saved last notification time for key: {Key} at {Time}", key, time);
                }
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