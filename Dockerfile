# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first for caching
COPY ["src/Synaxis.WebApi/Synaxis.WebApi.csproj", "src/Synaxis.WebApi/"]
COPY ["src/Synaxis.Application/Synaxis.Application.csproj", "src/Synaxis.Application/"]
COPY ["src/Synaxis.Infrastructure/Synaxis.Infrastructure.csproj", "src/Synaxis.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/Synaxis.WebApi/Synaxis.WebApi.csproj"

# Copy the rest of the source code
COPY . .

# Build and publish
WORKDIR "/src/src/Synaxis.WebApi"
RUN dotnet publish "Synaxis.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create non-root user
USER app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Synaxis.WebApi.dll"]
