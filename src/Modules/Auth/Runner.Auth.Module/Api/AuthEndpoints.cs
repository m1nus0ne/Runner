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
        // GitHub OAuth login
        app.MapGet("/auth/login", () =>
                Results.Challenge(
                    new AuthenticationProperties { RedirectUri = "/" },
                    ["GitHub"]))
            .AllowAnonymous();

        // Dev-only: hard-coded login without OAuth
        if (environment.IsDevelopment())
        {
            app.MapGet("/auth/dev-login", async (HttpContext ctx,
                string login = "dev-user",
                string role  = "Admin") =>
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
                return Results.Ok(new { message = $"Signed in as '{login}' with role '{role}'." });
            }).AllowAnonymous();
        }

        // Logout
        app.MapGet("/auth/logout", async ctx =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ctx.Response.Redirect("/");
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
}

