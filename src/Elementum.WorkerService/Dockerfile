# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore (better layer caching)
COPY ["Elementum.WorkerService/Elementum.WorkerService.csproj", "Elementum.WorkerService/"]
COPY ["Elementum.Infrastructure/Elementum.Infrastructure.csproj", "Elementum.Infrastructure/"]
COPY ["Elementum.Shared/Elementum.Shared.csproj", "Elementum.Shared/"]
RUN dotnet restore "Elementum.WorkerService/Elementum.WorkerService.csproj"

# Copy remaining source and publish
COPY . .
WORKDIR /src/Elementum.WorkerService
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5094
ENV Urls=http://+:5094

ENTRYPOINT ["dotnet", "Elementum.WorkerService.dll"]

