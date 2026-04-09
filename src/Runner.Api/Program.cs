using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using Runner.SharedKernel;
using Runner.Auth.Module;
using Runner.Parsers.Module;
using Runner.Runner.Module;
using Runner.Submissions.Module;

var builder = WebApplication.CreateBuilder(args);

// ── JSON ─────────────────────────────────────────────────────────────────────
builder.Services.ConfigureHttpJsonOptions(opt =>
{
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// ── OpenAPI ──────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Auth Module ──────────────────────────────────────────────────────────────
builder.Services.AddAuthModule(builder.Configuration);

// ── Parsers Module ───────────────────────────────────────────────────────────
builder.Services.AddParsersModule();

// ── Runner Module ────────────────────────────────────────────────────────────
builder.Services.AddRunnerModule(builder.Configuration);

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

// ── Forwarded headers (behind nginx + Traefik in Docker) ─────────────────────
// KnownProxies/KnownNetworks очищаем, чтобы доверять nginx внутри Docker-сети
// (его IP — 172.x.x.x, не входит в дефолтный доверенный список 127.0.0.1)
var forwardedOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedOptions.KnownIPNetworks.Clear();
forwardedOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedOptions);

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
var api = app.MapGroup("/api");
api.MapAuthModuleEndpoints(app.Environment);
api.MapSubmissionsModuleEndpoints();
api.MapRunnerModuleEndpoints();

// ── Migrations on startup ────────────────────────────────────────────────────
await app.Services.MigrateSubmissionsDbAsync();

app.Run();
