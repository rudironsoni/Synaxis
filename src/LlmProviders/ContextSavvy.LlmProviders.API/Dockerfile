# Base image with Playwright/Patchright dependencies
FROM mcr.microsoft.com/playwright:v1.44.0-jammy AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install .NET 10 runtime
RUN apt-get update && apt-get install -y wget && \
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh && \
    chmod +x dotnet-install.sh && \
    ./dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet && \
    ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet && \
    rm dotnet-install.sh

# Install Chrome for Patchright stealth
RUN npx playwright install chrome

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["src/Services/LlmProviders/ContextSavvy.LlmProviders.API/ContextSavvy.LlmProviders.API.csproj", "src/Services/LlmProviders/ContextSavvy.LlmProviders.API/"]
COPY ["src/Services/LlmProviders/ContextSavvy.LlmProviders.Application/ContextSavvy.LlmProviders.Application.csproj", "src/Services/LlmProviders/ContextSavvy.LlmProviders.Application/"]
COPY ["src/Services/LlmProviders/ContextSavvy.LlmProviders.Domain/ContextSavvy.LlmProviders.Domain.csproj", "src/Services/LlmProviders/ContextSavvy.LlmProviders.Domain/"]
COPY ["src/Services/LlmProviders/ContextSavvy.LlmProviders.Infrastructure/ContextSavvy.LlmProviders.Infrastructure.csproj", "src/Services/LlmProviders/ContextSavvy.LlmProviders.Infrastructure/"]
COPY ["src/Shared/ContextSavvy.Ghost.Infrastructure/ContextSavvy.Ghost.Infrastructure.csproj", "src/Shared/ContextSavvy.Ghost.Infrastructure/"]
COPY ["src/Shared/ContextSavvy.Contracts/ContextSavvy.Contracts.csproj", "src/Shared/ContextSavvy.Contracts/"]
COPY ["src/Shared/ContextSavvy.ServiceDefaults/ContextSavvy.ServiceDefaults.csproj", "src/Shared/ContextSavvy.ServiceDefaults/"]
COPY ["src/Shared/ContextSavvy.EventBus/ContextSavvy.EventBus.csproj", "src/Shared/ContextSavvy.EventBus/"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

RUN dotnet restore "src/Services/LlmProviders/ContextSavvy.LlmProviders.API/ContextSavvy.LlmProviders.API.csproj"

# Copy everything
COPY . .
WORKDIR "/src/src/Services/LlmProviders/ContextSavvy.LlmProviders.API"
RUN dotnet build "ContextSavvy.LlmProviders.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ContextSavvy.LlmProviders.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ContextSavvy.LlmProviders.API.dll"]
