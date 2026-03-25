using Microsoft.AspNetCore.Diagnostics;
using Scalar.AspNetCore;
using Runner.SharedKernel;
using Runner.Auth.Module;
using Runner.Submissions.Module;

var builder = WebApplication.CreateBuilder(args);

// ── OpenAPI ──────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Auth Module ──────────────────────────────────────────────────────────────
builder.Services.AddAuthModule(builder.Configuration);

// ── Submissions Module ───────────────────────────────────────────────────────
builder.Services.AddSubmissionsModule(builder.Configuration);

var app = builder.Build();

// ── Middleware ───────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                  // GET /openapi/v1.json
    app.MapScalarApiReference(opt =>   // GET /scalar/v1
    {
        opt.Title = "Runner API";
        opt.Theme = ScalarTheme.DeepSpace;
        opt.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    ctx.Response.ContentType = "application/json";
    ctx.Response.StatusCode = ex switch
    {
        NotFoundException  => 404,
        ForbiddenException => 403,
        _                  => 500
    };
    await ctx.Response.WriteAsJsonAsync(new { error = ex?.Message ?? "Internal server error" });
}));

// ── Module endpoints ─────────────────────────────────────────────────────────
app.MapAuthModuleEndpoints(app.Environment);
app.MapSubmissionsModuleEndpoints();

// ── Migrations on startup ────────────────────────────────────────────────────
await app.Services.MigrateSubmissionsDbAsync();

app.Run();
