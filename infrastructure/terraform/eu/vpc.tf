# VPC Module for Synaxis EU Region
# Multi-AZ VPC with public, private, and database subnets
# GDPR-compliant with VPC flow logs

resource "aws_vpc" "main" {
  cidr_block           = var.vpc_cidr
  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = {
    Name = "synaxis-${var.environment}-vpc"
    DataResidency = "EU"
    Environment = var.environment
  }
}

# Internet Gateway for public subnets
resource "aws_internet_gateway" "main" {
  vpc_id = aws_vpc.main.id

  tags = {
    Name = "synaxis-${var.environment}-igw"
    Environment = var.environment
  }
}

# Public Subnets (for ALB) - 3 AZs
resource "aws_subnet" "public" {
  count = 3

  vpc_id                  = aws_vpc.main.id
  cidr_block              = cidrsubnet(var.vpc_cidr, 4, count.index)
  availability_zone       = data.aws_availability_zones.available.names[count.index]
  map_public_ip_on_launch = true

  tags = {
    Name                     = "synaxis-${var.environment}-public-${data.aws_availability_zones.available.names[count.index]}"
    "kubernetes.io/role/elb" = "1"
    Type                     = "public"
    Environment              = var.environment
  }
}

# Private Subnets (for EKS nodes) - 3 AZs
resource "aws_subnet" "private" {
  count = 3

  vpc_id            = aws_vpc.main.id
  cidr_block        = cidrsubnet(var.vpc_cidr, 4, count.index + 3)
  availability_zone = data.aws_availability_zones.available.names[count.index]

  tags = {
    Name                              = "synaxis-${var.environment}-private-${data.aws_availability_zones.available.names[count.index]}"
    "kubernetes.io/role/internal-elb" = "1"
    Type                              = "private"
    Environment                       = var.environment
  }
}

# Database Subnets (for RDS) - 3 AZs
resource "aws_subnet" "database" {
  count = 3

  vpc_id            = aws_vpc.main.id
  cidr_block        = cidrsubnet(var.vpc_cidr, 4, count.index + 6)
  availability_zone = data.aws_availability_zones.available.names[count.index]

  tags = {
    Name = "synaxis-${var.environment}-database-${data.aws_availability_zones.available.names[count.index]}"
    Type = "database"
    Environment = var.environment
  }
}

# Cache Subnets (for ElastiCache) - 3 AZs
resource "aws_subnet" "cache" {
  count = 3

  vpc_id            = aws_vpc.main.id
  cidr_block        = cidrsubnet(var.vpc_cidr, 4, count.index + 9)
  availability_zone = data.aws_availability_zones.available.names[count.index]

  tags = {
    Name = "synaxis-${var.environment}-cache-${data.aws_availability_zones.available.names[count.index]}"
    Type = "cache"
    Environment = var.environment
  }
}

# Elastic IPs for NAT Gateways
resource "aws_eip" "nat" {
  count  = 3
  domain = "vpc"

  tags = {
    Name = "synaxis-${var.environment}-nat-eip-${count.index + 1}"
    Environment = var.environment
  }

  depends_on = [aws_internet_gateway.main]
}

# NAT Gateways in each public subnet (multi-AZ for high availability)
resource "aws_nat_gateway" "main" {
  count = 3

  allocation_id = aws_eip.nat[count.index].id
  subnet_id     = aws_subnet.public[count.index].id

  tags = {
    Name = "synaxis-${var.environment}-nat-${data.aws_availability_zones.available.names[count.index]}"
    Environment = var.environment
  }

  depends_on = [aws_internet_gateway.main]
}

# Route table for public subnets
resource "aws_route_table" "public" {
  vpc_id = aws_vpc.main.id

  tags = {
    Name = "synaxis-${var.environment}-public-rt"
    Type = "public"
    Environment = var.environment
  }
}

# Route to Internet Gateway for public subnets
resource "aws_route" "public_internet_gateway" {
  route_table_id         = aws_route_table.public.id
  destination_cidr_block = "0.0.0.0/0"
  gateway_id             = aws_internet_gateway.main.id
}

# Associate public subnets with public route table
resource "aws_route_table_association" "public" {
  count = 3

  subnet_id      = aws_subnet.public[count.index].id
  route_table_id = aws_route_table.public.id
}

