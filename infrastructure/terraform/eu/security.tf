# Security Groups Module for Synaxis EU Region
# Centralized security group management with least privilege access
# GDPR-compliant network security policies

# Application Load Balancer Security Group
resource "aws_security_group" "alb" {
  name_prefix = "synaxis-${var.environment}-alb-"
  description = "Security group for Synaxis Application Load Balancer"
  vpc_id      = var.vpc_id

  # Allow HTTPS from internet (TLS 1.3 enforced at ALB level)
  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
    description = "HTTPS from internet"
  }

  # Allow HTTP (redirect to HTTPS)
  ingress {
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
    description = "HTTP from internet (redirect to HTTPS)"
  }

  # Allow outbound to EKS nodes
  egress {
    from_port   = 0
    to_port     = 65535
    protocol    = "tcp"
    cidr_blocks = [var.vpc_cidr]
    description = "Allow traffic to VPC"
  }

  tags = {
    
    {
      Name = "synaxis-${var.environment}-alb-sg"
    }
  )

  lifecycle {
    create_before_destroy = true
  }
}

# Application Security Group (EKS Pods)
resource "aws_security_group" "app" {
  name_prefix = "synaxis-${var.environment}-app-"
  description = "Security group for Synaxis application pods"
  vpc_id      = var.vpc_id

  # Allow inbound from ALB
  ingress {
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [aws_security_group.alb.id]
    description     = "Allow traffic from ALB"
  }

  # Allow pod-to-pod communication
  ingress {
    from_port   = 0
    to_port     = 65535
    protocol    = "tcp"
    self        = true
    description = "Allow pod to pod communication"
  }

  # Allow all outbound traffic
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
    description = "Allow all outbound traffic"
  }

  tags = {
    
    {
      Name = "synaxis-${var.environment}-app-sg"
    }
  )

  lifecycle {
    create_before_destroy = true
  }
}

# Qdrant Vector DB Security Group
resource "aws_security_group" "qdrant" {
  name_prefix = "synaxis-${var.environment}-qdrant-"
  description = "Security group for Qdrant vector database"
  vpc_id      = var.vpc_id

  # Allow Qdrant HTTP API from application security group
  ingress {
    from_port       = 6333
    to_port         = 6333
    protocol        = "tcp"
    security_groups = [aws_security_group.app.id]
    description     = "Qdrant HTTP API from application"
  }

  # Allow Qdrant gRPC from application security group
  ingress {
    from_port       = 6334
    to_port         = 6334
    protocol        = "tcp"
    security_groups = [aws_security_group.app.id]
    description     = "Qdrant gRPC from application"
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
    description = "Allow all outbound traffic"
  }

  tags = {
    
    {
      Name = "synaxis-${var.environment}-qdrant-sg"
    }
  )

  lifecycle {
    create_before_destroy = true
  }
}

# VPC Endpoint Security Group (for AWS services)
resource "aws_security_group" "vpc_endpoints" {
  name_prefix = "synaxis-${var.environment}-vpce-"
  description = "Security group for VPC endpoints"
  vpc_id      = var.vpc_id

  # Allow HTTPS from VPC
  ingress {
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = [var.vpc_cidr]
    description = "HTTPS from VPC"
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
    description = "Allow all outbound traffic"
  }

  tags = {
    
    {
      Name = "synaxis-${var.environment}-vpce-sg"
    }
  )

  lifecycle {
    create_before_destroy = true
  }
}

# Network ACL for additional security layer (optional, defense in depth)
resource "aws_network_acl" "private" {
  vpc_id     = var.vpc_id
  subnet_ids = var.private_subnet_ids

  # Allow inbound ephemeral ports
  ingress {
    protocol   = "tcp"
    rule_no    = 100
    action     = "allow"
    cidr_block = "0.0.0.0/0"
    from_port  = 1024
    to_port    = 65535
  }

  # Allow inbound from VPC CIDR
  ingress {
    protocol   = "-1"
    rule_no    = 200
    action     = "allow"
    cidr_block = var.vpc_cidr
    from_port  = 0
    to_port    = 0
  }

  # Allow all outbound traffic
  egress {
    protocol   = "-1"
    rule_no    = 100
    action     = "allow"
    cidr_block = "0.0.0.0/0"
    from_port  = 0
    to_port    = 0
  }

  tags = {
    
    {
      Name = "synaxis-${var.environment}-private-nacl"
    }
  )
}

