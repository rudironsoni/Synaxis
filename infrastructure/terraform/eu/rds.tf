# RDS PostgreSQL 16 Module for Synaxis EU Region
# Multi-AZ deployment with encryption at rest (AES-256)
# GDPR-compliant backup and retention policies

# Security Group for RDS
resource "aws_security_group" "rds" {
  name_prefix = "synaxis-${var.environment}-rds-"
  description = "Security group for Synaxis RDS PostgreSQL"
  vpc_id      = aws_vpc.main.id

  # Allow PostgreSQL from VPC CIDR only (least privilege)
  ingress {
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = [var.vpc_cidr]
    description = "PostgreSQL access from VPC"
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
    description = "Allow all outbound traffic"
  }

  tags = {
    Name = "synaxis-${var.environment}-rds-sg"
    Environment = var.environment
  }

  lifecycle {
    create_before_destroy = true
  }
}

# RDS PostgreSQL 16 Instance (Multi-AZ)
resource "aws_db_instance" "main" {
  identifier     = "synaxis-${var.environment}-postgres"
  engine         = "postgres"
  engine_version = "16.1"

  # Instance configuration
  instance_class    = var.db_instance_class
  allocated_storage = var.db_allocated_storage
  storage_type      = "gp3"
  storage_encrypted = true
  kms_key_id        = aws_kms_key.synaxis_eu.arn

  # Database configuration
  db_name  = var.db_name
  username = var.db_username
  password = var.db_password
  port     = 5432

  # Multi-AZ for high availability
  multi_az               = true
  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.rds.id]
  publicly_accessible    = false

  # Backup configuration (GDPR compliance)
  backup_retention_period   = var.backup_retention_period
  backup_window             = "03:00-04:00"
  maintenance_window        = "sun:04:00-sun:05:00"
  delete_automated_backups  = false
  copy_tags_to_snapshot     = true
  skip_final_snapshot       = false
  final_snapshot_identifier = "synaxis-${var.environment}-final-snapshot-${formatdate("YYYY-MM-DD-hhmm", timestamp())}"

  # Enhanced monitoring
  enabled_cloudwatch_logs_exports = ["postgresql", "upgrade"]
  monitoring_interval             = 60
  monitoring_role_arn             = aws_iam_role.rds_monitoring.arn

  # Performance Insights (encrypted)
  performance_insights_enabled    = true
  performance_insights_kms_key_id = aws_kms_key.synaxis_eu.arn
  performance_insights_retention_period = 7

  # Parameter group for TLS enforcement
  parameter_group_name = aws_db_parameter_group.main.name

  # Auto minor version upgrade during maintenance window
  auto_minor_version_upgrade = true

  # Deletion protection for production
  deletion_protection = var.environment == "prod" ? true : false

  tags = {
    Name       = "synaxis-${var.environment}-postgres"
    Engine     = "postgres-16"
    Compliance = "GDPR"
    Encryption = "AES-256"
    Environment = var.environment
  }

  lifecycle {
    prevent_destroy = false # Set to true for production
    ignore_changes  = [final_snapshot_identifier]
  }
}

# DB Parameter Group for TLS 1.3 enforcement
resource "aws_db_parameter_group" "main" {
  name_prefix = "synaxis-${var.environment}-postgres16-"
  family      = "postgres16"
  description = "Custom parameter group for Synaxis PostgreSQL 16 (TLS 1.3 enforced)"

  # Enforce SSL/TLS connections (GDPR compliance)
  parameter {
    name  = "rds.force_ssl"
    value = "1"
  }

  # Set minimum TLS version to 1.3
  parameter {
    name  = "ssl_min_protocol_version"
    value = "TLSv1.3"
  }

  # Logging for audit trails
  parameter {
    name  = "log_connections"
    value = "1"
  }

  parameter {
    name  = "log_disconnections"
    value = "1"
  }

  parameter {
    name  = "log_statement"
    value = "ddl"
  }

  # Performance tuning
  parameter {
    name  = "shared_preload_libraries"
    value = "pg_stat_statements"
  }

  tags = {
    Name = "synaxis-${var.environment}-postgres16-params"
    Environment = var.environment
  }

  lifecycle {
    create_before_destroy = true
  }
}

# IAM Role for Enhanced Monitoring
resource "aws_iam_role" "rds_monitoring" {
  name_prefix = "synaxis-${var.environment}-rds-monitoring-"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "monitoring.rds.amazonaws.com"
        }
      }
    ]
  })

  tags = {
    Name = "synaxis-${var.environment}-rds-monitoring-role"
    Environment = var.environment
  }
}

# Attach AWS managed policy for RDS Enhanced Monitoring
resource "aws_iam_role_policy_attachment" "rds_monitoring" {
  role       = aws_iam_role.rds_monitoring.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonRDSEnhancedMonitoringRole"
}

# Read Replica (optional, for scaling reads)
resource "aws_db_instance" "replica" {
  count = 0 # Set to 1 to enable read replica

  identifier     = "synaxis-${var.environment}-postgres-replica"
  replicate_source_db = aws_db_instance.main.identifier

  instance_class    = var.db_instance_class
  storage_encrypted = true
  kms_key_id        = aws_kms_key.synaxis_eu.arn

  publicly_accessible    = false
  vpc_security_group_ids = [aws_security_group.rds.id]

  # Enhanced monitoring
  monitoring_interval = 60
  monitoring_role_arn = aws_iam_role.rds_monitoring.arn

  # Performance Insights
  performance_insights_enabled    = true
  performance_insights_kms_key_id = aws_kms_key.synaxis_eu.arn
  performance_insights_retention_period = 7

  auto_minor_version_upgrade = true
  skip_final_snapshot        = true

  tags = {
    Name       = "synaxis-${var.environment}-postgres-replica"
    Role       = "read-replica"
    Compliance = "GDPR"
    Environment = var.environment
  }
}

# CloudWatch Alarms for RDS monitoring
resource "aws_cloudwatch_metric_alarm" "database_cpu" {
  alarm_name          = "synaxis-${var.environment}-rds-cpu-utilization"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "CPUUtilization"
  namespace           = "AWS/RDS"
  period              = "300"
  statistic           = "Average"
  threshold           = "80"
  alarm_description   = "This metric monitors RDS CPU utilization"
  alarm_actions       = [] # Add SNS topic ARN for notifications

  dimensions = {
    DBInstanceIdentifier = aws_db_instance.main.id
  }

  tags = {
    Name = "synaxis-${var.environment}-rds-cpu-alarm"
    Environment = var.environment
  }
}

resource "aws_cloudwatch_metric_alarm" "database_storage" {
  alarm_name          = "synaxis-${var.environment}-rds-storage-space"
  comparison_operator = "LessThanThreshold"
  evaluation_periods  = "1"
  metric_name         = "FreeStorageSpace"
  namespace           = "AWS/RDS"
  period              = "300"
  statistic           = "Average"
  threshold           = "10737418240" # 10 GB in bytes
  alarm_description   = "This metric monitors RDS free storage space"
  alarm_actions       = [] # Add SNS topic ARN for notifications

  dimensions = {
    DBInstanceIdentifier = aws_db_instance.main.id
  }

  tags = {
    Name = "synaxis-${var.environment}-rds-storage-alarm"
    Environment = var.environment
  }
}
