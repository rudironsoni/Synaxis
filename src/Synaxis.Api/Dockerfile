# Build Stage
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Disable telemetry for faster builds
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Copy only required project files for restore
COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/Synaxis.Api/Synaxis.Api.csproj", "src/Synaxis.Api/"]
COPY ["src/Synaxis.Core/Synaxis.Core.csproj", "src/Synaxis.Core/"]
COPY ["src/Synaxis.Infrastructure/Synaxis.Infrastructure.csproj", "src/Synaxis.Infrastructure/"]

RUN dotnet restore src/Synaxis.Api/Synaxis.Api.csproj

# Copy source files, excluding obj/bin directories
COPY ["src/Synaxis.Api/", "src/Synaxis.Api/"]
COPY ["src/Synaxis.Core/", "src/Synaxis.Core/"]
COPY ["src/Synaxis.Infrastructure/", "src/Synaxis.Infrastructure/"]

# Remove any obj/bin directories that might have been copied despite .dockerignore
RUN find /src/src -type d -name "bin" -prune -exec rm -rf {} \; 2>/dev/null || true
RUN find /src/src -type d -name "obj" -prune -exec rm -rf {} \; 2>/dev/null || true

WORKDIR /src/src/Synaxis.Api
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false /p:TreatWarningsAsErrors=false /p:EnforceCodeStyleInBuild=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
EXPOSE 8080

# Install required libraries for Npgsql and internationalization
RUN apk add --no-cache krb5-libs icu-libs

# Set ASP.NET to listen on the correct port
ENV ASPNETCORE_URLS=http://+:8080

# Add non-root user
RUN adduser -D -s /bin/sh appuser && chown -R appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget -qO- http://localhost:8080/health 2>/dev/null || exit 1

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Synaxis.Api.dll"]
