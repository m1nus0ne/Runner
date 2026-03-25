using System.Security.Claims;
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

        // Current user info
        app.MapGet("/auth/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            id    = user.FindFirstValue(ClaimTypes.NameIdentifier),
            login = user.FindFirstValue("github_login"),
            role  = user.FindFirstValue(ClaimTypes.Role)
        })).RequireAuthorization();

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
        };
        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}

