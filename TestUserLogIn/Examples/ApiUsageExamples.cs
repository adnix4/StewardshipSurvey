using System.Text;
using System.Text.Json;
using TestUserLogIn.Models.DTOs;

namespace TestUserLogIn.Examples
{
    /// <summary>
    /// Example usage of the Web API from various clients
    /// </summary>
    public class ApiUsageExamples
    {
        private readonly HttpClient _httpClient;

        public ApiUsageExamples(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Example: Get all service roles
        /// </summary>
        public async Task<List<InvolvementAreaDto>> GetAllServiceRolesExample()
        {
            var response = await _httpClient.GetAsync("api/serviceroles/all");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<InvolvementAreaDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            return new List<InvolvementAreaDto>();
        }

        /// <summary>
        /// Example: Get current user's service roles
        /// </summary>
        public async Task<List<MemberServiceRoleDto>> GetCurrentUserServiceRolesExample()
        {
            var response = await _httpClient.GetAsync("api/serviceroles/current");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<MemberServiceRoleDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            return new List<MemberServiceRoleDto>();
        }

        /// <summary>
        /// Example: Update current user's service roles
        /// </summary>
        public async Task<bool> UpdateServiceRolesExample(List<int> serviceRoleIds)
        {
            var json = JsonSerializer.Serialize(serviceRoleIds);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/serviceroles/current", content);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Example: Get all interests
        /// </summary>
        public async Task<List<InterestAreaDto>> GetAllInterestsExample()
        {
            var response = await _httpClient.GetAsync("api/interests/all");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<InterestAreaDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            return new List<InterestAreaDto>();
        }

        /// <summary>
        /// Example: Get current member information
        /// </summary>
        public async Task<MemberDto> GetCurrentMemberExample()
        {
            var response = await _httpClient.GetAsync("api/members/current");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<MemberDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            return null;
        }

        /// <summary>
        /// Example: Update current member information
        /// </summary>
        public async Task<bool> UpdateCurrentMemberExample(MemberDto memberDto)
        {
            var json = JsonSerializer.Serialize(memberDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync("api/members/current", content);
            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Example: Get member report (Staff/Admin only)
        /// </summary>
        public async Task<List<MemberReportDto>> GetMemberReportExample(
            string searchTerm = "",
            string serviceRoles = "",
            string interests = "",
            string involvementAreas = "")
        {
            var url = $"api/reports/members?searchTerm={searchTerm}&serviceRoles={serviceRoles}&interests={interests}&involvementAreas={involvementAreas}";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<MemberReportDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            return new List<MemberReportDto>();
        }

        /// <summary>
        /// Example: Filter members by service roles
        /// </summary>
        public async Task<List<MemberReportDto>> GetMembersWithServiceRoleExample(int serviceRoleId)
        {
            var url = $"api/reports/members?serviceRoles={serviceRoleId}";
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<MemberReportDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            return new List<MemberReportDto>();
        }

        /// <summary>
        /// Example: Filter members by multiple criteria
        /// </summary>
        public async Task<List<MemberReportDto>> GetFilteredMembersExample(
            string searchTerm,
            List<int> serviceRoleIds,
            List<int> interestIds)
        {
            var serviceRoles = string.Join(",", serviceRoleIds);
            var interests = string.Join(",", interestIds);
            var url = $"api/reports/members?searchTerm={searchTerm}&serviceRoles={serviceRoles}&interests={interests}";
            
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<MemberReportDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }
            return new List<MemberReportDto>();
        }
    }
}
