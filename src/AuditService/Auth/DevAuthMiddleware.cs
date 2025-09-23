using System.Security.Claims;

namespace AuditService.Auth;

public class DevAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _cfg;

    public DevAuthMiddleware(RequestDelegate next, IConfiguration cfg)
    {
        _next = next; _cfg = cfg;
    }

    public async Task Invoke(HttpContext ctx)
    {
        var enabled = _cfg.GetValue<bool>("DevAuth:Enabled");
        if (enabled && !ctx.User.Identity!.IsAuthenticated)
        {
            var user  = _cfg["DevAuth:User"] ?? "dev";
            var roles = _cfg.GetSection("DevAuth:Roles").Get<string[]>() ?? Array.Empty<string>();
            var tenant= _cfg["DevAuth:Tenant"] ?? "dev-tenant";

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user),
                new("tenant", tenant)
            };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var id = new ClaimsIdentity(claims, "DevAuth");
            ctx.User = new ClaimsPrincipal(id);
        }
        await _next(ctx);
    }
}
