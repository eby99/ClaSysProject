using Microsoft.EntityFrameworkCore;
using RegistrationPortal.Data;
using RegistrationPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IValidationService, ValidationService>();

// Add memory cache
builder.Services.AddMemoryCache();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Test database connection
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Test connection to existing database
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

app.Run();