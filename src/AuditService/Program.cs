using AuditService.Data;
using AuditService.Models;
using AuditService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Caching;
using Shared.Messaging;
using Shared.Observability;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Telemetry
builder.Services.AddAppTelemetry("AuditService");

// Messaging & Cache
builder.Services.AddSingleton<RabbitMqClient>();
builder.Services.AddSingleton<IRedisClient, RedisClient>();

// Business
builder.Services.AddScoped<AuditScheduler>();

// CORS
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowFrontend", p =>
        p.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3000" })
         .AllowAnyHeader()
         .AllowAnyMethod());
});

// Auth
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var cfg = builder.Configuration.GetSection("Auth");
        options.Authority = cfg["Authority"];
        options.Audience  = cfg["Audience"];
        options.RequireHttpsMetadata = bool.Parse(cfg["RequireHttps"] ?? "false");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = "preferred_username"
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = ctx =>
            {
                var claimsIdentity = (ClaimsIdentity)ctx.Principal!.Identity!;
                if (ctx.SecurityToken is System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwt)
                {
                    using var doc = JsonDocument.Parse(jwt.Payload.SerializeToJson());
                    var root = doc.RootElement;
                    if (root.TryGetProperty("realm_access", out var realm) &&
                        realm.TryGetProperty("roles", out var roles) &&
                        roles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var r in roles.EnumerateArray())
                            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, r.GetString()!));
                    }
                    if (root.TryGetProperty("resource_access", out var res) &&
                        res.TryGetProperty("regulaflow-api", out var api) &&
                        api.TryGetProperty("roles", out var croles) &&
                        croles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var r in croles.EnumerateArray())
                            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, r.GetString()!));
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Auditor", p => p.RequireRole("Auditor", "ComplianceAdmin", "admin"));
});

var app = builder.Build();

// Ensure schema exists
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Dev auth stub (runtime toggled)
app.UseMiddleware<AuditService.Auth.DevAuthMiddleware>();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

var audits = app.MapGroup("/api/audit").RequireAuthorization("Auditor");

audits.MapPost("/schedule", async (Audit audit, AuditScheduler service) =>
{
    var result = await service.ScheduleAuditAsync(audit);
    return Results.Ok(new { result.Id, message = "Audit scheduled, event published, cached" });
});

audits.MapGet("/", async (AuditScheduler service) =>
{
    var list = await service.GetAuditsAsync();
    return Results.Ok(list);
});

app.MapGet("/api/audit/{id}", async (Guid id, IRedisClient cache) =>
{
    var cached = await cache.GetAsync<object>($"audit:{id}");
    return cached is not null ? Results.Ok(cached) : Results.NotFound();
}).RequireAuthorization("Auditor");

app.MapGet("/api/audit/{id}/summary", async (Guid id, IRedisClient cache) =>
{
    var summary = await cache.GetAsync<object>("audit:" + id + ":summary");
    return summary is not null ? Results.Ok(summary) : Results.NotFound();
}).RequireAuthorization("Auditor");

app.Run();
