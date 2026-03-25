FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем Directory.Build.props и csproj-файлы, затем восстанавливаем зависимости
COPY Directory.Build.props ./
COPY src/Runner.Api/WebApplication1.csproj                                                   src/Runner.Api/
COPY src/Runner.SharedKernel/Runner.SharedKernel.csproj                                      src/Runner.SharedKernel/
COPY src/Modules/Auth/Runner.Auth.Module/Runner.Auth.Module.csproj                            src/Modules/Auth/Runner.Auth.Module/
COPY src/Modules/Parsers/Runner.Parsers.Module/Runner.Parsers.Module.csproj                   src/Modules/Parsers/Runner.Parsers.Module/
COPY src/Modules/Runner/Runner.Runner.Module/Runner.Runner.Module.csproj                      src/Modules/Runner/Runner.Runner.Module/
COPY src/Modules/Submissions/Runner.Submissions.Module/Runner.Submissions.Module.csproj       src/Modules/Submissions/Runner.Submissions.Module/

RUN dotnet restore src/Runner.Api/WebApplication1.csproj

# Копируем исходники и собираем (obj/ и bin/ исключены через .dockerignore)
COPY src/ src/
RUN dotnet publish src/Runner.Api/WebApplication1.csproj \
    -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WebApplication1.dll"]
