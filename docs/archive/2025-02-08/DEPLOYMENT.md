# Deployment Guide

> **ULTRA MISER MODE™ Deployment Philosophy**: Why pay for managed Kubernetes when a $5 VPS and Docker Compose can route your AI requests for the price of a coffee? This guide shows you how to deploy Synaxis with maximum cost efficiency and minimum vendor lock-in.

## Table of Contents

- [Quick Start](#quick-start)
- [Docker Deployment](#docker-deployment)
- [Docker Compose Setup](#docker-compose-setup)
- [Environment Configuration](#environment-configuration)
- [Production Deployment](#production-deployment)
- [Reverse Proxy Setup](#reverse-proxy-setup)
- [Scaling Considerations](#scaling-considerations)
- [Monitoring & Health Checks](#monitoring--health-checks)
- [Troubleshooting](#troubleshooting)

---

## Quick Start

The fastest way to get Synaxis running (cost: $0 in infrastructure if you already have Docker):

```bash
# Clone the repository
git clone https://github.com/rudironsoni/Synaxis.git
cd Synaxis

# Copy environment template
cp .env.example .env

# Edit .env with your API keys (see Environment Configuration section)
nano .env

# Start with Docker Compose
docker compose up -d

# Verify deployment
curl http://localhost:8080/health/liveness
```

**Default Ports:**
- **Inference Gateway API**: http://localhost:8080
- **Web Admin UI**: http://localhost:8080/admin
- **PostgreSQL**: localhost:5435 (mapped from container 5432)
- **Redis**: localhost:6379

---

## Docker Deployment

### Prerequisites

- Docker Engine 24.0+
- Docker Compose 2.20+
- At least 2GB RAM available
- 10GB disk space for images and data

### Building Images

#### Inference Gateway

```bash
# Build the inference gateway image
docker build -f src/InferenceGateway/WebApi/Dockerfile -t synaxis/gateway:latest .

# Run standalone (not recommended for production - use Compose)
docker run -d \
  --name synaxis-gateway \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Synaxis__InferenceGateway__JwtSecret=your-secret-key \
  synaxis/gateway:latest
```

#### Web Admin UI

```bash
# Build the web app image
docker build -f src/Synaxis.WebApp/Dockerfile -t synaxis/webapp:latest .

# Run standalone
docker run -d \
  --name synaxis-webapp \
  -p 5002:8080 \
  -e GatewayUrl=http://gateway:8080 \
  synaxis/webapp:latest
```

### Image Optimization for ULTRA MISER MODE™

```dockerfile
# Multi-stage build example for minimal image size
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime image - Alpine variant for smaller footprint
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Synaxis.InferenceGateway.WebApi.dll"]
```

**Benefits:**
- ~50% smaller image size
- Faster deployment
- Lower memory footprint
- Perfect for resource-constrained environments

---

## Docker Compose Setup

### Development Configuration

The default `docker-compose.yml` provides a complete development environment:

```yaml
services:
  inference-gateway:
    image: synaxis/synaxis-inferencegateway:latest
    restart: unless-stopped
    build:
      context: .
      dockerfile: src/InferenceGateway/WebApi/Dockerfile
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      Synaxis__ControlPlane__ConnectionString: Host=postgres;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      ConnectionStrings__Redis: redis:6379,password=${REDIS_PASSWORD},abortConnect=false
      Synaxis__InferenceGateway__JwtSecret: ${JWT_SECRET}
      # Provider API keys...
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy

  webapp:
    image: synaxis/webapp:latest
    restart: unless-stopped
    build:
      context: .
      dockerfile: src/Synaxis.WebApp/Dockerfile
    ports:
      - "5002:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      GatewayUrl: http://inference-gateway:8080
    depends_on:
      - inference-gateway

  postgres:
    image: postgres:15-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 10s
      timeout: 5s
      retries: 5
    ports:
      - "5435:5432"
    volumes:
      - pgdata:/var/lib/postgresql/data

  redis:
    image: redis:alpine
    restart: unless-stopped
    command: redis-server --requirepass ${REDIS_PASSWORD}
    healthcheck:
      test: ["CMD-SHELL", "redis-cli -a ${REDIS_PASSWORD} ping | grep PONG"]
      interval: 10s
      timeout: 5s
      retries: 5
    ports:
      - "6379:6379"

volumes:
  pgdata:
```

### Infrastructure-Only Configuration

For development with local code execution, use `docker-compose.infrastructure.yml`:

```bash
# Start only infrastructure services
docker compose -f docker-compose.infrastructure.yml up -d

# Run gateway locally (for hot reload during development)
dotnet run --project src/InferenceGateway/WebApi
```

Services included:
- PostgreSQL 16
- Redis 7
- pgAdmin (dev profile)
- Jaeger (distributed tracing)

### Production Docker Compose

```yaml
version: '3.8'

services:
  inference-gateway:
    image: synaxis/gateway:${VERSION:-latest}
    restart: always
    deploy:
      replicas: 2
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      # ... other env vars
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/liveness"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    networks:
      - synaxis-network

  webapp:
    image: synaxis/webapp:${VERSION:-latest}
    restart: always
    deploy:
      replicas: 1
      resources:
        limits:
          cpus: '0.5'
          memory: 256M
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      GatewayUrl: http://inference-gateway:8080
    networks:
      - synaxis-network

  postgres:
    image: postgres:15-alpine
    restart: always
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    networks:
      - synaxis-network

  redis:
    image: redis:7-alpine
    restart: always
    command: redis-server --requirepass ${REDIS_PASSWORD} --maxmemory 256mb --maxmemory-policy allkeys-lru
    volumes:
      - redis-data:/data
    networks:
      - synaxis-network

  nginx:
    image: nginx:alpine
    restart: always
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl:/etc/nginx/ssl:ro
    depends_on:
      - inference-gateway
      - webapp
    networks:
      - synaxis-network

volumes:
  postgres-data:
  redis-data:

networks:
  synaxis-network:
    driver: bridge
```

---

## Environment Configuration

### Required Environment Variables

Create a `.env` file in the project root:

```bash
# Database Configuration
POSTGRES_DB=synaxis
POSTGRES_USER=synaxis
POSTGRES_PASSWORD=your-secure-password-here

# Redis Configuration
REDIS_PASSWORD=your-redis-password-here

# Security
JWT_SECRET=your-jwt-secret-minimum-32-characters-long

# Provider API Keys (add only the ones you use)
GROQ_API_KEY=your-groq-key
COHERE_API_KEY=your-cohere-key
CLOUDFLARE_API_KEY=your-cloudflare-key
CLOUDFLARE_ACCOUNT_ID=your-account-id
GEMINI_API_KEY=your-gemini-key
OPENROUTER_API_KEY=your-openrouter-key
DEEPSEEK_API_KEY=your-deepseek-key
DEEPSEEK_API_ENDPOINT=https://api.deepseek.com/v1
OPENAI_API_KEY=your-openai-key
OPENAI_API_ENDPOINT=https://api.openai.com/v1
NVIDIA_API_KEY=your-nvidia-key
HUGGINGFACE_API_KEY=your-huggingface-key
ANTIGRAVITY_PROJECT_ID=your-antigravity-project
ANTIGRAVITY_API_ENDPOINT=https://cloudcode-pa.googleapis.com
ANTIGRAVITY_API_ENDPOINT_FALLBACK=https://daily-cloudcode-pa.googleapis.com

# Optional: pgAdmin (dev only)
PGADMIN_DEFAULT_EMAIL=admin@example.com
PGADMIN_DEFAULT_PASSWORD=your-pgadmin-password
```

### Environment-Specific Configuration

#### Development

```bash
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:8080
```

#### Production

```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
# Disable detailed error pages
ASPNETCORE_DETAILEDERRORS=false
```

### Secrets Management (Production)

For production, avoid `.env` files. Use:

**Docker Secrets (Swarm mode):**
```bash
# Create secrets
echo "your-secret" | docker secret create jwt_secret -
echo "your-password" | docker secret create postgres_password -

# Use in compose file
secrets:
  jwt_secret:
    external: true
```

**Kubernetes Secrets:**
```bash
kubectl create secret generic synaxis-secrets \
  --from-literal=jwt-secret=your-secret \
  --from-literal=postgres-password=your-password
```

**Cloud Provider Secret Managers:**
- AWS Secrets Manager
- Azure Key Vault
- Google Secret Manager
- HashiCorp Vault

---

## Production Deployment

### Pre-Deployment Checklist

- [ ] JWT secret is at least 32 characters and cryptographically random
- [ ] Database passwords are strong and unique
- [ ] Redis is password-protected
- [ ] HTTPS is enabled with valid certificates
- [ ] Rate limiting is configured
- [ ] Health checks are enabled
- [ ] Logging is configured
- [ ] Backup strategy is in place
- [ ] Monitoring/alerting is set up

### Security Hardening

#### 1. Network Security

```yaml
# docker-compose.prod.yml
services:
  inference-gateway:
    # Only expose through reverse proxy
    expose:
      - "8080"
    # No direct port mapping
    # ports:
    #   - "8080:8080"  # DON'T DO THIS IN PRODUCTION
```

#### 2. Container Security

```dockerfile
# Run as non-root user
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
RUN adduser -D -s /bin/sh appuser
USER appuser
WORKDIR /app
COPY --chown=appuser:appuser . .
ENTRYPOINT ["dotnet", "Synaxis.InferenceGateway.WebApi.dll"]
```

#### 3. Resource Limits

```yaml
deploy:
  resources:
    limits:
      cpus: '1.0'
      memory: 512M
    reservations:
      cpus: '0.25'
      memory: 256M
```

### Database Migration Strategy

```bash
# Option 1: Automatic migrations (development only)
docker compose exec inference-gateway dotnet ef database update

# Option 2: Manual migration (recommended for production)
# 1. Backup database
# 2. Run migrations during maintenance window
# 3. Verify before switching traffic
```

### Backup Strategy

**Database Backup:**
```bash
#!/bin/bash
# backup.sh - Run as cron job
BACKUP_DIR="/backups/synaxis"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

# PostgreSQL backup
docker exec synaxis-postgres pg_dump -U synaxis synaxis > "$BACKUP_DIR/db_$TIMESTAMP.sql"

# Redis backup (if persistence enabled)
docker exec synaxis-redis redis-cli BGSAVE

# Cleanup old backups (keep 7 days)
find $BACKUP_DIR -name "*.sql" -mtime +7 -delete
```

**Configuration Backup:**
```bash
# Backup .env and compose files
tar czf "config_backup_$TIMESTAMP.tar.gz" .env docker-compose.yml nginx.conf/
```

### ULTRA MISER MODE™ Production Tips

1. **Use a cheap VPS**: Hetzner, DigitalOcean, or Vultr ($5-10/month)
2. **Share with friends**: Split costs across multiple users
3. **Use free tiers**: Cloudflare for DNS/CDN, Let's Encrypt for SSL
4. **Monitor quotas**: Set up alerts before hitting provider limits
5. **Rotate aggressively**: Configure short TTLs for provider switching
6. **Cache responses**: Redis caching for identical prompts
7. **Compress traffic**: Enable gzip/brotli at the reverse proxy

---

## Reverse Proxy Setup

### Nginx Configuration

#### Basic Setup

```nginx
# /etc/nginx/sites-available/synaxis
upstream synaxis_gateway {
    server localhost:8080;
    keepalive 32;
}

server {
    listen 80;
    server_name api.yourdomain.com;
    
    # Redirect HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name api.yourdomain.com;
    
    # SSL Configuration
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    
    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;
    
    # Rate limiting
    limit_req_zone $binary_remote_addr zone=api:10m rate=10r/s;
    limit_req zone=api burst=20 nodelay;
    
    # Proxy to Synaxis
    location / {
        proxy_pass http://synaxis_gateway;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Timeouts for long-running AI requests
        proxy_read_timeout 300s;
        proxy_send_timeout 300s;
        
        # Buffer settings for streaming
        proxy_buffering off;
        proxy_cache off;
    }
    
    # Health check endpoint (bypass rate limiting)
    location /health {
        proxy_pass http://synaxis_gateway;
        proxy_set_header Host $host;
        limit_req off;
    }
}
```

#### Admin UI Subdomain

```nginx
server {
    listen 443 ssl http2;
    server_name admin.yourdomain.com;
    
    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;
    
    # Restrict admin access by IP (optional)
    # allow 1.2.3.4;
    # deny all;
    
    location / {
        proxy_pass http://localhost:5002;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### Traefik Configuration

#### Docker Compose with Traefik

```yaml
version: '3.8'

services:
  traefik:
    image: traefik:v3.0
    restart: always
    command:
      - "--api.insecure=true"
      - "--providers.docker=true"
      - "--providers.docker.exposedbydefault=false"
      - "--entrypoints.web.address=:80"
      - "--entrypoints.websecure.address=:443"
      - "--certificatesresolvers.letsencrypt.acme.tlschallenge=true"
      - "--certificatesresolvers.letsencrypt.acme.email=admin@yourdomain.com"
      - "--certificatesresolvers.letsencrypt.acme.storage=/letsencrypt/acme.json"
      - "--accesslog=true"
      - "--log.level=INFO"
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - ./letsencrypt:/letsencrypt
    networks:
      - synaxis-network

  inference-gateway:
    image: synaxis/gateway:latest
    restart: always
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.gateway.rule=Host(`api.yourdomain.com`)"
      - "traefik.http.routers.gateway.entrypoints=websecure"
      - "traefik.http.routers.gateway.tls.certresolver=letsencrypt"
      - "traefik.http.services.gateway.loadbalancer.server.port=8080"
      - "traefik.http.middlewares.gateway-ratelimit.ratelimit.average=100"
      - "traefik.http.middlewares.gateway-ratelimit.ratelimit.burst=50"
      - "traefik.http.routers.gateway.middlewares=gateway-ratelimit"
    networks:
      - synaxis-network

  webapp:
    image: synaxis/webapp:latest
    restart: always
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.webapp.rule=Host(`admin.yourdomain.com`)"
      - "traefik.http.routers.webapp.entrypoints=websecure"
      - "traefik.http.routers.webapp.tls.certresolver=letsencrypt"
      - "traefik.http.services.webapp.loadbalancer.server.port=8080"
    networks:
      - synaxis-network

networks:
  synaxis-network:
    external: true
```

### Caddy Configuration

```caddyfile
# Caddyfile
api.yourdomain.com {
    reverse_proxy localhost:8080 {
        header_up Host {host}
        header_up X-Real-IP {remote}
        header_up X-Forwarded-For {remote}
        header_up X-Forwarded-Proto {scheme}
    }
    
    # Automatic HTTPS
    tls admin@yourdomain.com
    
    # Rate limiting (requires caddy-rate-limit module)
    rate_limit {
        zone static_api {
            key static
            events 100
            window 1m
        }
    }
}

admin.yourdomain.com {
    reverse_proxy localhost:5002
    tls admin@yourdomain.com
}
```

---

## Scaling Considerations

### Horizontal Scaling

#### Docker Swarm Mode

```bash
# Initialize swarm
docker swarm init

# Deploy stack
docker stack deploy -c docker-compose.swarm.yml synaxis
```

```yaml
# docker-compose.swarm.yml
version: '3.8'

services:
  inference-gateway:
    image: synaxis/gateway:latest
    deploy:
      replicas: 3
      update_config:
        parallelism: 1
        delay: 10s
        failure_action: rollback
      rollback_config:
        parallelism: 1
        delay: 10s
      restart_policy:
        condition: on-failure
        delay: 5s
        max_attempts: 3
      placement:
        constraints:
          - node.role == worker
    networks:
      - synaxis-network

  postgres:
    image: postgres:15-alpine
    deploy:
      replicas: 1
      placement:
        constraints:
          - node.labels.database == true
    volumes:
      - type: bind
        source: /mnt/postgres-data
        target: /var/lib/postgresql/data
    networks:
      - synaxis-network

networks:
  synaxis-network:
    driver: overlay
    attachable: true
```

#### Kubernetes Deployment

```yaml
# k8s-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: synaxis-gateway
  namespace: synaxis
spec:
  replicas: 3
  selector:
    matchLabels:
      app: synaxis-gateway
  template:
    metadata:
      labels:
        app: synaxis-gateway
    spec:
      containers:
      - name: gateway
        image: synaxis/gateway:latest
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: Synaxis__InferenceGateway__JwtSecret
          valueFrom:
            secretKeyRef:
              name: synaxis-secrets
              key: jwt-secret
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "1000m"
        livenessProbe:
          httpGet:
            path: /health/liveness
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/readiness
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: synaxis-gateway
  namespace: synaxis
spec:
  selector:
    app: synaxis-gateway
  ports:
  - port: 80
    targetPort: 8080
  type: ClusterIP
---
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: synaxis-ingress
  namespace: synaxis
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
    nginx.ingress.kubernetes.io/rate-limit: "100"
spec:
  tls:
  - hosts:
    - api.yourdomain.com
    secretName: synaxis-tls
  rules:
  - host: api.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: synaxis-gateway
            port:
              number: 80
```

### Database Scaling

**Read Replicas:**
```yaml
# Add read replica for scaling reads
postgres-replica:
  image: postgres:15-alpine
  environment:
    POSTGRES_DB: synaxis
    POSTGRES_USER: synaxis
    POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    PGDATA: /var/lib/postgresql/data/pgdata
  volumes:
    - pgdata-replica:/var/lib/postgresql/data
  command: |
    bash -c "
      pg_basebackup -h postgres -D /var/lib/postgresql/data/pgdata -U replicator -v -P -W
      echo 'standby_mode = on' >> /var/lib/postgresql/data/pgdata/recovery.conf
    "
```

### Redis Scaling

**Redis Cluster:**
```yaml
redis-cluster:
  image: grokzen/redis-cluster:latest
  environment:
    CLUSTER_ENABLED: "yes"
    CLUSTER_REQUIRE_FULL_COVERAGE: "no"
  ports:
    - "7000-7005:7000-7005"
```

---

## Monitoring & Health Checks

### Health Check Endpoints

Synaxis provides built-in health checks:

- **Liveness**: `GET /health/liveness` - Returns 200 if application is running
- **Readiness**: `GET /health/readiness` - Returns 200 if ready to accept traffic

### Docker Health Checks

```yaml
services:
  inference-gateway:
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/liveness"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

### Prometheus Metrics

Enable metrics export:

```yaml
environment:
  OTEL_EXPORTER_OTLP_ENDPOINT: http://jaeger:4317
  OTEL_SERVICE_NAME: synaxis-gateway
```

### Grafana Dashboard

Key metrics to monitor:
- Request rate and latency
- Provider response times
- Error rates by provider
- Token usage and quotas
- Cache hit/miss ratios
- Active connections

### Alerting Rules

```yaml
# prometheus-alerts.yml
groups:
  - name: synaxis
    rules:
      - alert: SynaxisHighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) > 0.1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High error rate detected"
          
      - alert: SynaxisProviderDown
        expr: up{job="synaxis-providers"} == 0
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "Provider {{ $labels.provider }} is down"
```

---

## Troubleshooting

### Common Issues

#### Container Won't Start

```bash
# Check logs
docker logs synaxis-inference-gateway

# Check for port conflicts
sudo netstat -tlnp | grep 8080

# Verify environment variables
docker exec synaxis-inference-gateway env
```

#### Database Connection Issues

```bash
# Test database connectivity
docker exec synaxis-inference-gateway nc -zv postgres 5432

# Check PostgreSQL logs
docker logs synaxis-postgres

# Verify connection string format
echo $Synaxis__ControlPlane__ConnectionString
```

#### Redis Connection Issues

```bash
# Test Redis
docker exec synaxis-redis redis-cli ping

# Check Redis authentication
docker exec synaxis-inference-gateway redis-cli -a $REDIS_PASSWORD ping
```

#### High Memory Usage

```bash
# Monitor container stats
docker stats

# Check for memory leaks
docker exec synaxis-inference-gateway dotnet-counters monitor

# Restart with memory limits
docker update --memory=512m --memory-swap=512m synaxis-inference-gateway
```

### Debug Mode

```bash
# Enable debug logging
docker compose exec inference-gateway \
  sh -c "export LOG_LEVEL=Debug && dotnet Synaxis.InferenceGateway.WebApi.dll"
```

### Getting Help

- Check [Troubleshooting Guide](ops/troubleshooting.md)
- Review [Monitoring Documentation](ops/monitoring.md)
- Open an issue on GitHub with logs and configuration

---

## Summary

**ULTRA MISER MODE™ Deployment Checklist:**

- [ ] Use smallest viable VPS ($5-10/month)
- [ ] Enable all free tiers from providers
- [ ] Configure aggressive provider rotation
- [ ] Set up automated backups
- [ ] Use Let's Encrypt (free SSL)
- [ ] Monitor quotas religiously
- [ ] Share costs with friends/colleagues
- [ ] Cache aggressively
- [ ] Compress all traffic
- [ ] Never pay for what you can get for free

Remember: The best deployment is one that routes AI requests for less than the cost of a coffee per month. Welcome to **ULTRA MISER MODE™** production deployment.
