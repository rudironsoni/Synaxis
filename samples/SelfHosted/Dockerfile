# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files
COPY ["samples/SelfHosted/SelfHosted.csproj", "samples/SelfHosted/"]
COPY ["src/Synaxis/Synaxis.csproj", "src/Synaxis/"]
COPY ["src/Synaxis.Abstractions/Synaxis.Abstractions.csproj", "src/Synaxis.Abstractions/"]
COPY ["src/Synaxis.Contracts/Synaxis.Contracts.csproj", "src/Synaxis.Contracts/"]
COPY ["src/Synaxis.Providers.OpenAI/Synaxis.Providers.OpenAI.csproj", "src/Synaxis.Providers.OpenAI/"]
COPY ["src/Synaxis.Providers.Anthropic/Synaxis.Providers.Anthropic.csproj", "src/Synaxis.Providers.Anthropic/"]
COPY ["src/Synaxis.Transport.Http/Synaxis.Transport.Http.csproj", "src/Synaxis.Transport.Http/"]
COPY ["src/Synaxis.Transport.Grpc/Synaxis.Transport.Grpc.csproj", "src/Synaxis.Transport.Grpc/"]
COPY ["src/Synaxis.Transport.WebSocket/Synaxis.Transport.WebSocket.csproj", "src/Synaxis.Transport.WebSocket/"]
COPY ["Directory.Packages.props", "./"]

# Restore dependencies
RUN dotnet restore "samples/SelfHosted/SelfHosted.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/samples/SelfHosted"
RUN dotnet build "SelfHosted.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "SelfHosted.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN useradd -m -s /bin/bash appuser && chown -R appuser /app
USER appuser

# Copy published application
COPY --from=publish /app/publish .

# Expose ports
EXPOSE 5000 5001 5002

# Set entry point
ENTRYPOINT ["dotnet", "SelfHosted.dll"]
