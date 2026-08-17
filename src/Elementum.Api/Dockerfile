# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for optimized layer restore
COPY ["Elementum.Api/Elementum.Api.csproj", "Elementum.Api/"]
COPY ["Elementum.Application/Elementum.Application.csproj", "Elementum.Application/"]
COPY ["Elementum.Domain/Elementum.Domain.csproj", "Elementum.Domain/"]
COPY ["Elementum.Infrastructure/Elementum.Infrastructure.csproj", "Elementum.Infrastructure/"]
RUN dotnet restore "Elementum.Api/Elementum.Api.csproj"

# Copy full source and publish Release
COPY . .
WORKDIR /src/Elementum.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage (non-root security default in .NET 8+)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
USER $APP_UID

ENTRYPOINT ["dotnet", "Elementum.Api.dll"]
