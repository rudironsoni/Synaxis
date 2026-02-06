# VPC Peering Connection to EU Region
resource "aws_vpc_peering_connection" "eu" {
  count = var.eu_vpc_id != "" ? 1 : 0

  vpc_id      = aws_vpc.main.id
  peer_vpc_id = var.eu_vpc_id
  peer_region = "eu-west-1"
  auto_accept = false

  tags = {
    Name = "${var.project_name}-us-to-eu-${var.environment}"
    Side = "Requester"
  }
}

# VPC Peering Connection to Brazil Region
resource "aws_vpc_peering_connection" "brazil" {
  count = var.brazil_vpc_id != "" ? 1 : 0

  vpc_id      = aws_vpc.main.id
  peer_vpc_id = var.brazil_vpc_id
  peer_region = "sa-east-1"
  auto_accept = false

  tags = {
    Name = "${var.project_name}-us-to-brazil-${var.environment}"
    Side = "Requester"
  }
}

# Routes to EU Region
resource "aws_route" "private_to_eu" {
  count = var.eu_vpc_id != "" ? var.availability_zones_count : 0

  route_table_id            = aws_route_table.private[count.index].id
  destination_cidr_block    = var.eu_vpc_cidr
  vpc_peering_connection_id = aws_vpc_peering_connection.eu[0].id
}

resource "aws_route" "database_to_eu" {
  count = var.eu_vpc_id != "" ? var.availability_zones_count : 0

  route_table_id            = aws_route_table.database[count.index].id
  destination_cidr_block    = var.eu_vpc_cidr
  vpc_peering_connection_id = aws_vpc_peering_connection.eu[0].id
}

# Routes to Brazil Region
resource "aws_route" "private_to_brazil" {
  count = var.brazil_vpc_id != "" ? var.availability_zones_count : 0

  route_table_id            = aws_route_table.private[count.index].id
  destination_cidr_block    = var.brazil_vpc_cidr
  vpc_peering_connection_id = aws_vpc_peering_connection.brazil[0].id
}

resource "aws_route" "database_to_brazil" {
  count = var.brazil_vpc_id != "" ? var.availability_zones_count : 0

  route_table_id            = aws_route_table.database[count.index].id
  destination_cidr_block    = var.brazil_vpc_cidr
  vpc_peering_connection_id = aws_vpc_peering_connection.brazil[0].id
}

# Security group rules for cross-region traffic
resource "aws_security_group_rule" "rds_from_eu" {
  count = var.eu_vpc_id != "" ? 1 : 0

  type              = "ingress"
  from_port         = 5432
  to_port           = 5432
  protocol          = "tcp"
  cidr_blocks       = [var.eu_vpc_cidr]
  security_group_id = aws_security_group.rds.id
  description       = "PostgreSQL from EU region"
}

resource "aws_security_group_rule" "rds_from_brazil" {
  count = var.brazil_vpc_id != "" ? 1 : 0

  type              = "ingress"
  from_port         = 5432
  to_port           = 5432
  protocol          = "tcp"
  cidr_blocks       = [var.brazil_vpc_cidr]
  security_group_id = aws_security_group.rds.id
  description       = "PostgreSQL from Brazil region"
}

resource "aws_security_group_rule" "redis_from_eu" {
  count = var.eu_vpc_id != "" ? 1 : 0

  type              = "ingress"
  from_port         = 6379
  to_port           = 6379
  protocol          = "tcp"
  cidr_blocks       = [var.eu_vpc_cidr]
  security_group_id = aws_security_group.redis.id
  description       = "Redis from EU region"
}

resource "aws_security_group_rule" "redis_from_brazil" {
  count = var.brazil_vpc_id != "" ? 1 : 0

  type              = "ingress"
  from_port         = 6379
  to_port           = 6379
  protocol          = "tcp"
  cidr_blocks       = [var.brazil_vpc_cidr]
  security_group_id = aws_security_group.redis.id
  description       = "Redis from Brazil region"
}

resource "aws_security_group_rule" "qdrant_from_eu" {
  count = var.eu_vpc_id != "" ? 1 : 0

  type              = "ingress"
  from_port         = 6333
  to_port           = 6334
  protocol          = "tcp"
  cidr_blocks       = [var.eu_vpc_cidr]
  security_group_id = aws_security_group.qdrant.id
  description       = "Qdrant from EU region"
}

resource "aws_security_group_rule" "qdrant_from_brazil" {
  count = var.brazil_vpc_id != "" ? 1 : 0

  type              = "ingress"
  from_port         = 6333
  to_port           = 6334
  protocol          = "tcp"
  cidr_blocks       = [var.brazil_vpc_cidr]
  security_group_id = aws_security_group.qdrant.id
  description       = "Qdrant from Brazil region"
}

# Allow EKS nodes to communicate with other regions
resource "aws_security_group_rule" "eks_to_eu" {
  count = var.eu_vpc_id != "" ? 1 : 0

  type              = "egress"
  from_port         = 0
  to_port           = 0
  protocol          = "-1"
  cidr_blocks       = [var.eu_vpc_cidr]
  security_group_id = module.eks.node_security_group_id
  description       = "Allow all traffic to EU region"
}

resource "aws_security_group_rule" "eks_to_brazil" {
  count = var.brazil_vpc_id != "" ? 1 : 0

  type              = "egress"
  from_port         = 0
  to_port           = 0
  protocol          = "-1"
  cidr_blocks       = [var.brazil_vpc_cidr]
  security_group_id = module.eks.node_security_group_id
  description       = "Allow all traffic to Brazil region"
}

resource "aws_security_group_rule" "eks_from_eu" {
  count = var.eu_vpc_id != "" ? 1 : 0

  type              = "ingress"
  from_port         = 0
  to_port           = 0
  protocol          = "-1"
  cidr_blocks       = [var.eu_vpc_cidr]
  security_group_id = module.eks.node_security_group_id
  description       = "Allow all traffic from EU region"
}

resource "aws_security_group_rule" "eks_from_brazil" {
  count = var.brazil_vpc_id != "" ? 1 : 0

  type              = "ingress"
  from_port         = 0
  to_port           = 0
  protocol          = "-1"
  cidr_blocks       = [var.brazil_vpc_cidr]
  security_group_id = module.eks.node_security_group_id
  description       = "Allow all traffic from Brazil region"
}
