# Synaxis SA-East-1 (São Paulo) Region Infrastructure
# LGPD-compliant, encrypted multi-AZ deployment
# All data stays in sa-east-1 region for Brazil data residency

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
    helm = {
      source  = "hashicorp/helm"
      version = "~> 2.11"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.5"
    }
  }

  backend "s3" {
    bucket         = "synaxis-terraform-state"
    key            = "sa-east-1/terraform.tfstate"
    region         = "sa-east-1"
    encrypt        = true
    dynamodb_table = "synaxis-terraform-locks"
  }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Environment   = var.environment
      Region        = "sa-east-1"
      ManagedBy     = "terraform"
      Project       = "synaxis"
      Service       = "inference-gateway"
      Compliance    = "LGPD"
      DataResidency = "Brazil"
      ANPD          = "notification-enabled"
    }
  }
}

provider "kubernetes" {
  host                   = aws_eks_cluster.brazil.endpoint
  cluster_ca_certificate = base64decode(aws_eks_cluster.brazil.certificate_authority[0].data)

  exec {
    api_version = "client.authentication.k8s.io/v1beta1"
    command     = "aws"
    args = [
      "eks",
      "get-token",
      "--cluster-name",
      aws_eks_cluster.brazil.name,
      "--region",
      var.aws_region
    ]
  }
}

provider "helm" {
  kubernetes {
    host                   = aws_eks_cluster.brazil.endpoint
    cluster_ca_certificate = base64decode(aws_eks_cluster.brazil.certificate_authority[0].data)

    exec {
      api_version = "client.authentication.k8s.io/v1beta1"
      command     = "aws"
      args = [
        "eks",
        "get-token",
        "--cluster-name",
        aws_eks_cluster.brazil.name,
        "--region",
        var.aws_region
      ]
    }
  }
}

# Random suffix for unique resource naming
resource "random_id" "suffix" {
  byte_length = 4
}

# Data sources
data "aws_availability_zones" "available" {
  state = "available"
  filter {
    name   = "opt-in-status"
    values = ["opt-in-not-required"]
  }
}

data "aws_caller_identity" "current" {}

# KMS key for encryption at rest (LGPD requirement)
resource "aws_kms_key" "synaxis_brazil" {
  description             = "Synaxis Brazil encryption key for RDS, ElastiCache, EBS, and secrets"
  deletion_window_in_days = 30
  enable_key_rotation     = true

  tags = {
    Name       = "synaxis-brazil-${var.environment}"
    Compliance = "LGPD"
  }
}

resource "aws_kms_alias" "synaxis_brazil" {
  name          = "alias/synaxis-brazil-${var.environment}"
  target_key_id = aws_kms_key.synaxis_brazil.key_id
}

# CloudWatch Log Group for application logs (LGPD-compliant retention)
resource "aws_cloudwatch_log_group" "synaxis_brazil" {
  name              = "/aws/synaxis/sa-east-1"
  retention_in_days = var.log_retention_days
  kms_key_id        = aws_kms_key.synaxis_brazil.arn

  tags = {
    Name       = "synaxis-brazil-logs"
    Compliance = "LGPD"
  }
}

# Secrets Manager for database credentials
resource "random_password" "db_password" {
  length  = 32
  special = true
}

resource "aws_secretsmanager_secret" "db_password" {
  name                    = "synaxis-brazil-db-password-${random_id.suffix.hex}"
  kms_key_id              = aws_kms_key.synaxis_brazil.arn
  recovery_window_in_days = 7

  tags = {
    Name = "synaxis-brazil-db-password"
  }
}

resource "aws_secretsmanager_secret_version" "db_password" {
  secret_id     = aws_secretsmanager_secret.db_password.id
  secret_string = random_password.db_password.result
}
