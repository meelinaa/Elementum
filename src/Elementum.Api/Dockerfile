# Build stage
# Floating tags (sdk:10.0 / aspnet:10.0). Pin by digest when a release must be bit-reproducible.
# Context: repository root (Central Package Management files live there).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props nuget.config ./
COPY src/Elementum.Api/Elementum.Api.csproj Elementum.Api/
COPY src/Elementum.Application/Elementum.Application.csproj Elementum.Application/
COPY src/Elementum.Domain/Elementum.Domain.csproj Elementum.Domain/
COPY src/Elementum.Infrastructure/Elementum.Infrastructure.csproj Elementum.Infrastructure/
RUN dotnet restore Elementum.Api/Elementum.Api.csproj

COPY src/ .
WORKDIR /src/Elementum.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID

HEALTHCHECK --interval=10s --timeout=5s --start-period=40s --retries=5 \
    CMD curl -f http://127.0.0.1:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "Elementum.Api.dll"]
