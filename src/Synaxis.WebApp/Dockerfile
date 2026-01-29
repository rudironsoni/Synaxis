# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Install Node.js for frontend build targets
RUN apt-get update && apt-get install -y nodejs npm

COPY Directory.Packages.props .
COPY src/Synaxis.WebApp/Synaxis.WebApp.csproj src/Synaxis.WebApp/
RUN dotnet restore src/Synaxis.WebApp/Synaxis.WebApp.csproj

COPY src/Synaxis.WebApp/ src/Synaxis.WebApp/
WORKDIR /src/src/Synaxis.WebApp
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Synaxis.WebApp.dll"]
