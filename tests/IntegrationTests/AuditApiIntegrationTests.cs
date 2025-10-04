using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace IntegrationTests;

public class AuditApiIntegrationTests
{
    private readonly HttpClient _client = new() { BaseAddress = new Uri("http://localhost:5000") };

    private static async Task LogFailure(HttpResponseMessage response)
    {
        var body = string.Empty;
        try { body = await response.Content.ReadAsStringAsync(); } catch { }
        Console.WriteLine($"[TEST DIAGNOSTIC] Status: {(int)response.StatusCode} {response.StatusCode}\nHeaders: {response.Headers}\nBody: {body}");
    }

    private void AddDevAuth()
    {
        if (!_client.DefaultRequestHeaders.Contains("X-Dev-User"))
        {
            // DevAuthMiddleware likely checks for a header to simulate an authenticated Auditor user (assumption).
            _client.DefaultRequestHeaders.Add("X-Dev-User", "auditor@interfacing.com");
            _client.DefaultRequestHeaders.Add("X-Dev-Roles", "Auditor");
        }
    }

    [Test]
    public async Task ScheduleAudit_Should_Return_200()
    {
        AddDevAuth();
        var payload = new { Title = "Integration Audit", ScheduledDate = DateTime.UtcNow.AddDays(3), AssignedTo = "auditor@interfacing.com" };
        var response = await _client.PostAsJsonAsync("/api/audit/schedule", payload);
        if (!response.IsSuccessStatusCode)
        {
            await LogFailure(response);
        }
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task GetAudits_Should_Return_List()
    {
        AddDevAuth();
        var response = await _client.GetAsync("/api/audit");
        if (!response.IsSuccessStatusCode)
        {
            await LogFailure(response);
        }
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }
}
