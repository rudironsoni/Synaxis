FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["src/Synaplexer/Synaplexer.API/Synaplexer.API.csproj", "src/Synaplexer/Synaplexer.API/"]
COPY ["src/Synaplexer/Synaplexer.Application/Synaplexer.Application.csproj", "src/Synaplexer/Synaplexer.Application/"]
COPY ["src/Synaplexer/Synaplexer.Domain/Synaplexer.Domain.csproj", "src/Synaplexer/Synaplexer.Domain/"]
COPY ["src/Synaplexer/Synaplexer.Infrastructure/Synaplexer.Infrastructure.csproj", "src/Synaplexer/Synaplexer.Infrastructure/"]
COPY ["src/Synaplexer/Synaplexer.Contracts/Synaplexer.Contracts.csproj", "src/Synaplexer/Synaplexer.Contracts/"]
COPY ["src/Synaplexer/Synaplexer.ServiceDefaults/Synaplexer.ServiceDefaults.csproj", "src/Synaplexer/Synaplexer.ServiceDefaults/"]
COPY ["src/Synaplexer/Synaplexer.EventBus/Synaplexer.EventBus.csproj", "src/Synaplexer/Synaplexer.EventBus/"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

RUN dotnet restore "src/Synaplexer/Synaplexer.API/Synaplexer.API.csproj"

# Copy everything
COPY . .
WORKDIR "/src/src/Synaplexer/Synaplexer.API"
RUN dotnet build "Synaplexer.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Synaplexer.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Synaplexer.API.dll"]
