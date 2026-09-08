using System.Diagnostics;
using System.Net.Http.Headers;
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

    /// <summary>
    /// Raised when the server has stopped accepting this session and a refresh could not
    /// recover it - the account was deactivated, its roles changed, someone logged out, or the
    /// signing key was rotated. Without this a rejected session simply looked like a member
    /// with no data.
    /// </summary>
    public event EventHandler? SessionEnded;

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
                    await StoreSessionAsync(result.Token, email);
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

    /// <summary>
    /// Exchanges the current token for a fresh one.
    /// <para>
    /// The access token is good for an hour. Before this existed the app held one for a week
    /// and never renewed it, so shortening the server-side lifetime would have left a session
    /// silently returning no data after sixty minutes.
    /// </para>
    /// <para>
    /// The server refuses the exchange when the session has been ended deliberately - a
    /// deactivated account, changed roles, a logout elsewhere, a rotated signing key. There is
    /// nothing to recover from that, so the session is cleared and <see cref="SessionEnded"/>
    /// is raised.
    /// </para>
    /// </summary>
    public async Task<bool> RefreshAsync()
    {
        if (!IsAuthenticated)
        {
            return false;
        }

        try
        {
            var json = JsonSerializer.Serialize(new { token = _authToken });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/auth/refresh", content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<LoginResponse>(
                    responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Token != null)
                {
                    await StoreSessionAsync(result.Token, _userEmail);
                    return true;
                }
            }

            // A refusal is final: the token is past the refresh window or has been revoked.
            // Retrying would fail the same way.
            Debug.WriteLine($"Token refresh refused: {(int)response.StatusCode}");
            EndSession();
            return false;
        }
        catch (Exception ex)
        {
            // A network failure is not a revoked session, so the token is kept and the caller
            // can try again later.
            Debug.WriteLine($"Token refresh error: {ex.Message}");
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
            // Tell the server first. A bearer token cannot be withdrawn, so clearing it locally
            // left it valid until it expired - anyone holding a copy stayed signed in. The
            // endpoint moves the account's security stamp, which kills every token it issued.
            if (IsAuthenticated)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
                    await _httpClient.SendAsync(request);
                }
                catch (Exception ex)
                {
                    // Offline, or the token had already expired. The local session still goes:
                    // failing to reach the server must not leave someone stuck signed in.
                    Debug.WriteLine($"Server logout failed, clearing locally anyway: {ex.Message}");
                }
            }

            EndSession(raiseEvent: false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Logout error: {ex.Message}");
        }
    }

    public string GetToken() => _authToken;

    private async Task StoreSessionAsync(string token, string email)
    {
        _authToken = token;
        _userEmail = email;

        await _secureStorage.SetAsync("auth_token", token);
        await _secureStorage.SetAsync("user_email", email);
    }

    /// <summary>
    /// Clears the stored session. Note <c>ISecureStorage.Remove</c> is synchronous and returns
    /// a bool - this used to await it, which does not compile.
    /// </summary>
    private void EndSession(bool raiseEvent = true)
    {
        _authToken = string.Empty;
        _userEmail = string.Empty;

        _secureStorage.Remove("auth_token");
        _secureStorage.Remove("user_email");

        if (raiseEvent)
        {
            SessionEnded?.Invoke(this, EventArgs.Empty);
        }
    }

    private class LoginResponse
    {
        public string? Token { get; set; }
        public string? Message { get; set; }
    }
}