# VPC Endpoints for AWS Services (PrivateLink)
# S3 Gateway Endpoint (no cost)
resource "aws_vpc_endpoint" "s3" {
  vpc_id       = var.vpc_id
  service_name = "com.amazonaws.${var.aws_region}.s3"
  vpc_endpoint_type = "Gateway"
  route_table_ids = var.private_route_table_ids

  tags = {
    
    {
      Name = "synaxis-${var.environment}-s3-endpoint"
    }
  )
}

# ECR API Endpoint (for pulling Docker images)
resource "aws_vpc_endpoint" "ecr_api" {
  vpc_id              = var.vpc_id
  service_name        = "com.amazonaws.${var.aws_region}.ecr.api"
  vpc_endpoint_type   = "Interface"
  subnet_ids          = var.private_subnet_ids
  security_group_ids  = [aws_security_group.vpc_endpoints.id]
  private_dns_enabled = true

  tags = {
    
    {
      Name = "synaxis-${var.environment}-ecr-api-endpoint"
    }
  )
}

# ECR Docker Endpoint (for pulling Docker images)
resource "aws_vpc_endpoint" "ecr_dkr" {
  vpc_id              = var.vpc_id
  service_name        = "com.amazonaws.${var.aws_region}.ecr.dkr"
  vpc_endpoint_type   = "Interface"
  subnet_ids          = var.private_subnet_ids
  security_group_ids  = [aws_security_group.vpc_endpoints.id]
  private_dns_enabled = true

  tags = {
    
    {
      Name = "synaxis-${var.environment}-ecr-dkr-endpoint"
    }
  )
}

# CloudWatch Logs Endpoint
resource "aws_vpc_endpoint" "logs" {
  vpc_id              = var.vpc_id
  service_name        = "com.amazonaws.${var.aws_region}.logs"
  vpc_endpoint_type   = "Interface"
  subnet_ids          = var.private_subnet_ids
  security_group_ids  = [aws_security_group.vpc_endpoints.id]
  private_dns_enabled = true

  tags = {
    
    {
      Name = "synaxis-${var.environment}-logs-endpoint"
    }
  )
}

# EKS Endpoint
resource "aws_vpc_endpoint" "eks" {
  vpc_id              = var.vpc_id
  service_name        = "com.amazonaws.${var.aws_region}.eks"
  vpc_endpoint_type   = "Interface"
  subnet_ids          = var.private_subnet_ids
  security_group_ids  = [aws_security_group.vpc_endpoints.id]
  private_dns_enabled = true

  tags = {
    
    {
      Name = "synaxis-${var.environment}-eks-endpoint"
    }
  )
}

# STS Endpoint (for IAM role assumption)
resource "aws_vpc_endpoint" "sts" {
  vpc_id              = var.vpc_id
  service_name        = "com.amazonaws.${var.aws_region}.sts"
  vpc_endpoint_type   = "Interface"
  subnet_ids          = var.private_subnet_ids
  security_group_ids  = [aws_security_group.vpc_endpoints.id]
  private_dns_enabled = true

  tags = {
    
    {
      Name = "synaxis-${var.environment}-sts-endpoint"
    }
  )
}

# WAF Web ACL for ALB (optional, for DDoS protection)
resource "aws_wafv2_web_acl" "main" {
  count = var.enable_waf ? 1 : 0

  name  = "synaxis-${var.environment}-waf"
  scope = "REGIONAL"

  default_action {
    allow {}
  }

  # AWS Managed Rules
  rule {
    name     = "AWSManagedRulesCommonRuleSet"
    priority = 1

    override_action {
      none {}
    }

    statement {
      managed_rule_group_statement {
        name        = "AWSManagedRulesCommonRuleSet"
        vendor_name = "AWS"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "AWSManagedRulesCommonRuleSetMetric"
      sampled_requests_enabled   = true
    }
  }

  # Rate limiting (1000 requests per 5 minutes per IP)
  rule {
    name     = "RateLimitRule"
    priority = 2

    action {
      block {}
    }

    statement {
      rate_based_statement {
        limit              = 1000
        aggregate_key_type = "IP"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "RateLimitRuleMetric"
      sampled_requests_enabled   = true
    }
  }

  visibility_config {
    cloudwatch_metrics_enabled = true
    metric_name                = "synaxis-${var.environment}-waf"
    sampled_requests_enabled   = true
  }

  tags = {
    
    {
      Name       = "synaxis-${var.environment}-waf"
      Compliance = "GDPR"
    }
  )
}