# Route tables for private subnets (one per AZ for NAT Gateway redundancy)
resource "aws_route_table" "private" {
  count = 3

  vpc_id = aws_vpc.main.id

  tags = {
    Name = "synaxis-${var.environment}-private-rt-${data.aws_availability_zones.available.names[count.index]}"
    Type = "private"
    Environment = var.environment
  }
}

# Routes to NAT Gateways for private subnets
resource "aws_route" "private_nat_gateway" {
  count = 3

  route_table_id         = aws_route_table.private[count.index].id
  destination_cidr_block = "0.0.0.0/0"
  nat_gateway_id         = aws_nat_gateway.main[count.index].id
}

# Associate private subnets with their route tables
resource "aws_route_table_association" "private" {
  count = 3

  subnet_id      = aws_subnet.private[count.index].id
  route_table_id = aws_route_table.private[count.index].id
}

# Route table for database subnets (no internet access)
resource "aws_route_table" "database" {
  vpc_id = aws_vpc.main.id

  tags = {
    Name = "synaxis-${var.environment}-database-rt"
    Type = "database"
    Environment = var.environment
  }
}

# Associate database subnets with database route table
resource "aws_route_table_association" "database" {
  count = 3

  subnet_id      = aws_subnet.database[count.index].id
  route_table_id = aws_route_table.database.id
}

# Route table for cache subnets (no internet access)
resource "aws_route_table" "cache" {
  vpc_id = aws_vpc.main.id

  tags = {
    Name = "synaxis-${var.environment}-cache-rt"
    Type = "cache"
    Environment = var.environment
  }
}

# Associate cache subnets with cache route table
resource "aws_route_table_association" "cache" {
  count = 3

  subnet_id      = aws_subnet.cache[count.index].id
  route_table_id = aws_route_table.cache.id
}

# VPC Flow Logs for security monitoring (GDPR compliance)
resource "aws_flow_log" "main" {
  count = var.enable_vpc_flow_logs ? 1 : 0

  iam_role_arn    = aws_iam_role.flow_logs[0].arn
  log_destination = aws_cloudwatch_log_group.flow_logs[0].arn
  traffic_type    = "ALL"
  vpc_id          = aws_vpc.main.id

  tags = {
    Name       = "synaxis-${var.environment}-vpc-flow-logs"
    Compliance = "GDPR"
    Environment = var.environment
  }
}

# CloudWatch Log Group for VPC Flow Logs
resource "aws_cloudwatch_log_group" "flow_logs" {
  count = var.enable_vpc_flow_logs ? 1 : 0

  name              = "/aws/vpc/synaxis-${var.environment}-flow-logs"
  retention_in_days = 90

  tags = {
    Name       = "synaxis-${var.environment}-vpc-flow-logs"
    Compliance = "GDPR"
    Environment = var.environment
  }
}

# IAM Role for VPC Flow Logs
resource "aws_iam_role" "flow_logs" {
  count = var.enable_vpc_flow_logs ? 1 : 0

  name = "synaxis-${var.environment}-vpc-flow-logs-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "vpc-flow-logs.amazonaws.com"
        }
      }
    ]
  })

  tags = {
    Name = "synaxis-${var.environment}-vpc-flow-logs-role"
    Environment = var.environment
  }
}

# IAM Policy for VPC Flow Logs
resource "aws_iam_role_policy" "flow_logs" {
  count = var.enable_flow_logs ? 1 : 0

  name = "synaxis-${var.environment}-vpc-flow-logs-policy"
  role = aws_iam_role.flow_logs[0].id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents",
          "logs:DescribeLogGroups",
          "logs:DescribeLogStreams"
        ]
        Effect   = "Allow"
        Resource = "*"
      }
    ]
  })
}

# DB Subnet Group for RDS
resource "aws_db_subnet_group" "main" {
  name       = "synaxis-${var.environment}-db-subnet-group"
  subnet_ids = aws_subnet.database[*].id

  tags = {
    Name = "synaxis-${var.environment}-db-subnet-group"
    Environment = var.environment
  }
}

# ElastiCache Subnet Group
resource "aws_elasticache_subnet_group" "main" {
  name       = "synaxis-${var.environment}-cache-subnet-group"
  subnet_ids = aws_subnet.cache[*].id

  tags = {
    Name = "synaxis-${var.environment}-cache-subnet-group"
    Environment = var.environment
  }
}
