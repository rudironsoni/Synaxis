variable "aws_region" {
  description = "AWS region for Brazil deployment"
  type        = string
  default     = "sa-east-1"
}

variable "environment" {
  description = "Environment name (e.g., production, staging)"
  type        = string
  default     = "production"
}

variable "project_name" {
  description = "Project name"
  type        = string
  default     = "synaxis"
}

variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
  default     = "10.2.0.0/16"
}

variable "availability_zones_count" {
  description = "Number of availability zones to use"
  type        = number
  default     = 3
}

variable "private_subnet_cidrs" {
  description = "CIDR blocks for private subnets"
  type        = list(string)
  default     = ["10.2.1.0/24", "10.2.2.0/24", "10.2.3.0/24"]
}

variable "public_subnet_cidrs" {
  description = "CIDR blocks for public subnets"
  type        = list(string)
  default     = ["10.2.101.0/24", "10.2.102.0/24", "10.2.103.0/24"]
}

variable "database_subnet_cidrs" {
  description = "CIDR blocks for database subnets"
  type        = list(string)
  default     = ["10.2.201.0/24", "10.2.202.0/24", "10.2.203.0/24"]
}

# RDS Configuration
variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.r6g.xlarge"
}

variable "db_allocated_storage" {
  description = "Allocated storage for RDS in GB"
  type        = number
  default     = 100
}

variable "db_max_allocated_storage" {
  description = "Maximum allocated storage for RDS autoscaling in GB"
  type        = number
  default     = 500
}

variable "db_name" {
  description = "Database name"
  type        = string
  default     = "synaxis"
}

variable "db_username" {
  description = "Database master username"
  type        = string
  default     = "synaxis_admin"
}

variable "db_backup_retention_period" {
  description = "Number of days to retain database backups"
  type        = number
  default     = 30
}

variable "db_engine_version" {
  description = "PostgreSQL engine version"
  type        = string
  default     = "16.1"
}

# Redis Configuration
variable "redis_node_type" {
  description = "ElastiCache Redis node type"
  type        = string
  default     = "cache.r7g.large"
}

variable "redis_num_cache_nodes" {
  description = "Number of cache nodes in the cluster"
  type        = number
  default     = 3
}

variable "redis_parameter_group_family" {
  description = "Redis parameter group family"
  type        = string
  default     = "redis7"
}

variable "redis_engine_version" {
  description = "Redis engine version"
  type        = string
  default     = "7.1"
}

# EKS Configuration
variable "eks_cluster_version" {
  description = "Kubernetes version for EKS cluster"
  type        = string
  default     = "1.28"
}

variable "eks_node_instance_types" {
  description = "Instance types for EKS node groups"
  type        = list(string)
  default     = ["m6i.xlarge", "m6i.2xlarge"]
}

variable "eks_node_desired_size" {
  description = "Desired number of nodes in EKS node group"
  type        = number
  default     = 6
}

variable "eks_node_min_size" {
  description = "Minimum number of nodes in EKS node group"
  type        = number
  default     = 3
}

variable "eks_node_max_size" {
  description = "Maximum number of nodes in EKS node group"
  type        = number
  default     = 15
}

variable "eks_node_disk_size" {
  description = "Disk size for EKS nodes in GB"
  type        = number
  default     = 100
}

# Qdrant Configuration
variable "qdrant_instance_type" {
  description = "Instance type for Qdrant vector database"
  type        = string
  default     = "m6i.2xlarge"
}

variable "qdrant_volume_size" {
  description = "EBS volume size for Qdrant in GB"
  type        = number
  default     = 500
}

variable "qdrant_volume_type" {
  description = "EBS volume type for Qdrant"
  type        = string
  default     = "gp3"
}

# VPC Peering Configuration
variable "us_vpc_id" {
  description = "VPC ID for US region peering"
  type        = string
  default     = ""
}

variable "us_vpc_cidr" {
  description = "CIDR block for US VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "us_region" {
  description = "US region for peering"
  type        = string
  default     = "us-east-1"
}

variable "eu_vpc_id" {
  description = "VPC ID for EU region peering"
  type        = string
  default     = ""
}

variable "eu_vpc_cidr" {
  description = "CIDR block for EU VPC"
  type        = string
  default     = "10.1.0.0/16"
}

variable "eu_region" {
  description = "EU region for peering"
  type        = string
  default     = "eu-west-1"
}

# Application Configuration
variable "api_replicas_min" {
  description = "Minimum number of API replicas"
  type        = number
  default     = 3
}

variable "api_replicas_max" {
  description = "Maximum number of API replicas"
  type        = number
  default     = 20
}

variable "enable_deletion_protection" {
  description = "Enable deletion protection for critical resources"
  type        = bool
  default     = true
}

variable "allowed_cidr_blocks" {
  description = "CIDR blocks allowed to access the application"
  type        = list(string)
  default     = ["0.0.0.0/0"]
}

variable "log_retention_days" {
  description = "CloudWatch logs retention in days"
  type        = number
  default     = 90
}

variable "tags" {
  description = "Additional tags for resources"
  type        = map(string)
  default     = {}
}
