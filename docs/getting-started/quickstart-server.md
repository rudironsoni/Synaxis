# Quickstart: Synaxis Server

This guide helps you deploy a self-hosted Synaxis Inference Gateway in about 15 minutes.

## Prerequisites

- **Docker** 24.0+ ([Install](https://docs.docker.com/get-docker/))
- **Docker Compose** 2.20+ (included with Docker Desktop)
- **4GB RAM** minimum (8GB recommended)
- **API Keys**: OpenAI, Anthropic, or other providers

## Installation

### Option 1: Docker Compose (Recommended)

Create a project directory and configuration:

```bash
mkdir synaxis-gateway
cd synaxis-gateway
```

Create a `.env` file:

```bash
# .env
POSTGRES_DB=synaxis
POSTGRES_USER=synaxis
POSTGRES_PASSWORD=your-secure-password-here
REDIS_PASSWORD=your-redis-password-here

# JWT Configuration
JWT_SECRET=your-super-secret-jwt-key-minimum-32-characters

# Provider API Keys
OPENAI_API_KEY=sk-...
ANTHROPIC_API_KEY=sk-ant-...
GROQ_API_KEY=gsk_...
GEMINI_API_KEY=...

# Optional: Additional providers
COHERE_API_KEY=
DEEPSEEK_API_KEY=
NVIDIA_API_KEY=
HUGGINGFACE_API_KEY=

# Qdrant Vector Database
QDRANT_API_KEY=your-qdrant-api-key
QDRANT_COLLECTION_NAME=synaxis_vectors

# Monitoring (optional)
PGADMIN_DEFAULT_EMAIL=admin@synaxis.local
PGADMIN_DEFAULT_PASSWORD=admin
GF_SECURITY_ADMIN_USER=admin
GF_SECURITY_ADMIN_PASSWORD=admin
```

Create `docker-compose.yml`:

```yaml
services:
  synaxis-gateway:
    image: synaxis/inference-gateway:latest
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: Host=postgres;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      ConnectionStrings__Redis: redis:6379,password=${REDIS_PASSWORD},abortConnect=false
      Synaxis__InferenceGateway__JwtSecret: ${JWT_SECRET}
      Synaxis__InferenceGateway__Providers__OpenAI__Key: ${OPENAI_API_KEY}
      Synaxis__InferenceGateway__Providers__Anthropic__Key: ${ANTHROPIC_API_KEY}
      Synaxis__InferenceGateway__Providers__Groq__Key: ${GROQ_API_KEY}
      Synaxis__InferenceGateway__Providers__Gemini__Key: ${GEMINI_API_KEY}
      Qdrant__Endpoint: http://qdrant:6333
      Qdrant__ApiKey: ${QDRANT_API_KEY}
      Qdrant__CollectionName: ${QDRANT_COLLECTION_NAME}
      OTEL_EXPORTER_OTLP_ENDPOINT: http://jaeger:4317
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
      qdrant:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "wget", "-q", "--spider", "http://localhost:8080/health/liveness"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 60s
    networks:
      - synaxis-network

  postgres:
    image: postgres:16
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 5s
      timeout: 3s
      retries: 10
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    networks:
      - synaxis-network

  redis:
    image: redis:7
    restart: unless-stopped
    command: ["redis-server", "--requirepass", "${REDIS_PASSWORD}"]
    healthcheck:
      test: ["CMD-SHELL", "redis-cli -a ${REDIS_PASSWORD} ping | grep PONG"]
      interval: 5s
      timeout: 3s
      retries: 10
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    networks:
      - synaxis-network

  qdrant:
    image: qdrant/qdrant:latest
    restart: unless-stopped
    ports:
      - "6333:6333"
    environment:
      QDRANT__SERVICE__API_KEY: ${QDRANT_API_KEY}
    volumes:
      - qdrant-data:/qdrant/storage
    networks:
      - synaxis-network
    healthcheck:
      test: ["CMD-SHELL", "wget -qO- http://localhost:6333/healthz | grep -q 'ok'"]
      interval: 10s
      timeout: 5s
      retries: 5

  jaeger:
    image: jaegertracing/all-in-one:latest
    restart: unless-stopped
    ports:
      - "16686:16686"  # Jaeger UI
      - "4317:4317"    # OTLP gRPC
    networks:
      - synaxis-network

volumes:
  postgres-data:
  redis-data:
  qdrant-data:

networks:
  synaxis-network:
    driver: bridge
```

### Option 2: Docker Run

For a minimal setup without dependencies:

```bash
docker run -d \
  --name synaxis-gateway \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Synaxis__InferenceGateway__JwtSecret=your-jwt-secret \
  -e Synaxis__InferenceGateway__Providers__OpenAI__Key=sk-... \
  synaxis/inference-gateway:latest
```

## Start the Server

### Using Docker Compose

```bash
docker-compose up -d
```

**Check status:**

```bash
docker-compose ps
```

**View logs:**

```bash
docker-compose logs -f synaxis-gateway
```

**Expected output:**

```
synaxis-gateway  | [INF] Starting web host
synaxis-gateway  | [INF] Database initialization succeeded
synaxis-gateway  | [INF] Now listening on: http://[::]:8080
synaxis-gateway  | [INF] Application started. Press Ctrl+C to shut down.
```

### Health Check

Verify the gateway is running:

```bash
curl http://localhost:8080/health/liveness
```

**Expected response:**

```json
{
  "status": "Healthy",
  "checks": []
}
```

## Test with curl

### Create a User

```bash
curl -X POST http://localhost:8080/api/identity/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }'
```

### Get Authentication Token

```bash
curl -X POST http://localhost:8080/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePassword123!"
  }'
```

**Response:**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-02-09T00:00:00Z"
}
```

### Chat Completion

```bash
curl -X POST http://localhost:8080/v1/chat/completions \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [
      {
        "role": "user",
        "content": "Hello, how are you?"
      }
    ]
  }'
