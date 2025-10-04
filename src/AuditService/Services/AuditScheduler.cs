    using AuditService.Data;
using AuditService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging;
using Shared.Caching;

namespace AuditService.Services;

public class AuditScheduler
{
    private readonly AppDbContext _context;
    private readonly IRabbitMqClient _mq;
    private readonly IRedisClient _cache;

    public AuditScheduler(AppDbContext context, IRabbitMqClient mq, IRedisClient cache)
    {
        _context = context;
        _mq = mq;
        _cache = cache;
    }

    public async Task<Audit> ScheduleAuditAsync(Audit audit)
    {
        _context.Audits.Add(audit);
        await _context.SaveChangesAsync();

        var evt = new AuditScheduledEvent(audit.Id, audit.Title, audit.ScheduledDate, audit.AssignedTo);
        _mq.Publish(evt);

        await _cache.SetAsync($"audit:{audit.Id}", new { audit.Id, audit.Title, audit.ScheduledDate });

        return audit;
    }

    public Task<List<Audit>> GetAuditsAsync() => _context.Audits.ToListAsync();
}
