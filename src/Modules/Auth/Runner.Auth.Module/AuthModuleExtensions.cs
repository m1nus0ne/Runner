using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Runner.Auth.Module.Api;

namespace Runner.Auth.Module;

public static class AuthModuleExtensions
{
    public static IServiceCollection AddAuthModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── GitHub OAuth + Cookie auth ───────────────────────────────────────
        var githubClientId     = configuration["GitHub:ClientId"];
        var githubClientSecret = configuration["GitHub:ClientSecret"];

        var authBuilder = services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(opt =>
            {
                opt.LoginPath = "/auth/login";
                opt.Events.OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = 401;
                    return Task.CompletedTask;
                };
                opt.Events.OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = 403;
                    return Task.CompletedTask;
                };
            });

        if (!string.IsNullOrWhiteSpace(githubClientId) &&
            !string.IsNullOrWhiteSpace(githubClientSecret))
        {
            authBuilder.AddGitHub(opt =>
            {
                opt.ClientId     = githubClientId;
                opt.ClientSecret = githubClientSecret;
                opt.Scope.Add("read:user");
                opt.CallbackPath = "/auth/github/callback";

                opt.Events.OnCreatingTicket = ctx =>
                {
                    var login = ctx.User.GetProperty("login").GetString() ?? string.Empty;
                    var id    = ctx.User.GetProperty("id").GetInt64();
                    ctx.Identity?.AddClaim(new Claim("github_login", login));
                    ctx.Identity?.AddClaim(new Claim("github_id",    id.ToString()));
                    return Task.CompletedTask;
                };
            });
        }

        // ── Authorization policies ───────────────────────────────────────────
        services.AddAuthorization(opt =>
        {
            opt.AddPolicy("AdminOnly",   p => p.RequireRole("Admin"));
            opt.AddPolicy("StudentOnly", p => p.RequireRole("Student", "Admin"));
        });

        return services;
    }

    public static IEndpointRouteBuilder MapAuthModuleEndpoints(
        this IEndpointRouteBuilder app,
        IHostEnvironment environment)
    {
        app.MapAuthEndpoints(environment);
        return app;
    }
}

