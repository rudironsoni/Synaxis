# VPC Configuration for Brazil Region
# 3 Availability Zones with private, public, and database subnets

resource "aws_vpc" "brazil" {
  cidr_block           = var.vpc_cidr
  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = {
    Name = "synaxis-brazil-vpc-${var.environment}"
  }
}

# Internet Gateway
resource "aws_internet_gateway" "brazil" {
  vpc_id = aws_vpc.brazil.id

  tags = {
    Name = "synaxis-brazil-igw-${var.environment}"
  }
}

# Elastic IPs for NAT Gateways
resource "aws_eip" "nat" {
  count  = var.availability_zones_count
  domain = "vpc"

  tags = {
    Name = "synaxis-brazil-nat-eip-${count.index + 1}-${var.environment}"
  }

  depends_on = [aws_internet_gateway.brazil]
}

# NAT Gateways (one per AZ for high availability)
resource "aws_nat_gateway" "brazil" {
  count         = var.availability_zones_count
  allocation_id = aws_eip.nat[count.index].id
  subnet_id     = aws_subnet.public[count.index].id

  tags = {
    Name = "synaxis-brazil-nat-${count.index + 1}-${var.environment}"
  }

  depends_on = [aws_internet_gateway.brazil]
}

# Public Subnets (for ALB and NAT gateways)
resource "aws_subnet" "public" {
  count                   = var.availability_zones_count
  vpc_id                  = aws_vpc.brazil.id
  cidr_block              = var.public_subnet_cidrs[count.index]
  availability_zone       = data.aws_availability_zones.available.names[count.index]
  map_public_ip_on_launch = true

  tags = {
    Name                                            = "synaxis-brazil-public-${count.index + 1}-${var.environment}"
    "kubernetes.io/role/elb"                        = "1"
    "kubernetes.io/cluster/synaxis-brazil-${var.environment}" = "shared"
  }
}

# Private Subnets (for EKS nodes and application workloads)
resource "aws_subnet" "private" {
  count             = var.availability_zones_count
  vpc_id            = aws_vpc.brazil.id
  cidr_block        = var.private_subnet_cidrs[count.index]
  availability_zone = data.aws_availability_zones.available.names[count.index]

  tags = {
    Name                                            = "synaxis-brazil-private-${count.index + 1}-${var.environment}"
    "kubernetes.io/role/internal-elb"               = "1"
    "kubernetes.io/cluster/synaxis-brazil-${var.environment}" = "shared"
  }
}

# Database Subnets (for RDS and ElastiCache)
resource "aws_subnet" "database" {
  count             = var.availability_zones_count
  vpc_id            = aws_vpc.brazil.id
  cidr_block        = var.database_subnet_cidrs[count.index]
  availability_zone = data.aws_availability_zones.available.names[count.index]

  tags = {
    Name = "synaxis-brazil-database-${count.index + 1}-${var.environment}"
  }
}

# Route Table for Public Subnets
resource "aws_route_table" "public" {
  vpc_id = aws_vpc.brazil.id

  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.brazil.id
  }

  tags = {
    Name = "synaxis-brazil-public-rt-${var.environment}"
  }
}

# Route Table Associations for Public Subnets
resource "aws_route_table_association" "public" {
  count          = var.availability_zones_count
  subnet_id      = aws_subnet.public[count.index].id
  route_table_id = aws_route_table.public.id
}

# Route Tables for Private Subnets (one per AZ for NAT Gateway)
resource "aws_route_table" "private" {
  count  = var.availability_zones_count
  vpc_id = aws_vpc.brazil.id

  route {
    cidr_block     = "0.0.0.0/0"
    nat_gateway_id = aws_nat_gateway.brazil[count.index].id
  }

  tags = {
    Name = "synaxis-brazil-private-rt-${count.index + 1}-${var.environment}"
  }
}

# Route Table Associations for Private Subnets
resource "aws_route_table_association" "private" {
  count          = var.availability_zones_count
  subnet_id      = aws_subnet.private[count.index].id
  route_table_id = aws_route_table.private[count.index].id
}

# Route Tables for Database Subnets
resource "aws_route_table" "database" {
  count  = var.availability_zones_count
  vpc_id = aws_vpc.brazil.id

  tags = {
    Name = "synaxis-brazil-database-rt-${count.index + 1}-${var.environment}"
  }
}

# Route Table Associations for Database Subnets
resource "aws_route_table_association" "database" {
  count          = var.availability_zones_count
  subnet_id      = aws_subnet.database[count.index].id
  route_table_id = aws_route_table.database[count.index].id
}

# DB Subnet Group for RDS
resource "aws_db_subnet_group" "brazil" {
  name       = "synaxis-brazil-db-subnet-group-${var.environment}"
  subnet_ids = aws_subnet.database[*].id

  tags = {
    Name = "synaxis-brazil-db-subnet-group-${var.environment}"
  }
}

# ElastiCache Subnet Group
resource "aws_elasticache_subnet_group" "brazil" {
  name       = "synaxis-brazil-cache-subnet-group-${var.environment}"
  subnet_ids = aws_subnet.database[*].id

  tags = {
    Name = "synaxis-brazil-cache-subnet-group-${var.environment}"
  }
}

# VPC Flow Logs for security monitoring (LGPD compliance)
resource "aws_flow_log" "brazil" {
  iam_role_arn    = aws_iam_role.vpc_flow_log.arn
  log_destination = aws_cloudwatch_log_group.vpc_flow_log.arn
  traffic_type    = "ALL"
  vpc_id          = aws_vpc.brazil.id

  tags = {
    Name = "synaxis-brazil-vpc-flow-log-${var.environment}"
  }
}

resource "aws_cloudwatch_log_group" "vpc_flow_log" {
  name              = "/aws/vpc/synaxis-brazil-${var.environment}"
  retention_in_days = var.log_retention_days
  kms_key_id        = aws_kms_key.synaxis_brazil.arn

  tags = {
    Name = "synaxis-brazil-vpc-flow-log"
  }
}

resource "aws_iam_role" "vpc_flow_log" {
  name = "synaxis-brazil-vpc-flow-log-${var.environment}"

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
    Name = "synaxis-brazil-vpc-flow-log-role"
  }
}

resource "aws_iam_role_policy" "vpc_flow_log" {
  name = "synaxis-brazil-vpc-flow-log-policy"
  role = aws_iam_role.vpc_flow_log.id

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
