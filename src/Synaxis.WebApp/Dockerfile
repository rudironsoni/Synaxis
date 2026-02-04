# Stage 1: Build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Disable telemetry for faster builds
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

# Install Node.js for frontend build (required by csproj npm targets)
RUN apk add --no-cache nodejs npm

COPY Directory.Packages.props .
COPY src/Synaxis.WebApp/Synaxis.WebApp.csproj src/Synaxis.WebApp/
RUN dotnet restore src/Synaxis.WebApp/Synaxis.WebApp.csproj

# Copy package files first for better layer caching
COPY src/Synaxis.WebApp/ClientApp/package*.json src/Synaxis.WebApp/ClientApp/
RUN cd src/Synaxis.WebApp/ClientApp && npm install

# Copy remaining source (excluding obj/ for clean build)
COPY src/Synaxis.WebApp/*.cs src/Synaxis.WebApp/
COPY src/Synaxis.WebApp/ClientApp/ src/Synaxis.WebApp/ClientApp/
COPY src/Synaxis.WebApp/Properties/ src/Synaxis.WebApp/Properties/
COPY src/Synaxis.WebApp/appsettings*.json src/Synaxis.WebApp/
WORKDIR /src/src/Synaxis.WebApp
RUN dotnet publish -c Release -o /app/publish --no-restore /p:EnableCompressionInSingleFile=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
EXPOSE 54873

# Set ASP.NET to listen on the correct port
ENV ASPNETCORE_URLS=http://+:54873

# Add non-root user
RUN adduser -D -s /bin/sh appuser && chown -R appuser /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:54873/health || exit 1

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Synaxis.WebApp.dll"]
