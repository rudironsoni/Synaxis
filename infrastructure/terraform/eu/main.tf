# Synaxis EU-West-1 (Frankfurt) Region Infrastructure
# GDPR-compliant, encrypted multi-AZ deployment
# All data stays in eu-west-1 region

terraform {
  required_version = ">= 1.6.0"
  
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "~> 2.23"
    }
  }

  # Backend configuration for state management
  # Uncomment and configure for production use
  # backend "s3" {
  #   bucket         = "synaxis-terraform-state-eu"
  #   key            = "eu-west-1/terraform.tfstate"
  #   region         = "eu-west-1"
  #   encrypt        = true
  #   kms_key_id     = "alias/terraform-state-key"
  #   dynamodb_table = "synaxis-terraform-locks"
  # }
}

# Provider configuration - EU West 1 (Frankfurt)
provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "Synaxis"
      Environment = var.environment
      Region      = "eu-west-1"
      ManagedBy   = "Terraform"
      Compliance  = "GDPR"
      DataResidency = "EU"
    }
  }
}

# Kubernetes provider configuration
# Configure after EKS cluster is created
provider "kubernetes" {
  host                   = aws_eks_cluster.main.endpoint
  cluster_ca_certificate = base64decode(aws_eks_cluster.main.certificate_authority[0].data)
  
  exec {
    api_version = "client.authentication.k8s.io/v1beta1"
    command     = "aws"
    args = [
      "eks",
      "get-token",
      "--cluster-name",
      aws_eks_cluster.main.name,
      "--region",
      var.aws_region
    ]
  }
}

# Data source for available AZs in eu-west-1
data "aws_availability_zones" "available" {
  state = "available"
  
  filter {
    name   = "opt-in-status"
    values = ["opt-in-not-required"]
  }
}

# KMS key for encryption at rest
resource "aws_kms_key" "synaxis_eu" {
  description             = "Synaxis EU encryption key for RDS, ElastiCache, and EBS"
  deletion_window_in_days = 30
  enable_key_rotation     = true

  tags = {
    Name = "synaxis-eu-encryption-key"
  }
}

resource "aws_kms_alias" "synaxis_eu" {
  name          = "alias/synaxis-eu"
  target_key_id = aws_kms_key.synaxis_eu.key_id
}

# CloudWatch Log Group for application logs (GDPR-compliant retention)
resource "aws_cloudwatch_log_group" "synaxis_eu" {
  name              = "/aws/synaxis/eu-west-1"
  retention_in_days = var.log_retention_days
  kms_key_id        = aws_kms_key.synaxis_eu.arn

  tags = {
    Name = "synaxis-eu-logs"
    Compliance = "GDPR"
  }
}

# Note: VPC, RDS, ElastiCache, EKS, and Security resources are defined in their respective files:
# - vpc.tf: VPC and networking resources
# - rds.tf: PostgreSQL database
# - elasticache.tf: Redis cluster
# - eks.tf: EKS cluster and node groups
# - security.tf: Security groups and network policies
