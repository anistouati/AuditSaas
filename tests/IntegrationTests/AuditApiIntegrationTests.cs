using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace IntegrationTests;

public class AuditApiIntegrationTests
{
    private readonly HttpClient _client = new() { BaseAddress = new Uri("http://localhost:5000") };

    [Test]
    public async Task ScheduleAudit_Should_Return_200()
    {
        var payload = new { Title = "Integration Audit", ScheduledDate = DateTime.UtcNow.AddDays(3), AssignedTo = "auditor@interfacing.com" };
        var response = await _client.PostAsJsonAsync("/api/audit/schedule", payload);
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }

    [Test]
    public async Task GetAudits_Should_Return_List()
    {
        var response = await _client.GetAsync("/api/audit");
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }
}
