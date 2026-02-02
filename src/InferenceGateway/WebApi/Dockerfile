# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN dotnet --version

# Copy solution and project files for restore
COPY ["Synaxis.sln", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/InferenceGateway/WebApi/Synaxis.InferenceGateway.WebApi.csproj", "src/InferenceGateway/WebApi/"]
COPY ["src/InferenceGateway/Application/Synaxis.InferenceGateway.Application.csproj", "src/InferenceGateway/Application/"]
COPY ["src/InferenceGateway/Infrastructure/Synaxis.InferenceGateway.Infrastructure.csproj", "src/InferenceGateway/Infrastructure/"]
COPY ["src/Synaxis.WebApp/Synaxis.WebApp.csproj", "src/Synaxis.WebApp/"]
COPY ["tests/InferenceGateway/Infrastructure.Tests/Synaxis.InferenceGateway.Infrastructure.Tests.csproj", "tests/InferenceGateway/Infrastructure.Tests/"]
COPY ["tests/InferenceGateway/Application.Tests/Synaxis.InferenceGateway.Application.Tests.csproj", "tests/InferenceGateway/Application.Tests/"]
COPY ["tests/InferenceGateway/IntegrationTests/Synaxis.InferenceGateway.IntegrationTests.csproj", "tests/InferenceGateway/IntegrationTests/"]
COPY ["tests/InferenceGateway.UnitTests/Synaxis.InferenceGateway.UnitTests.csproj", "tests/InferenceGateway.UnitTests/"]
COPY ["src/Tests/Benchmarks/Synaxis.Benchmarks.csproj", "src/Tests/Benchmarks/"]
COPY ["tests/Common/Synaxis.Common.Tests.csproj", "tests/Common/"]

RUN dotnet restore

# Copy everything else and build
COPY . .
WORKDIR "/src/src/InferenceGateway/WebApi"
RUN dotnet publish "Synaxis.InferenceGateway.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

# Install dependencies for GitHub Copilot SDK (gh CLI, nodejs, npm, copilot-cli)
RUN apt-get update && apt-get install -y curl gnupg ca-certificates nodejs npm \
    && mkdir -p -m 755 /etc/apt/keyrings \
    && curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | tee /etc/apt/keyrings/githubcli-archive-keyring.gpg > /dev/null \
    && chmod go+r /etc/apt/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
    && apt-get update && apt-get install -y gh \
    && npm install -g @githubnext/github-copilot-cli \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Synaxis.InferenceGateway.WebApi.dll"]
