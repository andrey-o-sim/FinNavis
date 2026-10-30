# Build context is the repository root, not this folder — see docker-compose.yml.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore before the sources are copied, so an edit that leaves the csproj files untouched
# reuses the restore layer.
# .editorconfig carries the analyser rules, and Directory.Build.props turns warnings into errors.
# Without it the image compiles under a different ruleset than ./build.sh and CI.
COPY Directory.Build.props .editorconfig ./
COPY src/FinNavis.Domain/FinNavis.Domain.csproj src/FinNavis.Domain/
COPY src/FinNavis.Application/FinNavis.Application.csproj src/FinNavis.Application/
COPY src/FinNavis.Infrastructure/FinNavis.Infrastructure.csproj src/FinNavis.Infrastructure/
COPY src/FinNavis.Presentation/FinNavis.Presentation.csproj src/FinNavis.Presentation/
RUN dotnet restore src/FinNavis.Presentation/FinNavis.Presentation.csproj

COPY src/ src/
RUN dotnet publish src/FinNavis.Presentation/FinNavis.Presentation.csproj \
    --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "FinNavis.Presentation.dll"]
