# Security Groups and Network ACLs for Brazil Region

# Security Group for Application Load Balancer
resource "aws_security_group" "alb" {
  name        = "synaxis-brazil-alb-sg-${var.environment}"
  description = "Security group for Application Load Balancer"
  vpc_id      = aws_vpc.brazil.id

  ingress {
    description = "HTTPS from internet"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = var.allowed_cidr_blocks
  }

  ingress {
    description = "HTTP from internet (redirect to HTTPS)"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = var.allowed_cidr_blocks
  }

  egress {
    description = "Allow all outbound traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "synaxis-brazil-alb-sg-${var.environment}"
  }
}

# Security Group for Qdrant Vector Database
resource "aws_security_group" "qdrant" {
  name        = "synaxis-brazil-qdrant-sg-${var.environment}"
  description = "Security group for Qdrant vector database"
  vpc_id      = aws_vpc.brazil.id

  ingress {
    description     = "Qdrant API from EKS"
    from_port       = 6333
    to_port         = 6333
    protocol        = "tcp"
    security_groups = [aws_eks_cluster.brazil.vpc_config[0].cluster_security_group_id]
  }

  ingress {
    description     = "Qdrant gRPC from EKS"
    from_port       = 6334
    to_port         = 6334
    protocol        = "tcp"
    security_groups = [aws_eks_cluster.brazil.vpc_config[0].cluster_security_group_id]
  }

  ingress {
    description = "SSH for management (restricted)"
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = [var.vpc_cidr]
  }

  egress {
    description = "Allow all outbound traffic"
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "synaxis-brazil-qdrant-sg-${var.environment}"
  }
}

# Network ACL for Database Subnets
resource "aws_network_acl" "database" {
  vpc_id     = aws_vpc.brazil.id
  subnet_ids = aws_subnet.database[*].id

  # Inbound rules
  ingress {
    protocol   = "tcp"
    rule_no    = 100
    action     = "allow"
    cidr_block = var.vpc_cidr
    from_port  = 5432
    to_port    = 5432
  }

  ingress {
    protocol   = "tcp"
    rule_no    = 110
    action     = "allow"
    cidr_block = var.vpc_cidr
    from_port  = 6379
    to_port    = 6379
  }

  ingress {
    protocol   = "tcp"
    rule_no    = 120
    action     = "allow"
    cidr_block = var.vpc_cidr
    from_port  = 1024
    to_port    = 65535
  }

  # Outbound rules
  egress {
    protocol   = "tcp"
    rule_no    = 100
    action     = "allow"
    cidr_block = var.vpc_cidr
    from_port  = 1024
    to_port    = 65535
  }

  tags = {
    Name = "synaxis-brazil-database-nacl-${var.environment}"
  }
}

# KMS Key Policy for encryption
resource "aws_kms_key_policy" "synaxis_brazil" {
  key_id = aws_kms_key.synaxis_brazil.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "Enable IAM User Permissions"
        Effect = "Allow"
        Principal = {
          AWS = "arn:aws:iam::${data.aws_caller_identity.current.account_id}:root"
        }
        Action   = "kms:*"
        Resource = "*"
      },
      {
        Sid    = "Allow CloudWatch Logs"
        Effect = "Allow"
        Principal = {
          Service = "logs.amazonaws.com"
        }
        Action = [
          "kms:Encrypt",
          "kms:Decrypt",
          "kms:ReEncrypt*",
          "kms:GenerateDataKey*",
          "kms:CreateGrant",
          "kms:DescribeKey"
        ]
        Resource = "*"
      },
      {
        Sid    = "Allow RDS"
        Effect = "Allow"
        Principal = {
          Service = "rds.amazonaws.com"
        }
        Action = [
          "kms:Encrypt",
          "kms:Decrypt",
          "kms:ReEncrypt*",
          "kms:GenerateDataKey*",
          "kms:CreateGrant",
          "kms:DescribeKey"
        ]
        Resource = "*"
      },
      {
        Sid    = "Allow ElastiCache"
        Effect = "Allow"
        Principal = {
          Service = "elasticache.amazonaws.com"
        }
        Action = [
          "kms:Encrypt",
          "kms:Decrypt",
          "kms:ReEncrypt*",
          "kms:GenerateDataKey*",
          "kms:CreateGrant",
          "kms:DescribeKey"
        ]
        Resource = "*"
      },
      {
        Sid    = "Allow Secrets Manager"
        Effect = "Allow"
        Principal = {
          Service = "secretsmanager.amazonaws.com"
        }
        Action = [
          "kms:Encrypt",
          "kms:Decrypt",
          "kms:ReEncrypt*",
          "kms:GenerateDataKey*",
          "kms:CreateGrant",
          "kms:DescribeKey"
        ]
        Resource = "*"
      },
      {
        Sid    = "Allow EKS"
        Effect = "Allow"
        Principal = {
          Service = "eks.amazonaws.com"
        }
        Action = [
          "kms:Encrypt",
          "kms:Decrypt",
          "kms:ReEncrypt*",
          "kms:GenerateDataKey*",
          "kms:CreateGrant",
          "kms:DescribeKey"
        ]
        Resource = "*"
      }
    ]
  })
}

# Security Group Rules for VPC Peering
resource "aws_security_group_rule" "allow_us_vpc" {
  count             = var.us_vpc_id != "" ? 1 : 0
  type              = "ingress"
  from_port         = 0
  to_port           = 65535
  protocol          = "tcp"
  cidr_blocks       = [var.us_vpc_cidr]
  security_group_id = aws_eks_cluster.brazil.vpc_config[0].cluster_security_group_id
  description       = "Allow traffic from US VPC"
}

resource "aws_security_group_rule" "allow_eu_vpc" {
  count             = var.eu_vpc_id != "" ? 1 : 0
  type              = "ingress"
  from_port         = 0
  to_port           = 65535
  protocol          = "tcp"
  cidr_blocks       = [var.eu_vpc_cidr]
  security_group_id = aws_eks_cluster.brazil.vpc_config[0].cluster_security_group_id
  description       = "Allow traffic from EU VPC"
}

# IAM Password Policy (LGPD compliance)
resource "aws_iam_account_password_policy" "brazil" {
  minimum_password_length        = 14
  require_lowercase_characters   = true
  require_numbers               = true
  require_uppercase_characters   = true
  require_symbols               = true
  allow_users_to_change_password = true
  max_password_age              = 90
  password_reuse_prevention      = 24
}
