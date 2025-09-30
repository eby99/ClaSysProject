using Microsoft.EntityFrameworkCore;
using RegistrationPortal.Data;
using RegistrationPortal.Services;
using RegistrationPortal.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
var loggerConfig = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
        rollOnFileSizeLimit: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
    );

// Add Windows Event Log for Web App (non-API requests) only in production on Windows
if (!builder.Environment.IsDevelopment() && System.OperatingSystem.IsWindows())
{
    try
    {
        loggerConfig.WriteTo.EventLog(
            source: "RegistrationPortal",
            logName: "Application",
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning,
            outputTemplate: "[RegistrationPortal] {Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        );
    }
    catch (System.Security.SecurityException)
    {
        // EventLog source creation requires admin privileges
        // This will be handled during deployment setup
    }
}

Log.Logger = loggerConfig.CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation(); // Enable runtime Razor compilation

// Add API controllers with JSON configuration
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep PascalCase
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Add Entity Framework
builder.Services.AddDbContext<RegistrationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "RegistrationPortal.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Add custom services
builder.Services.AddScoped<UserService>(); // Register the concrete implementation
builder.Services.AddScoped<IUserApiService, UserApiService>();
builder.Services.AddScoped<IUserService, HybridUserService>(); // Use hybrid service as the main implementation
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IValidationService, ValidationService>(); // Add missing validation service

// Add logging services
builder.Services.AddScoped<IEventLoggerService, EventLoggerService>();
builder.Services.AddScoped<IDatabaseLoggerService, DatabaseLoggerService>();

// Add notification services
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddHostedService<PendingApprovalNotificationService>();

// Add CAPTCHA services
builder.Services.AddHttpClient<ICaptchaService, GoogleReCaptchaService>();
// Use development CAPTCHA service in development environment (always returns true)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<ICaptchaService, DevelopmentCaptchaService>();
}

// Add HTTP client for API calls with configurable base URL
builder.Services.AddHttpClient<IUserApiClientService, UserApiClientService>(client =>
{
    // Get API base URL from configuration
    var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl");
    
    // If not configured, use environment-specific defaults
    if (string.IsNullOrEmpty(apiBaseUrl))
    {
        if (builder.Environment.IsDevelopment())
        {
            apiBaseUrl = "https://localhost:7149"; // Development
        }
        else
        {
            // Production - you need to set this in appsettings.json
            // For IIS virtual directory: "https://yourdomain.com/api/"
            // For separate site: "https://api.yourdomain.com/"
            apiBaseUrl = "https://localhost:7149"; // Fallback - replace with your production URL
        }
    }
    
    client.BaseAddress = new Uri(apiBaseUrl);
    client.DefaultRequestHeaders.Add("User-Agent", "RegistrationPortal-MVC/1.0");
});

// Add memory cache
builder.Services.AddMemoryCache();

// Logging is handled by Serilog

// Add anti-forgery token
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "__RequestVerificationToken";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Use global exception handling middleware instead of default exception handler
app.UseGlobalExceptionHandler();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    // Only use HTTPS redirection in development if HTTPS is configured
    var urls = builder.Configuration["ASPNETCORE_URLS"] ??
               Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ??
               app.Configuration["ApplicationUrl"] ?? "";

    if (urls.Contains("https://"))
    {
        app.UseHttpsRedirection();
    }
}
app.UseStaticFiles();

app.UseRouting();

// Add session middleware
app.UseSession();

// Add API logging middleware (only for /api paths)
app.UseApiLogging();

app.UseAuthorization();

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers.Remove("Server");

    await next();
});

// Map API controllers
app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("Database ensured to exist");

        var canConnect = await context.Database.CanConnectAsync();
        if (canConnect)
        {
            logger.LogInformation("Successfully connected to existing RegistrationDB database at {Time}", DateTime.Now);

            // Log some stats
            var userCount = await context.Users.CountAsync();
            var adminCount = await context.Admins.CountAsync();
            logger.LogInformation("Database contains {UserCount} users and {AdminCount} admins", userCount, adminCount);

            // Create default admin if none exists
            if (adminCount == 0)
            {
                var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
                var defaultAdmin = new RegistrationPortal.Models.Admin
                {
                    Username = "admin",
                    PasswordHash = passwordService.HashPassword("Admin@123"),
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                context.Admins.Add(defaultAdmin);
                await context.SaveChangesAsync();
                logger.LogInformation("Created default admin account - Username: admin, Password: Admin@123");
            }
        }
        else
        {
            logger.LogError("Could not connect to RegistrationDB database. Please check your connection string.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while connecting to the database");
    }
}

try
{
    Log.Information("Starting RegistrationPortal application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}