using System.Net;
using System.Text.Json;
using RegistrationPortal.Controllers.Api;

namespace RegistrationPortal.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log the full exception details for developers
            _logger.LogError(exception,
                "Unhandled exception occurred. RequestId: {RequestId}, Path: {Path}, Method: {Method}, UserAgent: {UserAgent}, RemoteIP: {RemoteIP}",
                context.TraceIdentifier,
                context.Request.Path,
                context.Request.Method,
                context.Request.Headers.UserAgent.ToString(),
                context.Connection.RemoteIpAddress?.ToString());

            // Determine if this is an API request
            bool isApiRequest = context.Request.Path.StartsWithSegments("/api");

            if (isApiRequest)
            {
                await HandleApiException(context, exception);
            }
            else
            {
                await HandleWebException(context, exception);
            }
        }

        private async Task HandleApiException(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = GetApiErrorResponse(exception);
            context.Response.StatusCode = (int)statusCode;

            var response = new ErrorResponse(
                message,
                _environment.IsDevelopment() ? new[] { exception.ToString() } : null
            );

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = null, // Keep PascalCase
                WriteIndented = true
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private async Task HandleWebException(HttpContext context, Exception exception)
        {
            // For web requests, redirect to error page or return a simple error response
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/html";

            var errorHtml = _environment.IsDevelopment()
                ? GenerateDetailedErrorHtml(exception, context)
                : GenerateSimpleErrorHtml();

            await context.Response.WriteAsync(errorHtml);
        }

        private static (HttpStatusCode statusCode, string message) GetApiErrorResponse(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException _ => (HttpStatusCode.BadRequest, "Invalid request: Required parameter is missing"),
                ArgumentException _ => (HttpStatusCode.BadRequest, "Invalid request: Invalid parameter value"),
                UnauthorizedAccessException _ => (HttpStatusCode.Unauthorized, "Access denied"),
                NotImplementedException _ => (HttpStatusCode.NotImplemented, "Feature not implemented"),
                TimeoutException _ => (HttpStatusCode.RequestTimeout, "Request timeout"),
                InvalidOperationException _ => (HttpStatusCode.BadRequest, "Invalid operation"),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
            };
        }

        private static string GenerateDetailedErrorHtml(Exception exception, HttpContext context)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Application Error</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .error-container {{ background-color: #f8f9fa; padding: 20px; border-radius: 8px; border-left: 4px solid #dc3545; }}
        .error-title {{ color: #dc3545; font-size: 24px; margin-bottom: 20px; }}
        .error-details {{ background-color: #ffffff; padding: 15px; border-radius: 4px; border: 1px solid #dee2e6; }}
        .stack-trace {{ background-color: #f8f9fa; padding: 10px; font-family: monospace; font-size: 12px; white-space: pre-wrap; border-radius: 4px; margin-top: 10px; }}
        .request-info {{ margin-top: 20px; }}
        .request-info table {{ width: 100%; border-collapse: collapse; }}
        .request-info th, .request-info td {{ padding: 8px; text-align: left; border-bottom: 1px solid #dee2e6; }}
        .request-info th {{ background-color: #f8f9fa; }}
    </style>
</head>
<body>
    <div class='error-container'>
        <h1 class='error-title'>Application Error (Development Mode)</h1>
        <div class='error-details'>
            <h3>Exception Details</h3>
            <p><strong>Type:</strong> {exception.GetType().FullName}</p>
            <p><strong>Message:</strong> {exception.Message}</p>
            <div class='stack-trace'>{exception.ToString()}</div>
        </div>

        <div class='request-info'>
            <h3>Request Information</h3>
            <table>
                <tr><th>Request ID</th><td>{context.TraceIdentifier}</td></tr>
                <tr><th>Path</th><td>{context.Request.Path}</td></tr>
                <tr><th>Method</th><td>{context.Request.Method}</td></tr>
                <tr><th>Query String</th><td>{context.Request.QueryString}</td></tr>
                <tr><th>User Agent</th><td>{context.Request.Headers.UserAgent}</td></tr>
                <tr><th>Remote IP</th><td>{context.Connection.RemoteIpAddress}</td></tr>
                <tr><th>Timestamp</th><td>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</td></tr>
            </table>
        </div>
    </div>
</body>
</html>";
        }

        private static string GenerateSimpleErrorHtml()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <title>Application Error</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 50px; text-align: center; }
        .error-container { background-color: #f8f9fa; padding: 40px; border-radius: 8px; border-left: 4px solid #dc3545; max-width: 600px; margin: 0 auto; }
        .error-title { color: #dc3545; font-size: 32px; margin-bottom: 20px; }
        .error-message { font-size: 18px; color: #6c757d; margin-bottom: 30px; }
        .back-link { background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; display: inline-block; }
        .back-link:hover { background-color: #0056b3; }
    </style>
</head>
<body>
    <div class='error-container'>
        <h1 class='error-title'>Oops! Something went wrong</h1>
        <p class='error-message'>We're sorry, but an unexpected error has occurred. Our team has been notified and is working to resolve the issue.</p>
        <a href='/' class='back-link'>Return to Home</a>
    </div>
</body>
</html>";
        }
    }

    // Extension method to register the middleware
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}