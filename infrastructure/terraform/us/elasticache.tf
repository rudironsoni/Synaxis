# ElastiCache Subnet Group
resource "aws_elasticache_subnet_group" "redis" {
  name       = "${var.project_name}-redis-subnet-group-${var.environment}"
  subnet_ids = aws_subnet.database[*].id

  tags = {
    Name = "${var.project_name}-redis-subnet-group-${var.environment}"
  }
}

# ElastiCache Parameter Group
resource "aws_elasticache_parameter_group" "redis" {
  name   = "${var.project_name}-redis7-${var.environment}"
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
    name  = "maxmemory-samples"
    value = "5"
  }

  tags = {
    Name = "${var.project_name}-redis7-${var.environment}"
  }
}

# Generate auth token for Redis
resource "random_password" "redis_auth_token" {
  length  = 32
  special = false # ElastiCache has restrictions on special characters
}

# Store Redis auth token in Secrets Manager
resource "aws_secretsmanager_secret" "redis_auth_token" {
  name                    = "${var.project_name}-redis-auth-${var.environment}-${random_id.suffix.hex}"
  description             = "Redis authentication token for Synaxis"
  kms_key_id              = aws_kms_key.synaxis.id
  recovery_window_in_days = 30

  tags = {
    Name = "${var.project_name}-redis-auth-${var.environment}"
  }
}

resource "aws_secretsmanager_secret_version" "redis_auth_token" {
  secret_id = aws_secretsmanager_secret.redis_auth_token.id
  secret_string = jsonencode({
    auth_token = random_password.redis_auth_token.result
    endpoint   = aws_elasticache_replication_group.redis.configuration_endpoint_address
    port       = aws_elasticache_replication_group.redis.port
  })
}

# ElastiCache Replication Group (Cluster Mode Enabled)
resource "aws_elasticache_replication_group" "redis" {
  replication_group_id = "${var.project_name}-redis-${var.environment}"
  description          = "Redis cluster for Synaxis ${var.environment}"

  engine               = "redis"
  engine_version       = var.redis_engine_version
  port                 = 6379
  parameter_group_name = aws_elasticache_parameter_group.redis.name
  node_type            = var.redis_node_type

  # Number of cache clusters (primary + replicas) per shard
  num_cache_clusters = var.redis_num_cache_nodes

  # Security
  subnet_group_name    = aws_elasticache_subnet_group.redis.name
  security_group_ids   = [aws_security_group.redis.id]
  at_rest_encryption_enabled = true
  transit_encryption_enabled = true
  auth_token                 = random_password.redis_auth_token.result
  kms_key_id                = aws_kms_key.synaxis.arn

  # Automatic failover must be enabled for multi-AZ
  automatic_failover_enabled = true
  multi_az_enabled          = true

  # Backup configuration
  snapshot_retention_limit = 5
  snapshot_window         = "03:00-05:00"
  maintenance_window      = "mon:05:00-mon:07:00"

  # Auto minor version upgrade
  auto_minor_version_upgrade = true

  # Notification configuration
  notification_topic_arn = aws_sns_topic.redis_notifications.arn

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
    Name = "${var.project_name}-redis-${var.environment}"
  }
}

# CloudWatch Log Groups for Redis
resource "aws_cloudwatch_log_group" "redis_slow_log" {
  name              = "/aws/elasticache/${var.project_name}-redis-slow-log-${var.environment}"
  retention_in_days = 7
  kms_key_id        = aws_kms_key.synaxis.arn

  tags = {
    Name = "${var.project_name}-redis-slow-log-${var.environment}"
  }
}

resource "aws_cloudwatch_log_group" "redis_engine_log" {
  name              = "/aws/elasticache/${var.project_name}-redis-engine-log-${var.environment}"
  retention_in_days = 7
  kms_key_id        = aws_kms_key.synaxis.arn

  tags = {
    Name = "${var.project_name}-redis-engine-log-${var.environment}"
  }
}

# SNS Topic for Redis notifications
resource "aws_sns_topic" "redis_notifications" {
  name              = "${var.project_name}-redis-notifications-${var.environment}"
  kms_master_key_id = aws_kms_key.synaxis.id

  tags = {
    Name = "${var.project_name}-redis-notifications-${var.environment}"
  }
}

# CloudWatch Alarms for Redis
resource "aws_cloudwatch_metric_alarm" "redis_cpu" {
  alarm_name          = "${var.project_name}-redis-high-cpu-${var.environment}"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "CPUUtilization"
  namespace           = "AWS/ElastiCache"
  period              = "300"
  statistic           = "Average"
  threshold           = "75"
  alarm_description   = "This metric monitors Redis CPU utilization"

  dimensions = {
    ReplicationGroupId = aws_elasticache_replication_group.redis.id
  }

  tags = {
    Name = "${var.project_name}-redis-cpu-alarm-${var.environment}"
  }
}

resource "aws_cloudwatch_metric_alarm" "redis_memory" {
  alarm_name          = "${var.project_name}-redis-high-memory-${var.environment}"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "DatabaseMemoryUsagePercentage"
  namespace           = "AWS/ElastiCache"
  period              = "300"
  statistic           = "Average"
  threshold           = "80"
  alarm_description   = "This metric monitors Redis memory usage"

  dimensions = {
    ReplicationGroupId = aws_elasticache_replication_group.redis.id
  }

  tags = {
    Name = "${var.project_name}-redis-memory-alarm-${var.environment}"
  }
}

resource "aws_cloudwatch_metric_alarm" "redis_evictions" {
  alarm_name          = "${var.project_name}-redis-evictions-${var.environment}"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "Evictions"
  namespace           = "AWS/ElastiCache"
  period              = "300"
  statistic           = "Sum"
  threshold           = "1000"
  alarm_description   = "This metric monitors Redis evictions"

  dimensions = {
    ReplicationGroupId = aws_elasticache_replication_group.redis.id
  }

  tags = {
    Name = "${var.project_name}-redis-evictions-alarm-${var.environment}"
  }
}

resource "aws_cloudwatch_metric_alarm" "redis_replication_lag" {
  alarm_name          = "${var.project_name}-redis-replication-lag-${var.environment}"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "ReplicationLag"
  namespace           = "AWS/ElastiCache"
  period              = "60"
  statistic           = "Average"
  threshold           = "30"
  alarm_description   = "This metric monitors Redis replication lag"

  dimensions = {
    ReplicationGroupId = aws_elasticache_replication_group.redis.id
  }

  tags = {
    Name = "${var.project_name}-redis-lag-alarm-${var.environment}"
  }
}
