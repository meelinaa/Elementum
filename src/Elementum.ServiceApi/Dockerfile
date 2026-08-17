# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore (better layer caching)
COPY ["Elementum-ServiceApi/Elementum_ServiceApi.csproj", "Elementum-ServiceApi/"]
COPY ["Elementum.Infrastructure/Elementum.Infrastructure.csproj", "Elementum.Infrastructure/"]
COPY ["Elementum.Shared/Elementum.Shared.csproj", "Elementum.Shared/"]
RUN dotnet restore "Elementum-ServiceApi/Elementum_ServiceApi.csproj"

# Copy remaining source and publish
COPY . .
WORKDIR /src/Elementum-ServiceApi
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# ASP.NET Core 8+ listens on 8080 by default in containers
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Elementum_ServiceApi.dll"]
