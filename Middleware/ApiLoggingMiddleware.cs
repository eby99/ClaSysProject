using RegistrationPortal.Services;
using System.Diagnostics;
using System.Text;

namespace RegistrationPortal.Middleware
{
    public class ApiLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiLoggingMiddleware> _logger;

        public ApiLoggingMiddleware(RequestDelegate next, ILogger<ApiLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only log API calls, not static files or regular web pages
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;

            Exception? caughtException = null;
            var responseStatusCode = 200;

            try
            {
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                await _next(context);

                responseStatusCode = context.Response.StatusCode;

                // Copy the response back to the original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                caughtException = ex;
                responseStatusCode = 500;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                context.Response.Body = originalBodyStream;

                // Log the API call
                await LogApiCallAsync(context, responseStatusCode, (int)stopwatch.ElapsedMilliseconds, caughtException);
            }
        }

        private async Task LogApiCallAsync(HttpContext context, int statusCode, int duration, Exception? exception)
        {
            try
            {
                var request = context.Request;
                var user = context.User;

                var username = user.Identity?.IsAuthenticated == true
                    ? user.Identity.Name ?? user.FindFirst("username")?.Value
                    : null;

                var ipAddress = GetClientIpAddress(context);
                var userAgent = request.Headers.UserAgent.FirstOrDefault();

                var details = new StringBuilder();
                if (request.QueryString.HasValue)
                {
                    details.AppendLine($"Query: {request.QueryString}");
                }

                // Add request headers for debugging (be careful with sensitive data)
                if (request.Headers.ContainsKey("Content-Type"))
                {
                    details.AppendLine($"Content-Type: {request.Headers.ContentType}");
                }

                if (request.ContentLength.HasValue)
                {
                    details.AppendLine($"Content-Length: {request.ContentLength}");
                }

                // Get the database logger from the request scope
                var databaseLogger = context.RequestServices.GetService<IDatabaseLoggerService>();
                if (databaseLogger != null)
                {
                    await databaseLogger.LogApiCallAsync(
                        method: request.Method,
                        path: request.Path + request.QueryString,
                        statusCode: statusCode,
                        duration: duration,
                        username: username,
                        ipAddress: ipAddress,
                        userAgent: userAgent,
                        details: details.Length > 0 ? details.ToString().Trim() : null,
                        exception: exception
                    );
                }

                // Also log to regular logger for immediate visibility
                if (exception != null)
                {
                    _logger.LogError(exception, "API call failed: {Method} {Path} - {StatusCode} in {Duration}ms",
                        request.Method, request.Path, statusCode, duration);
                }
                else if (statusCode >= 400)
                {
                    _logger.LogWarning("API call warning: {Method} {Path} - {StatusCode} in {Duration}ms",
                        request.Method, request.Path, statusCode, duration);
                }
                else
                {
                    _logger.LogInformation("API call: {Method} {Path} - {StatusCode} in {Duration}ms",
                        request.Method, request.Path, statusCode, duration);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log API call");
                // Don't rethrow to prevent logging failures from breaking the API
            }
        }

        private static string GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded IP first (in case of load balancer/proxy)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(forwardedFor))
            {
                return forwardedFor.Split(',').FirstOrDefault()?.Trim() ?? context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }

    // Extension method to make it easier to register the middleware
    public static class ApiLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiLoggingMiddleware>();
        }
    }
}