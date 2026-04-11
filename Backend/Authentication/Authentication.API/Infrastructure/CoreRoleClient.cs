using Authentication.DAL.Models;
using Authentication.DL.Services;
using System.Net.Http.Json;

namespace Authentication.API.Infrastructure
{
    public class CoreRoleClient : ICoreRoleClient
    {
        private readonly HttpClient _httpClient;

        public CoreRoleClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetRoleAsync(string email)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/u/general/{Uri.EscapeDataString(email)}");
                if (!response.IsSuccessStatusCode)
                    return Roles.Student;

                var body = await response.Content.ReadFromJsonAsync<CoreRoleResponse>();
                if (body?.Value?.IsTeacher == true)
                    return Roles.Teacher;
            }
            catch
            {
                // Core unavailable or not yet processed the registration event — default to Student
            }

            return Roles.Student;
        }

        private sealed class CoreRoleResponse
        {
            public bool Success { get; set; }
            public RoleValue? Value { get; set; }

            public sealed class RoleValue
            {
                public bool IsTeacher { get; set; }
            }
        }
    }
}
