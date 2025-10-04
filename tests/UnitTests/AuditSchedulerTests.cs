using AuditService.Data;
using AuditService.Models;
using AuditService.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Caching;
using Shared.Messaging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests;

public class AuditSchedulerTests
{
    private AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Test]
    public async Task ScheduleAudit_Should_Save_And_Publish_Event()
    {
        var dbContext = CreateInMemoryDb();
        var mockMq = new Mock<IRabbitMqClient>();
        mockMq.Setup(m => m.Publish(It.IsAny<AuditScheduledEvent>()));
        var mockCache = new Mock<IRedisClient>();

        var service = new AuditScheduler(dbContext, mockMq.Object, mockCache.Object);
        var audit = new Audit { Title = "ISO 27001", ScheduledDate = DateTime.UtcNow.AddDays(10), AssignedTo = "auditor@test.com" };

        var result = await service.ScheduleAuditAsync(audit);

        await Assert.That(dbContext.Audits.Count()).IsEqualTo(1);
        await Assert.That(result.Title).IsEqualTo("ISO 27001");
        mockMq.Verify(m => m.Publish(It.IsAny<AuditScheduledEvent>()), Times.Once);
    }

    [Test]
    public async Task GetAudits_Should_Return_All_Audits()
    {
        var dbContext = CreateInMemoryDb();
        var mockMq = new Mock<IRabbitMqClient>();
        var mockCache = new Mock<IRedisClient>();

        dbContext.Audits.Add(new Audit { Title = "GDPR", ScheduledDate = DateTime.UtcNow.AddDays(5), AssignedTo = "auditor2@test.com" });
        await dbContext.SaveChangesAsync();

        var service = new AuditScheduler(dbContext, mockMq.Object, mockCache.Object);
        var audits = await service.GetAuditsAsync();

        await Assert.That(audits.Count).IsEqualTo(1);
        await Assert.That(audits.First().Title).IsEqualTo("GDPR");
    }

    [Test]
    public async Task ScheduleAudit_Should_Cache_Summary()
    {
        var dbContext = CreateInMemoryDb();
        var mockMq = new Mock<IRabbitMqClient>();
        var mockCache = new Mock<IRedisClient>();
        mockCache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), null))
                 .Returns(Task.CompletedTask)
                 .Verifiable();

        var service = new AuditScheduler(dbContext, mockMq.Object, mockCache.Object);
        var audit = new Audit { Title = "PCI DSS", ScheduledDate = DateTime.UtcNow.AddDays(30), AssignedTo = "auditor@test.com" };

        var result = await service.ScheduleAuditAsync(audit);

        await Assert.That(dbContext.Audits.Count()).IsEqualTo(1);
        mockCache.Verify(c => c.SetAsync($"audit:{result.Id}", It.IsAny<object>(), null), Times.Once);
    }
}
