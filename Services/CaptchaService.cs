using System.Text.Json;
using System.Text.Json.Serialization;

namespace RegistrationPortal.Services
{
    public interface ICaptchaService
    {
        Task<bool> VerifyTokenAsync(string token, string? userIpAddress = null);
    }

    public class GoogleReCaptchaService : ICaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleReCaptchaService> _logger;
        private readonly string _secretKey;

        public GoogleReCaptchaService(HttpClient httpClient, IConfiguration configuration, ILogger<GoogleReCaptchaService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _secretKey = _configuration["ReCaptcha:SecretKey"] ?? "6LeIxAcTAAAAAGG-vFI1TnRWxMZNFuojJ4WifJWe"; // Default test key
        }

        public async Task<bool> VerifyTokenAsync(string token, string? userIpAddress = null)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("🔒 CAPTCHA: Token is null or empty");
                return false;
            }

            try
            {
                var requestContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("secret", _secretKey),
                    new KeyValuePair<string, string>("response", token),
                    new KeyValuePair<string, string>("remoteip", userIpAddress ?? "")
                });

                _logger.LogInformation("🔍 CAPTCHA: Verifying token from IP: {IP}", userIpAddress ?? "Unknown");

                var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", requestContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ CAPTCHA: API returned non-success status: {StatusCode}", response.StatusCode);
                    return false;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var captchaResult = JsonSerializer.Deserialize<ReCaptchaResponse>(jsonResponse);

                if (captchaResult?.Success == true)
                {
                    _logger.LogInformation("✅ CAPTCHA: Verification successful for IP: {IP}", userIpAddress ?? "Unknown");
                    return true;
                }

                var errors = captchaResult?.ErrorCodes != null ? string.Join(", ", captchaResult.ErrorCodes) : "Unknown";
                _logger.LogWarning("⚠️ CAPTCHA: Verification failed. Errors: {Errors}", errors);

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CAPTCHA: Error verifying token");
                return false;
            }
        }
    }

    public class ReCaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
        public DateTime? ChallengeTs { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }
    }

    // Development/Testing CAPTCHA service that always returns true
    public class DevelopmentCaptchaService : ICaptchaService
    {
        private readonly ILogger<DevelopmentCaptchaService> _logger;

        public DevelopmentCaptchaService(ILogger<DevelopmentCaptchaService> logger)
        {
            _logger = logger;
        }

        public Task<bool> VerifyTokenAsync(string token, string? userIpAddress = null)
        {
            _logger.LogInformation("🐛 DEV CAPTCHA: Auto-accepting for development (IP: {IP})", userIpAddress ?? "Unknown");
            return Task.FromResult(true);
        }
    }
}