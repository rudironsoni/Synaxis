# Synaxis EU-West-1 Variables
# Production-ready configuration for Frankfurt region

variable "aws_region" {
  description = "AWS region for EU deployment (eu-west-1 Frankfurt)"
  type        = string
  default     = "eu-west-1"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "prod"
  
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be dev, staging, or prod."
  }
}

# VPC Configuration
variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
  default     = "10.1.0.0/16"
}

# RDS PostgreSQL Configuration
variable "db_name" {
  description = "PostgreSQL database name"
  type        = string
  default     = "synaxis"
}

variable "db_username" {
  description = "PostgreSQL master username"
  type        = string
  default     = "synaxis_admin"
  sensitive   = true
}

variable "db_password" {
  description = "PostgreSQL master password"
  type        = string
  sensitive   = true
  
  validation {
    condition     = length(var.db_password) >= 16
    error_message = "Database password must be at least 16 characters for security compliance."
  }
}

variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.r6g.xlarge"
}

variable "db_allocated_storage" {
  description = "Allocated storage in GB"
  type        = number
  default     = 100
}

variable "backup_retention_period" {
  description = "Days to retain backups (GDPR compliance: 30 days recommended)"
  type        = number
  default     = 30
}

# Redis ElastiCache Configuration
variable "redis_node_type" {
  description = "ElastiCache node type"
  type        = string
  default     = "cache.r6g.large"
}

variable "redis_num_cache_nodes" {
  description = "Number of cache nodes"
  type        = number
  default     = 3
}

# EKS Configuration
variable "eks_cluster_version" {
  description = "Kubernetes version"
  type        = string
  default     = "1.28"
}

variable "eks_node_instance_types" {
  description = "EKS node instance types"
  type        = list(string)
  default     = ["t3.xlarge", "t3a.xlarge"]
}

variable "eks_node_desired_size" {
  description = "Desired number of EKS nodes"
  type        = number
  default     = 3
}

variable "eks_node_min_size" {
  description = "Minimum number of EKS nodes"
  type        = number
  default     = 3
}

variable "eks_node_max_size" {
  description = "Maximum number of EKS nodes"
  type        = number
  default     = 10
}

# ALB Configuration
variable "alb_enable_deletion_protection" {
  description = "Enable deletion protection for ALB (recommended for production)"
  type        = bool
  default     = true
}

# Logging Configuration
variable "log_retention_days" {
  description = "CloudWatch log retention in days (GDPR compliance)"
  type        = number
  default     = 90
  
  validation {
    condition     = contains([7, 14, 30, 60, 90, 120, 150, 180, 365, 400, 545, 731, 1827, 3653], var.log_retention_days)
    error_message = "Log retention must be a valid CloudWatch retention period."
  }
}

# Tagging
variable "additional_tags" {
  description = "Additional tags to apply to all resources"
  type        = map(string)
  default     = {}
}

# Qdrant Vector DB Configuration
variable "qdrant_instance_type" {
  description = "Instance type for Qdrant vector database"
  type        = string
  default     = "t3.xlarge"
}

variable "qdrant_volume_size" {
  description = "EBS volume size for Qdrant in GB"
  type        = number
  default     = 100
}

# Network ACL Configuration
variable "enable_vpc_flow_logs" {
  description = "Enable VPC flow logs for security monitoring"
  type        = bool
  default     = true
}

# TLS Configuration
variable "minimum_tls_version" {
  description = "Minimum TLS version (TLS 1.3 for GDPR compliance)"
  type        = string
  default     = "TLSv1.3"
}
