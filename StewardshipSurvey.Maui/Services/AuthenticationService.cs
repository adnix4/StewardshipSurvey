using System.Text;
using System.Text.Json;

namespace StewardshipSurvey.Maui.Services;

public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly ISecureStorage _secureStorage;
    private string _authToken;
    private string _userEmail;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken);
    public string UserEmail => _userEmail;

    public AuthenticationService()
    {
        _httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7295") };
        _secureStorage = SecureStorage.Default;
        _authToken = string.Empty;
        _userEmail = string.Empty;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var loginData = new { email, password };
            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/auth/login", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<LoginResponse>(
                    responseJson, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Token != null)
                {
                    _authToken = result.Token;
                    _userEmail = email;

                    // Store token securely
                    await _secureStorage.SetAsync("auth_token", _authToken);
                    await _secureStorage.SetAsync("user_email", email);

                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Login error: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RestoreTokenAsync()
    {
        try
        {
            _authToken = await _secureStorage.GetAsync("auth_token") ?? string.Empty;
            _userEmail = await _secureStorage.GetAsync("user_email") ?? string.Empty;

            return IsAuthenticated;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Restore token error: {ex.Message}");
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            _authToken = string.Empty;
            _userEmail = string.Empty;

            await _secureStorage.Remove("auth_token");
            await _secureStorage.Remove("user_email");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Logout error: {ex.Message}");
        }
    }

    public string GetToken() => _authToken;

    private class LoginResponse
    {
        public string Token { get; set; }
        public string Message { get; set; }
    }
}