```

**Response:**

```json
{
  "id": "chatcmpl-abc123",
  "object": "chat.completion",
  "created": 1738972800,
  "model": "gpt-4",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "I'm doing well, thank you! How can I help you today?"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 12,
    "completion_tokens": 15,
    "total_tokens": 27
  }
}
```

### Streaming Completion

```bash
curl -X POST http://localhost:8080/v1/chat/completions \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4",
    "messages": [{"role": "user", "content": "Write a haiku"}],
    "stream": true
  }'
```

## Deploy with Docker

### Build Custom Image

If you need customization:

```bash
git clone https://github.com/synaxis/synaxis.git
cd synaxis
docker build -t myorg/synaxis-gateway:latest \
  -f src/InferenceGateway/WebApi/Dockerfile .
```

### Push to Registry

```bash
docker tag myorg/synaxis-gateway:latest myregistry.io/synaxis-gateway:latest
docker push myregistry.io/synaxis-gateway:latest
```

### Production Deployment

**docker-compose.prod.yml:**

```yaml
services:
  synaxis-gateway:
    image: myregistry.io/synaxis-gateway:latest
    restart: always
    deploy:
      replicas: 3
      resources:
        limits:
          cpus: '2'
          memory: 4G
        reservations:
          cpus: '1'
          memory: 2G
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_HTTPS_PORT: 443
    volumes:
      - /etc/ssl/certs:/etc/ssl/certs:ro
    networks:
      - synaxis-network
```

Deploy:

```bash
docker-compose -f docker-compose.prod.yml up -d
```

## Next Steps

- **[Configuration](../configuration.md)**: Advanced server configuration
- **[Security](../security.md)**: SSL/TLS, authentication, and hardening
- **[Monitoring](../monitoring.md)**: Metrics, tracing, and alerting
- **[Scaling](../scaling.md)**: Load balancing and horizontal scaling
- **[Backup](../backup.md)**: Database and state management

## Troubleshooting

### Container Fails to Start

**Issue:** Gateway exits immediately

**Solution:**
1. Check logs: `docker-compose logs synaxis-gateway`
2. Verify environment variables in `.env`
3. Ensure JWT_SECRET is at least 32 characters
4. Check database connectivity: `docker-compose logs postgres`

### Database Connection Failed

**Error:** `Failed to connect to database`

**Solution:**
1. Verify postgres is healthy: `docker-compose ps postgres`
2. Check connection string in environment variables
3. Ensure database initialized: `docker-compose logs postgres | grep "database system is ready"`
4. Wait for healthcheck: `docker-compose up -d && sleep 30`

### Provider Authentication Failed

**Error:** `401 Unauthorized` from provider

**Solution:**
1. Verify API keys in `.env` file
2. Check key format (OpenAI: `sk-...`, Anthropic: `sk-ant-...`)
3. Restart after updating: `docker-compose restart synaxis-gateway`
4. View configuration: `docker-compose exec synaxis-gateway env | grep API_KEY`

### Port Already in Use

**Error:** `bind: address already in use`

**Solution:**
Change ports in `docker-compose.yml`:

```yaml
ports:
  - "9090:8080"  # Changed from 8080:8080
```

### High Memory Usage

**Issue:** Container using excessive RAM

**Solution:**
Add resource limits:

```yaml
services:
  synaxis-gateway:
    deploy:
      resources:
        limits:
          memory: 2G
```

### Slow First Request

**Issue:** Initial request takes 30+ seconds

**Solution:**
This is normal during cold start. The gateway:
1. Initializes database connections
2. Loads provider configurations
3. Establishes connection pools

Subsequent requests will be fast. To verify:

```bash
time curl http://localhost:8080/health/readiness
```

## Monitoring

Access monitoring tools:

- **Jaeger UI**: http://localhost:16686 (Distributed tracing)
- **Health Endpoint**: http://localhost:8080/health
- **Metrics**: http://localhost:8080/metrics (if enabled)

## Updating

Pull latest version:

```bash
docker-compose pull
docker-compose up -d
```

**Backup before updating:**

```bash
docker-compose exec postgres pg_dump -U synaxis synaxis > backup.sql
```

## Support

- **Documentation**: [https://docs.synaxis.io](https://docs.synaxis.io)
- **GitHub Issues**: [https://github.com/synaxis/synaxis/issues](https://github.com/synaxis/synaxis/issues)
- **Community**: [Discord](https://discord.gg/synaxis)
