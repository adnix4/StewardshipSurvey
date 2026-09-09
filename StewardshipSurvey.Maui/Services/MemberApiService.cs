using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using StewardshipSurvey.Maui.Models;
using System.Diagnostics;

namespace StewardshipSurvey.Maui.Services;

public class MemberApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthenticationService _auth;
    private readonly JsonSerializerOptions _jsonOptions;

    public MemberApiService(HttpClient httpClient, AuthenticationService auth)
    {
        _httpClient = httpClient;
        _auth = auth;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    /// <summary>
    /// Sends an authenticated request, renewing the token once if the server rejects it.
    /// <para>
    /// Every method here reports a failure by returning an empty result, so a 401 was
    /// indistinguishable from a member who had simply answered nothing. With the access token
    /// now lasting an hour rather than a week, that would have become the normal experience
    /// after sixty minutes: a survey that quietly forgot every answer.
    /// </para>
    /// <para>
    /// One retry only. If the refresh fails the session is genuinely over - the account was
    /// deactivated, its roles changed, or the signing key was rotated - and
    /// <see cref="AuthenticationService.SessionEnded"/> has already fired.
    /// </para>
    /// </summary>
    private async Task<HttpResponseMessage> SendAuthenticatedAsync(
        HttpRequestMessage request, string authToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        if (!await _auth.RefreshAsync())
        {
            return response;
        }

        // A request cannot be sent twice, so the retry goes on a copy carrying the new token.
        var retry = await CloneAsync(request);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.GetToken());

        return await _httpClient.SendAsync(retry);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        if (request.Content != null)
        {
            var body = await request.Content.ReadAsStringAsync();
            var mediaType = request.Content.Headers.ContentType?.MediaType ?? "application/json";
            clone.Content = new StringContent(body, Encoding.UTF8, mediaType);
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

    public async Task<List<InterestAreaDto>> GetAllInterestsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/interests/all");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<InterestAreaDto>>(json, _jsonOptions) ?? new();
            }
            return new();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting interests: {ex.Message}");
            return new();
        }
    }

    public async Task<List<InterestAreaDto>> GetCurrentInterestsAsync(string authToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/interests/current");

            var response = await SendAuthenticatedAsync(request, authToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<InterestAreaDto>>(json, _jsonOptions) ?? new();
            }
            return new();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting current interests: {ex.Message}");
            return new();
        }
    }

    public async Task<bool> SaveInterestsAsync(List<int> interestIds, string authToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(interestIds);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/interests/current")
            {
                Content = content
            };

            var response = await SendAuthenticatedAsync(request, authToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving interests: {ex.Message}");
            return false;
        }
    }

    public async Task<List<InvolvementAreaDto>> GetAllInvolvementsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/involvements/all");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<InvolvementAreaDto>>(json, _jsonOptions) ?? new();
            }
            return new();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting involvements: {ex.Message}");
            return new();
        }
    }

    public async Task<List<InvolvementAreaDto>> GetCurrentInvolvementsAsync(string authToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/involvements/current");

            var response = await SendAuthenticatedAsync(request, authToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<InvolvementAreaDto>>(json, _jsonOptions) ?? new();
            }
            return new();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting current involvements: {ex.Message}");
            return new();
        }
    }

    public async Task<bool> SaveInvolvementsAsync(List<int> involvementIds, string authToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(involvementIds);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/involvements/current")
            {
                Content = content
            };

            var response = await SendAuthenticatedAsync(request, authToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving involvements: {ex.Message}");
            return false;
        }
    }

    public async Task<List<InvolvementAreaDto>> GetAllServiceRolesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/serviceroles/all");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<InvolvementAreaDto>>(json, _jsonOptions) ?? new();
            }
            return new();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting service roles: {ex.Message}");
            return new();
        }
    }

    public async Task<List<InvolvementAreaDto>> GetCurrentServiceRolesAsync(string authToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/serviceroles/current");

            var response = await SendAuthenticatedAsync(request, authToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<InvolvementAreaDto>>(json, _jsonOptions) ?? new();
            }
            return new();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting current service roles: {ex.Message}");
            return new();
        }
    }

    public async Task<bool> SaveServiceRolesAsync(List<int> serviceRoleIds, string authToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(serviceRoleIds);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/serviceroles/current")
            {
                Content = content
            };

            var response = await SendAuthenticatedAsync(request, authToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving service roles: {ex.Message}");
            return false;
        }
    }

    public async Task<MemberDto> GetMemberInfoAsync(string authToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/members/current");

            var response = await SendAuthenticatedAsync(request, authToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MemberDto>(json, _jsonOptions) ?? new();
            }
            return new();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting member info: {ex.Message}");
            return new();
        }
    }

    public async Task<bool> SaveMemberInfoAsync(MemberDto member, string authToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(member);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // PUT, not POST. The server exposes this route as PUT only, so the POST this
            // used to send came back 405 and saving a profile could never have worked.
            var request = new HttpRequestMessage(HttpMethod.Put, "/api/members/current")
            {
                Content = content
            };

            var response = await SendAuthenticatedAsync(request, authToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving member info: {ex.Message}");
            return false;
        }
    }
}
