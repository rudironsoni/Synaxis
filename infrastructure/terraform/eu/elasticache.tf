# ElastiCache Redis 7 Cluster for Synaxis EU Region
# Multi-AZ replication group with encryption at rest and in transit
# GDPR-compliant with automatic failover

# Security Group for ElastiCache
resource "aws_security_group" "elasticache" {
  name_prefix = "synaxis-${var.environment}-redis-"
  description = "Security group for Synaxis ElastiCache Redis"
  vpc_id      = aws_vpc.main.id

  # Allow Redis from VPC CIDR only (least privilege)
  ingress {
    from_port   = 6379
    to_port     = 6379
    protocol    = "tcp"
    cidr_blocks = [var.vpc_cidr]
    description = "Redis access from VPC"
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
    description = "Allow all outbound traffic"
  }

  tags = {
    Name = "synaxis-${var.environment}-redis-sg"
    Environment = var.environment
  }

  lifecycle {
    create_before_destroy = true
  }
}

# ElastiCache Replication Group (Redis 7 Cluster Mode Disabled)
resource "aws_elasticache_replication_group" "main" {
  replication_group_id       = "synaxis-${var.environment}-redis"
  replication_group_description = "Synaxis Redis cluster for session and cache management"
  
  # Engine configuration
  engine               = "redis"
  engine_version       = "7.1"
  node_type            = var.redis_node_type
  num_cache_clusters   = var.redis_num_cache_nodes
  parameter_group_name = aws_elasticache_parameter_group.main.name
  port                 = 6379

  # Network configuration
  subnet_group_name    = aws_elasticache_subnet_group.main.name
  security_group_ids   = [aws_security_group.elasticache.id]

  # Multi-AZ with automatic failover
  automatic_failover_enabled = true
  multi_az_enabled          = true

  # Encryption (GDPR compliance)
  at_rest_encryption_enabled = true
  kms_key_id                = aws_kms_key.synaxis_eu.arn
  transit_encryption_enabled = true
  transit_encryption_mode    = "required"
  auth_token_enabled        = false # Set to true and provide auth_token for production

  # Backup configuration
  snapshot_retention_limit   = 7
  snapshot_window           = "03:00-04:00"
  maintenance_window        = "sun:04:00-sun:05:00"
  final_snapshot_identifier = "synaxis-${var.environment}-redis-final-snapshot-${formatdate("YYYY-MM-DD-hhmm", timestamp())}"

  # Auto upgrade
  auto_minor_version_upgrade = true
  apply_immediately         = false

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
    Name       = "synaxis-${var.environment}-redis"
    Engine     = "redis-7"
    Compliance = "GDPR"
    Encryption = "AES-256"
    Environment = var.environment
  }

  lifecycle {
    prevent_destroy = false # Set to true for production
    ignore_changes  = [final_snapshot_identifier]
  }
}

# Parameter Group for Redis 7
resource "aws_elasticache_parameter_group" "main" {
  name_prefix = "synaxis-${var.environment}-redis7-"
  family      = "redis7"
  description = "Custom parameter group for Synaxis Redis 7"

  # Memory management
  parameter {
    name  = "maxmemory-policy"
    value = "allkeys-lru"
  }

  # Timeout for idle connections (5 minutes)
  parameter {
    name  = "timeout"
    value = "300"
  }

  # Enable active defragmentation
  parameter {
    name  = "activedefrag"
    value = "yes"
  }

  # Slow log threshold (10ms)
  parameter {
    name  = "slowlog-log-slower-than"
    value = "10000"
  }

  parameter {
    name  = "slowlog-max-len"
    value = "128"
  }

  tags = {
    
    {
      Name = "synaxis-${var.environment}-redis7-params"
    }
  )

  lifecycle {
    create_before_destroy = true
  }
}

# CloudWatch Log Groups for Redis logs
resource "aws_cloudwatch_log_group" "redis_slow_log" {
  name              = "/aws/elasticache/synaxis-${var.environment}/slow-log"
  retention_in_days = 30

  tags = {
    
    {
      Name       = "synaxis-${var.environment}-redis-slow-log"
      Compliance = "GDPR"
    }
  )
}

resource "aws_cloudwatch_log_group" "redis_engine_log" {
  name              = "/aws/elasticache/synaxis-${var.environment}/engine-log"
  retention_in_days = 30

  tags = {
    
    {
      Name       = "synaxis-${var.environment}-redis-engine-log"
      Compliance = "GDPR"
    }
  )
}

# CloudWatch Alarms for Redis monitoring
resource "aws_cloudwatch_metric_alarm" "redis_cpu" {
  alarm_name          = "synaxis-${var.environment}-redis-cpu-utilization"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "CPUUtilization"
  namespace           = "AWS/ElastiCache"
  period              = "300"
  statistic           = "Average"
  threshold           = "75"
  alarm_description   = "This metric monitors Redis CPU utilization"
  alarm_actions       = [] # Add SNS topic ARN for notifications

  dimensions = {
    ReplicationGroupId = aws_elasticache_replication_group.main.id
  }

  tags = { Name = "synaxis-${var.environment}", Environment = var.environment }
}

resource "aws_cloudwatch_metric_alarm" "redis_memory" {
  alarm_name          = "synaxis-${var.environment}-redis-memory-utilization"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "DatabaseMemoryUsagePercentage"
  namespace           = "AWS/ElastiCache"
  period              = "300"
  statistic           = "Average"
  threshold           = "80"
  alarm_description   = "This metric monitors Redis memory utilization"
  alarm_actions       = [] # Add SNS topic ARN for notifications

  dimensions = {
    ReplicationGroupId = aws_elasticache_replication_group.main.id
  }

  tags = { Name = "synaxis-${var.environment}", Environment = var.environment }
}

resource "aws_cloudwatch_metric_alarm" "redis_evictions" {
  alarm_name          = "synaxis-${var.environment}-redis-evictions"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "1"
  metric_name         = "Evictions"
  namespace           = "AWS/ElastiCache"
  period              = "300"
  statistic           = "Sum"
  threshold           = "1000"
  alarm_description   = "This metric monitors Redis evictions"
  alarm_actions       = [] # Add SNS topic ARN for notifications

  dimensions = {
    ReplicationGroupId = aws_elasticache_replication_group.main.id
  }

  tags = { Name = "synaxis-${var.environment}", Environment = var.environment }
}

resource "aws_cloudwatch_metric_alarm" "redis_connections" {
  alarm_name          = "synaxis-${var.environment}-redis-connection-count"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "CurrConnections"
  namespace           = "AWS/ElastiCache"
  period              = "300"
  statistic           = "Average"
  threshold           = "5000"
  alarm_description   = "This metric monitors Redis connection count"
  alarm_actions       = [] # Add SNS topic ARN for notifications

  dimensions = {
    ReplicationGroupId = aws_elasticache_replication_group.main.id
  }

  tags = { Name = "synaxis-${var.environment}", Environment = var.environment }
}
