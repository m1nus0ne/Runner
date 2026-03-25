using Microsoft.Extensions.DependencyInjection;
using Runner.Parsers.Module.Application.Interfaces;
using Runner.Parsers.Module.Infrastructure.NUnit;

namespace Runner.Parsers.Module;

public static class ParsersModuleExtensions
{
    public static IServiceCollection AddParsersModule(this IServiceCollection services)
    {
        services.AddScoped<INUnitXmlParser, NUnitXmlParser>();
        return services;
    }
}

