using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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
                opt.Cookie.HttpOnly = true;
                opt.Cookie.SameSite = SameSiteMode.Lax;
                opt.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                opt.LoginPath = "/api/auth/login";
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
            // Список GitHub-логинов с ролью Admin (из конфига "GitHub:Admins": "login1,login2")
            var adminLogins = (configuration["GitHub:Admins"] ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            authBuilder.AddGitHub(opt =>
            {
                opt.ClientId     = githubClientId;
                opt.ClientSecret = githubClientSecret;
                opt.Scope.Add("read:user");
                opt.Scope.Add("public_repo");
                opt.SaveTokens   = true;
                opt.CallbackPath = "/api/auth/github/callback";

                opt.Events.OnCreatingTicket = ctx =>
                {
                    var login = ctx.User.GetProperty("login").GetString() ?? string.Empty;
                    var id    = ctx.User.GetProperty("id").GetInt64();
                    var profileUrl = ctx.User.GetProperty("html_url").GetString()
                                     ?? $"https://github.com/{login}";

                    var role = adminLogins.Contains(login) ? "Admin" : "Student";

                    ctx.Identity?.AddClaim(new Claim("github_login", login));
                    ctx.Identity?.AddClaim(new Claim("github_id",    id.ToString()));
                    ctx.Identity?.AddClaim(new Claim("github_profile_url", profileUrl));
                    ctx.Identity?.AddClaim(new Claim(ClaimTypes.Role, role));

                    // Сохраняем access token для вызовов GitHub API (repos, branches)
                    if (!string.IsNullOrEmpty(ctx.AccessToken))
                        ctx.Identity?.AddClaim(new Claim("github_access_token", ctx.AccessToken));

                    return Task.CompletedTask;
                };
            });
        }

        // ── HttpClient for GitHub API proxy ──────────────────────────────────
        services.AddHttpClient("GitHubApi", client =>
        {
            client.BaseAddress = new Uri("https://api.github.com");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            client.DefaultRequestHeaders.Add("User-Agent", "Runner-App");
        });

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

