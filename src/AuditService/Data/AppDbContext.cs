using Microsoft.EntityFrameworkCore;
using AuditService.Models;

namespace AuditService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}
    public DbSet<Audit> Audits => Set<Audit>();
}
