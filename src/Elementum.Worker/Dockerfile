# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore (better layer caching)
COPY ["Elementum.Worker/Elementum.Worker.csproj", "Elementum.Worker/"]
COPY ["Elementum.Application/Elementum.Application.csproj", "Elementum.Application/"]
COPY ["Elementum.Domain/Elementum.Domain.csproj", "Elementum.Domain/"]
COPY ["Elementum.Infrastructure/Elementum.Infrastructure.csproj", "Elementum.Infrastructure/"]
RUN dotnet restore "Elementum.Worker/Elementum.Worker.csproj"

# Copy remaining source and publish
COPY . .
WORKDIR /src/Elementum.Worker
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5094
ENV Urls=http://+:5094

ENTRYPOINT ["dotnet", "Elementum.Worker.dll"]
