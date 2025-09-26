using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace RegistrationPortal.Services
{
    public interface IDatabaseLoggerService
    {
        Task LogAsync(string logLevel, string eventType, string message,
            string? details = null, string? username = null, string? ipAddress = null,
            string? userAgent = null, string? requestPath = null, string? httpMethod = null,
            int? statusCode = null, int? duration = null, Exception? exception = null,
            object? additionalData = null);

        Task LogInformationAsync(string eventType, string message, string? details = null,
            string? username = null, string? ipAddress = null, object? additionalData = null);

        Task LogWarningAsync(string eventType, string message, string? details = null,
            string? username = null, string? ipAddress = null, object? additionalData = null);

        Task LogErrorAsync(string eventType, string message, Exception? exception = null,
            string? details = null, string? username = null, string? ipAddress = null, object? additionalData = null);

        Task LogApiCallAsync(string method, string path, int statusCode, int duration,
            string? username = null, string? ipAddress = null, string? userAgent = null,
            string? details = null, Exception? exception = null);

        Task<IEnumerable<ApiLogEntry>> GetRecentLogsAsync(int count = 100, string? logLevel = null,
            string? eventType = null, DateTime? fromDate = null);

        Task CleanupOldLogsAsync(int daysToKeep = 30);
    }

    public class DatabaseLoggerService : IDatabaseLoggerService
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseLoggerService> _logger;

        public DatabaseLoggerService(IConfiguration configuration, ILogger<DatabaseLoggerService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentException("DefaultConnection string is required");
            _logger = logger;
        }

        public async Task LogAsync(string logLevel, string eventType, string message,
            string? details = null, string? username = null, string? ipAddress = null,
            string? userAgent = null, string? requestPath = null, string? httpMethod = null,
            int? statusCode = null, int? duration = null, Exception? exception = null,
            object? additionalData = null)
        {
            try
            {
                const string sql = @"
                    INSERT INTO ApiLogs (
                        LogLevel, EventType, Message, Details, Username, IPAddress,
                        UserAgent, RequestPath, HttpMethod, StatusCode, Duration,
                        ExceptionDetails, AdditionalData, CreatedDate
                    ) VALUES (
                        @LogLevel, @EventType, @Message, @Details, @Username, @IPAddress,
                        @UserAgent, @RequestPath, @HttpMethod, @StatusCode, @Duration,
                        @ExceptionDetails, @AdditionalData, @CreatedDate
                    )";

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);

                command.Parameters.AddWithValue("@LogLevel", logLevel);
                command.Parameters.AddWithValue("@EventType", eventType);
                command.Parameters.AddWithValue("@Message", message);
                command.Parameters.AddWithValue("@Details", (object?)details ?? DBNull.Value);
                command.Parameters.AddWithValue("@Username", (object?)username ?? DBNull.Value);
                command.Parameters.AddWithValue("@IPAddress", (object?)ipAddress ?? DBNull.Value);
                command.Parameters.AddWithValue("@UserAgent", (object?)userAgent ?? DBNull.Value);
                command.Parameters.AddWithValue("@RequestPath", (object?)requestPath ?? DBNull.Value);
                command.Parameters.AddWithValue("@HttpMethod", (object?)httpMethod ?? DBNull.Value);
                command.Parameters.AddWithValue("@StatusCode", (object?)statusCode ?? DBNull.Value);
                command.Parameters.AddWithValue("@Duration", (object?)duration ?? DBNull.Value);
                command.Parameters.AddWithValue("@ExceptionDetails", exception?.ToString() ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AdditionalData",
                    additionalData != null ? JsonSerializer.Serialize(additionalData) : (object)DBNull.Value);
                command.Parameters.AddWithValue("@CreatedDate", DateTime.UtcNow);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log to database: {Message}", message);
                // Don't rethrow to prevent logging failures from breaking the application
            }
        }

        public async Task LogInformationAsync(string eventType, string message, string? details = null,
            string? username = null, string? ipAddress = null, object? additionalData = null)
        {
            await LogAsync("Information", eventType, message, details, username, ipAddress,
                additionalData: additionalData);
        }

        public async Task LogWarningAsync(string eventType, string message, string? details = null,
            string? username = null, string? ipAddress = null, object? additionalData = null)
        {
            await LogAsync("Warning", eventType, message, details, username, ipAddress,
                additionalData: additionalData);
        }

        public async Task LogErrorAsync(string eventType, string message, Exception? exception = null,
            string? details = null, string? username = null, string? ipAddress = null, object? additionalData = null)
        {
            await LogAsync("Error", eventType, message, details, username, ipAddress,
                exception: exception, additionalData: additionalData);
        }

        public async Task LogApiCallAsync(string method, string path, int statusCode, int duration,
            string? username = null, string? ipAddress = null, string? userAgent = null,
            string? details = null, Exception? exception = null)
        {
            var logLevel = statusCode >= 500 ? "Error" : statusCode >= 400 ? "Warning" : "Information";
            var eventType = "ApiCall";
            var message = $"{method} {path} returned {statusCode} in {duration}ms";

            await LogAsync(logLevel, eventType, message, details, username, ipAddress,
                userAgent, path, method, statusCode, duration, exception);
        }

        public async Task<IEnumerable<ApiLogEntry>> GetRecentLogsAsync(int count = 100, string? logLevel = null,
            string? eventType = null, DateTime? fromDate = null)
        {
            try
            {
                var sql = @"
                    SELECT TOP (@Count)
                        LogID, LogLevel, EventType, Message, Details, Username, IPAddress,
                        UserAgent, RequestPath, HttpMethod, StatusCode, Duration,
                        ExceptionDetails, AdditionalData, CreatedDate
                    FROM ApiLogs
                    WHERE (@LogLevel IS NULL OR LogLevel = @LogLevel)
                        AND (@EventType IS NULL OR EventType = @EventType)
                        AND (@FromDate IS NULL OR CreatedDate >= @FromDate)
                    ORDER BY CreatedDate DESC";

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);

                command.Parameters.AddWithValue("@Count", count);
                command.Parameters.AddWithValue("@LogLevel", (object?)logLevel ?? DBNull.Value);
                command.Parameters.AddWithValue("@EventType", (object?)eventType ?? DBNull.Value);
                command.Parameters.AddWithValue("@FromDate", (object?)fromDate ?? DBNull.Value);

                await connection.OpenAsync();
                using var reader = await command.ExecuteReaderAsync();

                var logs = new List<ApiLogEntry>();
                while (await reader.ReadAsync())
                {
                    logs.Add(new ApiLogEntry
                    {
                        LogID = reader.GetInt32("LogID"),
                        LogLevel = reader.GetString("LogLevel"),
                        EventType = reader.GetString("EventType"),
                        Message = reader.GetString("Message"),
                        Details = reader.IsDBNull("Details") ? null : reader.GetString("Details"),
                        Username = reader.IsDBNull("Username") ? null : reader.GetString("Username"),
                        IPAddress = reader.IsDBNull("IPAddress") ? null : reader.GetString("IPAddress"),
                        UserAgent = reader.IsDBNull("UserAgent") ? null : reader.GetString("UserAgent"),
                        RequestPath = reader.IsDBNull("RequestPath") ? null : reader.GetString("RequestPath"),
                        HttpMethod = reader.IsDBNull("HttpMethod") ? null : reader.GetString("HttpMethod"),
                        StatusCode = reader.IsDBNull("StatusCode") ? null : reader.GetInt32("StatusCode"),
                        Duration = reader.IsDBNull("Duration") ? null : reader.GetInt32("Duration"),
                        ExceptionDetails = reader.IsDBNull("ExceptionDetails") ? null : reader.GetString("ExceptionDetails"),
                        AdditionalData = reader.IsDBNull("AdditionalData") ? null : reader.GetString("AdditionalData"),
                        CreatedDate = reader.GetDateTime("CreatedDate")
                    });
                }

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve logs from database");
                return new List<ApiLogEntry>();
            }
        }

        public async Task CleanupOldLogsAsync(int daysToKeep = 30)
        {
            try
            {
                const string sql = "EXEC CleanupApiLogs @DaysToKeep";

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(sql, connection);

                command.Parameters.AddWithValue("@DaysToKeep", daysToKeep);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Successfully cleaned up API logs older than {Days} days", daysToKeep);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old API logs");
            }
        }
    }

    public class ApiLogEntry
    {
        public int LogID { get; set; }
        public string LogLevel { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? Username { get; set; }
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? RequestPath { get; set; }
        public string? HttpMethod { get; set; }
        public int? StatusCode { get; set; }
        public int? Duration { get; set; }
        public string? ExceptionDetails { get; set; }
        public string? AdditionalData { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}