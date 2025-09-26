using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace RegistrationPortal.Services
{
    public interface IEventLoggerService
    {
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, Exception? exception = null, params object[] args);
        void LogCritical(string message, Exception? exception = null, params object[] args);
        void LogUserAction(string action, string? username = null, string? details = null);
        void LogSecurityEvent(string eventType, string? username = null, string? ipAddress = null, string? details = null);
    }

    public class EventLoggerService : IEventLoggerService
    {
        private readonly ILogger<EventLoggerService> _logger;
        private readonly string _sourceName = "RegistrationPortal";
        private readonly string _logName = "Application";
        private bool _eventSourceExists;

        public EventLoggerService(ILogger<EventLoggerService> logger)
        {
            _logger = logger;
            _eventSourceExists = EnsureEventSourceExists();
        }

        private bool EnsureEventSourceExists()
        {
            try
            {
                // Only create event source on Windows
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _logger.LogWarning("Event logging to Windows Event Viewer is only available on Windows platform");
                    return false;
                }

                if (!EventLog.SourceExists(_sourceName))
                {
                    EventLog.CreateEventSource(_sourceName, _logName);
                    _logger.LogInformation("Created event source: {SourceName}", _sourceName);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create event source: {SourceName}. Make sure the application has administrator privileges or the event source already exists.", _sourceName);
                return false;
            }
        }

        public void LogInformation(string message, params object[] args)
        {
            var formattedMessage = string.Format(message, args);
            _logger.LogInformation(formattedMessage);
            WriteToEventLog(formattedMessage, EventLogEntryType.Information);
        }

        public void LogWarning(string message, params object[] args)
        {
            var formattedMessage = string.Format(message, args);
            _logger.LogWarning(formattedMessage);
            WriteToEventLog(formattedMessage, EventLogEntryType.Warning);
        }

        public void LogError(string message, Exception? exception = null, params object[] args)
        {
            var formattedMessage = string.Format(message, args);
            if (exception != null)
            {
                formattedMessage += $"\n\nException Details:\n{exception}";
            }

            _logger.LogError(exception, formattedMessage);
            WriteToEventLog(formattedMessage, EventLogEntryType.Error);
        }

        public void LogCritical(string message, Exception? exception = null, params object[] args)
        {
            var formattedMessage = string.Format(message, args);
            if (exception != null)
            {
                formattedMessage += $"\n\nException Details:\n{exception}";
            }

            _logger.LogCritical(exception, formattedMessage);
            WriteToEventLog(formattedMessage, EventLogEntryType.Error);
        }

        public void LogUserAction(string action, string? username = null, string? details = null)
        {
            var message = $"User Action: {action}";
            if (!string.IsNullOrEmpty(username))
                message += $"\nUser: {username}";
            if (!string.IsNullOrEmpty(details))
                message += $"\nDetails: {details}";

            message += $"\nTimestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            _logger.LogInformation("User Action - {Action} by {Username}: {Details}", action, username ?? "Anonymous", details ?? "N/A");
            WriteToEventLog(message, EventLogEntryType.Information, 1001); // Custom event ID for user actions
        }

        public void LogSecurityEvent(string eventType, string? username = null, string? ipAddress = null, string? details = null)
        {
            var message = $"Security Event: {eventType}";
            if (!string.IsNullOrEmpty(username))
                message += $"\nUser: {username}";
            if (!string.IsNullOrEmpty(ipAddress))
                message += $"\nIP Address: {ipAddress}";
            if (!string.IsNullOrEmpty(details))
                message += $"\nDetails: {details}";

            message += $"\nTimestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            _logger.LogWarning("Security Event - {EventType} for {Username} from {IPAddress}: {Details}",
                eventType, username ?? "Unknown", ipAddress ?? "Unknown", details ?? "N/A");
            WriteToEventLog(message, EventLogEntryType.Warning, 2001); // Custom event ID for security events
        }

        [SupportedOSPlatform("windows")]
        private void WriteToEventLog(string message, EventLogEntryType entryType, int eventId = 0)
        {
            if (!_eventSourceExists)
                return;

            try
            {
                using var eventLog = new EventLog(_logName);
                eventLog.Source = _sourceName;
                eventLog.WriteEntry(message, entryType, eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to event log: {Message}", message);
            }
        }
    }
}