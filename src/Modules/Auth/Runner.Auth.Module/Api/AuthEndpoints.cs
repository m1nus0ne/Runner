using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Runner.Auth.Module.Api;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder app,
        IHostEnvironment environment)
    {
        // GitHub OAuth login — принимает returnUrl для редиректа после авторизации
        app.MapGet("/auth/login", (string? returnUrl) =>
                Results.Challenge(
                    new AuthenticationProperties
                    {
                        RedirectUri = returnUrl ?? "/"
                    },
                    ["GitHub"]))
            .AllowAnonymous();

        // Dev-only: быстрый вход без OAuth
        if (environment.IsDevelopment())
        {
            app.MapGet("/auth/dev-login/student", async (HttpContext ctx) =>
            {
                await SignInDev(ctx, "dev-student", "Student");
                return Results.Ok(new { message = "Signed in as 'dev-student' with role 'Student'." });
            }).AllowAnonymous();

            app.MapGet("/auth/dev-login/admin", async (HttpContext ctx) =>
            {
                await SignInDev(ctx, "dev-admin", "Admin");
                return Results.Ok(new { message = "Signed in as 'dev-admin' with role 'Admin'." });
            }).AllowAnonymous();
        }

        // Logout
        app.MapPost("/auth/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Ok(new { message = "Signed out." });
        }).RequireAuthorization();

        // Current user info (+ profileUrl + hasGitHubToken)
        app.MapGet("/auth/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            id              = user.FindFirstValue(ClaimTypes.NameIdentifier),
            login           = user.FindFirstValue("github_login"),
            role            = user.FindFirstValue(ClaimTypes.Role),
            profileUrl      = user.FindFirstValue("github_profile_url"),
            hasGitHubToken  = !string.IsNullOrEmpty(user.FindFirstValue("github_access_token"))
        })).RequireAuthorization();

        // ── GitHub API proxy: список публичных репозиториев пользователя ─────
        app.MapGet("/auth/github/repos", async (
            ClaimsPrincipal user,
            IHttpClientFactory httpFactory,
            CancellationToken ct) =>
        {
            var token = user.FindFirstValue("github_access_token");
            if (string.IsNullOrEmpty(token))
                return Results.Ok(Array.Empty<object>());

            var client = httpFactory.CreateClient("GitHubApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // visibility + affiliation (параметр type с ними несовместим)
            var resp = await client.GetAsync(
                "/user/repos?visibility=public&affiliation=owner&sort=updated&per_page=100", ct);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                return Results.Problem($"GitHub API error: {body}", statusCode: (int)resp.StatusCode);
            }

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
            var repos = json.EnumerateArray().Select(r => new
            {
                name     = r.GetProperty("name").GetString(),
                fullName = r.GetProperty("full_name").GetString(),
                htmlUrl  = r.GetProperty("html_url").GetString(),
                isPrivate = r.GetProperty("private").GetBoolean(),
                defaultBranch = r.GetProperty("default_branch").GetString()
            });

            return Results.Ok(repos);
        }).RequireAuthorization();

        // ── GitHub API proxy: ветки репозитория ─────────────────────────────
        app.MapGet("/auth/github/repos/{owner}/{repo}/branches", async (
            string owner,
            string repo,
            ClaimsPrincipal user,
            IHttpClientFactory httpFactory,
            CancellationToken ct) =>
        {
            var token = user.FindFirstValue("github_access_token");
            if (string.IsNullOrEmpty(token))
                return Results.Ok(Array.Empty<object>());

            var client = httpFactory.CreateClient("GitHubApi");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var resp = await client.GetAsync(
                $"/repos/{owner}/{repo}/branches?per_page=100", ct);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                return Results.Problem($"GitHub API error: {body}", statusCode: (int)resp.StatusCode);
            }

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
            var branches = json.EnumerateArray().Select(b => new
            {
                name = b.GetProperty("name").GetString()
            });

            return Results.Ok(branches);
        }).RequireAuthorization();

        return app;
    }

    private static async Task SignInDev(HttpContext ctx, string login, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "0"),
            new(ClaimTypes.Name,           login),
            new(ClaimTypes.Role,           role),
            new("github_login",            login),
            new("github_id",               "0"),
            new("github_profile_url",      $"https://github.com/{login}"),
        };
        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}

