using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Runner.Submissions.Module.Api;
using Runner.Submissions.Module.Application.Interfaces;
using Runner.Submissions.Module.Application.UseCases.CreateAssignment;
using Runner.Submissions.Module.Application.UseCases.CreateSubmission;
using Runner.Submissions.Module.Application.UseCases.GetAssignment;
using Runner.Submissions.Module.Application.UseCases.GetSubmission;
using Runner.Submissions.Module.Application.UseCases.GetSubmissionReport;
using Runner.Submissions.Module.Application.UseCases.ListMySubmissions;
using Runner.Submissions.Module.Infrastructure.Database;

namespace Runner.Submissions.Module;

public static class SubmissionsModuleExtensions
{
    public static IServiceCollection AddSubmissionsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core — PostgreSQL
        services.AddDbContext<SubmissionsDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Default")));
        services.AddScoped<ISubmissionsDbContext>(sp =>
            sp.GetRequiredService<SubmissionsDbContext>());

        // Use-case handlers
        services.AddScoped<CreateAssignmentHandler>();
        services.AddScoped<CreateSubmissionHandler>();
        services.AddScoped<GetAssignmentHandler>();
        services.AddScoped<GetSubmissionHandler>();
        services.AddScoped<GetSubmissionReportHandler>();
        services.AddScoped<ListMySubmissionsHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapSubmissionsModuleEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapAssignmentsEndpoints();
        app.MapSubmissionsEndpoints();
        return app;
    }

    /// <summary>Применяет миграции при старте приложения.</summary>
    public static async Task MigrateSubmissionsDbAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SubmissionsDbContext>();
        await db.Database.MigrateAsync();
    }
}
