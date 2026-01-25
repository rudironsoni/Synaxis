# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY ["Synaxis.sln", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/Synaxis.WebApi/Synaxis.WebApi.csproj", "src/Synaxis.WebApi/"]
COPY ["src/Synaxis.Application/Synaxis.Application.csproj", "src/Synaxis.Application/"]
COPY ["src/Synaxis.Infrastructure/Synaxis.Infrastructure.csproj", "src/Synaxis.Infrastructure/"]
COPY ["tests/Synaxis.Infrastructure.Tests/Synaxis.Infrastructure.Tests.csproj", "tests/Synaxis.Infrastructure.Tests/"]
COPY ["tests/Synaxis.Application.Tests/Synaxis.Application.Tests.csproj", "tests/Synaxis.Application.Tests/"]
COPY ["tests/Synaxis.IntegrationTests/Synaxis.IntegrationTests.csproj", "tests/Synaxis.IntegrationTests/"]

RUN dotnet restore

# Copy everything else and build
COPY . .
WORKDIR "/src/src/Synaxis.WebApi"
RUN dotnet publish "Synaxis.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Synaxis.WebApi.dll"]
