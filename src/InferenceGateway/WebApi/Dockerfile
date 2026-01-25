# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY ["Synaxis.sln", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/InferenceGateway/WebApi/Synaxis.InferenceGateway.WebApi.csproj", "src/InferenceGateway/WebApi/"]
COPY ["src/InferenceGateway/Application/Synaxis.InferenceGateway.Application.csproj", "src/InferenceGateway/Application/"]
COPY ["src/InferenceGateway/Infrastructure/Synaxis.InferenceGateway.Infrastructure.csproj", "src/InferenceGateway/Infrastructure/"]
COPY ["tests/InferenceGateway/Infrastructure.Tests/Synaxis.InferenceGateway.Infrastructure.Tests.csproj", "tests/InferenceGateway/Infrastructure.Tests/"]
COPY ["tests/InferenceGateway/Application.Tests/Synaxis.InferenceGateway.Application.Tests.csproj", "tests/InferenceGateway/Application.Tests/"]
COPY ["tests/InferenceGateway/IntegrationTests/Synaxis.InferenceGateway.IntegrationTests.csproj", "tests/InferenceGateway/IntegrationTests/"]

RUN dotnet restore

# Copy everything else and build
COPY . .
WORKDIR "/src/src/InferenceGateway/WebApi"
RUN dotnet publish "Synaxis.InferenceGateway.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Synaxis.InferenceGateway.WebApi.dll"]
