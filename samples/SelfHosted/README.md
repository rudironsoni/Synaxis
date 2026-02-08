# Synaxis Self-Hosted Gateway Sample

A complete self-hosted Synaxis gateway with all transports (HTTP, gRPC, WebSocket), PostgreSQL, Redis, and observability tools.

## Features

- **Multiple Transports**: HTTP REST, gRPC, and WebSocket
- **Multiple Providers**: OpenAI and Anthropic support
- **Data Persistence**: PostgreSQL database
- **Caching**: Redis for performance optimization
- **Health Checks**: Kubernetes-ready readiness and liveness probes
- **Observability**: Prometheus metrics and Jaeger tracing
- **Containerized**: Docker Compose for easy deployment

## Prerequisites

- Docker and Docker Compose
- .NET 10.0 SDK (for local development)
- OpenAI and/or Anthropic API keys

## Quick Start with Docker Compose

1. Create a `.env` file in this directory:

```bash
OPENAI_API_KEY=your-openai-api-key-here
ANTHROPIC_API_KEY=your-anthropic-api-key-here
```

2. Start all services:

```bash
docker-compose up -d
```

3. Verify services are running:

```bash
docker-compose ps
```

4. Check gateway health:

```bash
curl http://localhost:5000/health/ready
```

## Services

| Service | Port | Description |
|---------|------|-------------|
| Synaxis HTTP | 5000 | REST API endpoints |
| Synaxis HTTPS | 5001 | Secure REST API |
| Synaxis gRPC | 5002 | gRPC services |
| PostgreSQL | 5432 | Database |
| Redis | 6379 | Cache |
| Jaeger UI | 16686 | Distributed tracing |
| Prometheus Metrics | 5000/metrics | Metrics endpoint |

## Usage Examples

### HTTP REST API

```bash
# Chat completion
curl -X POST http://localhost:5000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "messages": [
      {"role": "user", "content": "Hello!"}
    ],
    "model": "gpt-3.5-turbo"
  }'
```

### gRPC

```bash
# Using grpcurl
grpcurl -plaintext \
  -d '{
    "messages": [
      {"role": "user", "content": "Hello!"}
    ],
    "model": "gpt-3.5-turbo"
  }' \
  localhost:5002 synaxis.v1.ChatService/Chat
```

### WebSocket

```javascript
// JavaScript client
const ws = new WebSocket('ws://localhost:5000/ws');

ws.onopen = () => {
  ws.send(JSON.stringify({
    type: 'chat',
    messages: [
      { role: 'user', content: 'Hello!' }
    ],
    model: 'gpt-3.5-turbo'
  }));
};

ws.onmessage = (event) => {
  console.log('Response:', JSON.parse(event.data));
};
```

## Health Checks

### Readiness Probe

Checks if the service is ready to handle requests:

```bash
curl http://localhost:5000/health/ready
```

Returns 200 OK when:
- Application is started
- Redis connection is healthy
- PostgreSQL connection is healthy

### Liveness Probe

Checks if the service is alive:

```bash
curl http://localhost:5000/health/live
```

Always returns 200 OK if the process is running.

## Monitoring

### Prometheus Metrics

Access metrics at:
```bash
curl http://localhost:5000/metrics
```

Key metrics:
- `http_request_duration_seconds` - Request latency
- `http_requests_total` - Total requests
- `synaxis_provider_requests_total` - Provider-specific requests
- `dotnet_gc_collections_total` - Garbage collection stats

### Jaeger Tracing

Access Jaeger UI at: http://localhost:16686

View:
- Request traces across services
- Latency breakdown
- Error traces
- Service dependencies

## Local Development

### Run without Docker

1. Ensure PostgreSQL and Redis are running locally.

2. Update `appsettings.json`:

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "PostgreSQL": {
    "ConnectionString": "Host=localhost;Database=synaxis;Username=synaxis;Password=synaxis_password"
  }
}
```

3. Run the application:

```bash
dotnet run
```

### Build Docker Image

```bash
docker build -t synaxis-server -f Dockerfile ../..
```

## Configuration

### Environment Variables

- `OPENAI_API_KEY` - OpenAI API key (required for OpenAI provider)
- `ANTHROPIC_API_KEY` - Anthropic API key (required for Anthropic provider)
- `REDIS_CONNECTION_STRING` - Redis connection string
- `POSTGRESQL_CONNECTION_STRING` - PostgreSQL connection string
- `OTLP_ENDPOINT` - OpenTelemetry collector endpoint
- `ASPNETCORE_ENVIRONMENT` - Environment (Development/Production)

### Routing Strategy

Configure in `appsettings.json`:

```json
{
  "Synaxis": {
    "DefaultRoutingStrategy": "RoundRobin"
  }
}
```

Available strategies:
- `RoundRobin` - Distribute requests evenly
- `LeastLoaded` - Route to least loaded provider
- `Priority` - Use priority-based routing

## Production Deployment

### Kubernetes

Example deployment manifests are available in the `k8s/` directory (to be created).

Key considerations:
1. Use Kubernetes secrets for API keys
2. Configure resource limits
3. Set up HPA (Horizontal Pod Autoscaler)
4. Use persistent volumes for PostgreSQL
5. Configure ingress for external access

### Security Checklist

- [ ] Use HTTPS in production
- [ ] Store API keys in secrets manager
- [ ] Enable authentication/authorization
- [ ] Configure CORS appropriately
- [ ] Use network policies
- [ ] Enable audit logging
- [ ] Scan images for vulnerabilities

## Troubleshooting

### Service won't start

Check logs:
```bash
docker-compose logs synaxis
```

### Database connection issues

Verify PostgreSQL is healthy:
```bash
docker-compose logs postgres
docker exec -it synaxis-postgres pg_isready -U synaxis
```

### Redis connection issues

Verify Redis is healthy:
```bash
docker-compose logs redis
docker exec -it synaxis-redis redis-cli ping
```

### Performance issues

1. Check Prometheus metrics at http://localhost:5000/metrics
2. View traces in Jaeger at http://localhost:16686
3. Review resource usage: `docker stats`

## Cleanup

Stop and remove all containers:

```bash
docker-compose down
```

Remove volumes (WARNING: deletes all data):

```bash
docker-compose down -v
```

## Next Steps

- Configure multiple AI providers
- Set up monitoring dashboards (Grafana)
- Implement API authentication
- Add rate limiting
- Configure load balancing
- Explore advanced routing strategies

## Support

For issues and questions, see the main [documentation](../../README.md).
