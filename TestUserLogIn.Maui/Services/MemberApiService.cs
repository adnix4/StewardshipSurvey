using System.Text;
using System.Text.Json;
using TestUserLogIn.Maui.Models;

namespace TestUserLogIn.Maui.Services;

public class MemberApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public MemberApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
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
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);
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
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);
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
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);
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
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);
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
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);
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
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);
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
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);
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

            var request = new HttpRequestMessage(HttpMethod.Post, "/api/members/current")
            {
                Content = content
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving member info: {ex.Message}");
            return false;
        }
    }
}
