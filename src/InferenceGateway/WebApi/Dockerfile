# Build Stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Disable telemetry for faster builds
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Copy only required project files for restore
COPY ["Directory.Packages.props", "./"]
COPY ["src/InferenceGateway/WebApi/Synaxis.InferenceGateway.WebApi.csproj", "src/InferenceGateway/WebApi/"]
COPY ["src/InferenceGateway/Application/Synaxis.InferenceGateway.Application.csproj", "src/InferenceGateway/Application/"]
COPY ["src/InferenceGateway/Infrastructure/Synaxis.InferenceGateway.Infrastructure.csproj", "src/InferenceGateway/Infrastructure/"]

RUN dotnet restore src/InferenceGateway/WebApi/Synaxis.InferenceGateway.WebApi.csproj

# Copy source files, excluding obj/bin directories
COPY ["src/InferenceGateway/Application/", "src/InferenceGateway/Application/"]
COPY ["src/InferenceGateway/Infrastructure/", "src/InferenceGateway/Infrastructure/"]
COPY ["src/InferenceGateway/WebApi/", "src/InferenceGateway/WebApi/"]

# Remove any obj/bin directories that might have been copied despite .dockerignore
# This prevents stale build artifacts with .resx references from causing build errors
RUN find /src/src/InferenceGateway -type d -name "bin" -prune -exec rm -rf {} \; 2>/dev/null || true
RUN find /src/src/InferenceGateway -type d -name "obj" -prune -exec rm -rf {} \; 2>/dev/null || true

WORKDIR /src/src/InferenceGateway/WebApi
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
EXPOSE 54872

# Install required libraries for Npgsql and internationalization
RUN apk add --no-cache krb5-libs icu-libs

# Set ASP.NET to listen on the correct port
ENV ASPNETCORE_URLS=http://+:54872

# Add non-root user
RUN adduser -D -s /bin/sh appuser && chown -R appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget -qO- http://localhost:54872/health/liveness 2>/dev/null | grep -q 'healthy' || exit 1

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Synaxis.InferenceGateway.WebApi.dll"]
