# ElastiCache Redis 7 Configuration for Brazil Region
# Multi-node cluster with encryption at rest and in transit

# Security Group for ElastiCache
resource "aws_security_group" "elasticache" {
  name        = "synaxis-brazil-elasticache-sg-${var.environment}"
  description = "Security group for ElastiCache Redis"
  vpc_id      = aws_vpc.brazil.id

  ingress {
    description     = "Redis from EKS"
    from_port       = 6379
    to_port         = 6379
    protocol        = "tcp"
    security_groups = [aws_eks_cluster.brazil.vpc_config[0].cluster_security_group_id]
  }

  ingress {
    description     = "Redis from Qdrant"
    from_port       = 6379
    to_port         = 6379
    protocol        = "tcp"
    security_groups = [aws_security_group.qdrant.id]
  }

  egress {
    description = "Allow all outbound traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "synaxis-brazil-elasticache-sg-${var.environment}"
  }
}

# ElastiCache Parameter Group for Redis 7
resource "aws_elasticache_parameter_group" "brazil" {
  name   = "synaxis-brazil-redis7-${var.environment}"
  family = var.redis_parameter_group_family

  parameter {
    name  = "maxmemory-policy"
    value = "allkeys-lru"
  }

  parameter {
    name  = "timeout"
    value = "300"
  }

  parameter {
    name  = "tcp-keepalive"
    value = "300"
  }

  parameter {
    name  = "notify-keyspace-events"
    value = "Ex"
  }

  tags = {
    Name = "synaxis-brazil-redis7-${var.environment}"
  }
}

# ElastiCache Redis Cluster
resource "aws_elasticache_cluster" "brazil" {
  cluster_id           = "synaxis-brazil-${var.environment}"
  engine               = "redis"
  engine_version       = var.redis_engine_version
  node_type            = var.redis_node_type
  num_cache_nodes      = var.redis_num_cache_nodes
  parameter_group_name = aws_elasticache_parameter_group.brazil.name
  port                 = 6379

  # Network configuration
  subnet_group_name  = aws_elasticache_subnet_group.brazil.name
  security_group_ids = [aws_security_group.elasticache.id]

  # Encryption at rest
  at_rest_encryption_enabled = true
  kms_key_id                 = aws_kms_key.synaxis_brazil.arn

  # Encryption in transit
  transit_encryption_enabled = true
  auth_token_enabled         = true

  # Maintenance and snapshots
  maintenance_window         = "sun:05:00-sun:06:00"
  snapshot_window            = "03:00-04:00"
  snapshot_retention_limit   = 7
  auto_minor_version_upgrade = true

  # Logging
  log_delivery_configuration {
    destination      = aws_cloudwatch_log_group.redis_slow_log.name
    destination_type = "cloudwatch-logs"
    log_format       = "json"
    log_type         = "slow-log"
  }

  log_delivery_configuration {
    destination      = aws_cloudwatch_log_group.redis_engine_log.name
    destination_type = "cloudwatch-logs"
    log_format       = "json"
    log_type         = "engine-log"
  }

  tags = {
    Name       = "synaxis-brazil-redis-${var.environment}"
    Compliance = "LGPD"
  }
}

# CloudWatch Log Groups for Redis
resource "aws_cloudwatch_log_group" "redis_slow_log" {
  name              = "/aws/elasticache/synaxis-brazil-slow-log-${var.environment}"
  retention_in_days = var.log_retention_days
  kms_key_id        = aws_kms_key.synaxis_brazil.arn

  tags = {
    Name = "synaxis-brazil-redis-slow-log"
  }
}

resource "aws_cloudwatch_log_group" "redis_engine_log" {
  name              = "/aws/elasticache/synaxis-brazil-engine-log-${var.environment}"
  retention_in_days = var.log_retention_days
  kms_key_id        = aws_kms_key.synaxis_brazil.arn

  tags = {
    Name = "synaxis-brazil-redis-engine-log"
  }
}

# CloudWatch Alarms for ElastiCache
resource "aws_cloudwatch_metric_alarm" "redis_cpu" {
  alarm_name          = "synaxis-brazil-redis-cpu-${var.environment}"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "CPUUtilization"
  namespace           = "AWS/ElastiCache"
  period              = 300
  statistic           = "Average"
  threshold           = 75
  alarm_description   = "This metric monitors ElastiCache CPU utilization"
  treat_missing_data  = "notBreaching"

  dimensions = {
    CacheClusterId = aws_elasticache_cluster.brazil.id
  }

  tags = {
    Name = "synaxis-brazil-redis-cpu-alarm"
  }
}

resource "aws_cloudwatch_metric_alarm" "redis_memory" {
  alarm_name          = "synaxis-brazil-redis-memory-${var.environment}"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 2
  metric_name         = "DatabaseMemoryUsagePercentage"
  namespace           = "AWS/ElastiCache"
  period              = 300
  statistic           = "Average"
  threshold           = 80
  alarm_description   = "This metric monitors ElastiCache memory usage"
  treat_missing_data  = "notBreaching"

  dimensions = {
    CacheClusterId = aws_elasticache_cluster.brazil.id
  }

  tags = {
    Name = "synaxis-brazil-redis-memory-alarm"
  }
}

resource "aws_cloudwatch_metric_alarm" "redis_evictions" {
  alarm_name          = "synaxis-brazil-redis-evictions-${var.environment}"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "Evictions"
  namespace           = "AWS/ElastiCache"
  period              = 300
  statistic           = "Sum"
  threshold           = 1000
  alarm_description   = "This metric monitors ElastiCache evictions"
  treat_missing_data  = "notBreaching"

  dimensions = {
    CacheClusterId = aws_elasticache_cluster.brazil.id
  }

  tags = {
    Name = "synaxis-brazil-redis-evictions-alarm"
  }
}

# Generate Redis auth token
resource "random_password" "redis_auth_token" {
  length  = 32
  special = true
  # Redis auth token requirements
  override_special = "!&#$^<>-"
}

# Store Redis auth token in Secrets Manager
resource "aws_secretsmanager_secret" "redis_auth_token" {
  name                    = "synaxis-brazil-redis-auth-token-${random_id.suffix.hex}"
  kms_key_id              = aws_kms_key.synaxis_brazil.arn
  recovery_window_in_days = 7

  tags = {
    Name = "synaxis-brazil-redis-auth-token"
  }
}

resource "aws_secretsmanager_secret_version" "redis_auth_token" {
  secret_id     = aws_secretsmanager_secret.redis_auth_token.id
  secret_string = random_password.redis_auth_token.result
}
