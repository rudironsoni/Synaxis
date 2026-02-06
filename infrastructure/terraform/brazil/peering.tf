# VPC Peering Connections for Brazil Region
# Connections to US (us-east-1) and EU (eu-west-1) regions

# Provider for US region
provider "aws" {
  alias  = "us"
  region = var.us_region
}

# Provider for EU region
provider "aws" {
  alias  = "eu"
  region = var.eu_region
}

# VPC Peering Connection to US Region
resource "aws_vpc_peering_connection" "brazil_to_us" {
  count = var.us_vpc_id != "" ? 1 : 0

  vpc_id        = aws_vpc.brazil.id
  peer_vpc_id   = var.us_vpc_id
  peer_region   = var.us_region
  auto_accept   = false

  tags = {
    Name        = "synaxis-brazil-to-us-${var.environment}"
    Side        = "Requester"
    Environment = var.environment
  }
}

# Accept VPC Peering Connection in US Region
resource "aws_vpc_peering_connection_accepter" "us_accept_brazil" {
  count                     = var.us_vpc_id != "" ? 1 : 0
  provider                  = aws.us
  vpc_peering_connection_id = aws_vpc_peering_connection.brazil_to_us[0].id
  auto_accept               = true

  tags = {
    Name        = "synaxis-us-accept-brazil-${var.environment}"
    Side        = "Accepter"
    Environment = var.environment
  }
}

# VPC Peering Connection to EU Region
resource "aws_vpc_peering_connection" "brazil_to_eu" {
  count = var.eu_vpc_id != "" ? 1 : 0

  vpc_id        = aws_vpc.brazil.id
  peer_vpc_id   = var.eu_vpc_id
  peer_region   = var.eu_region
  auto_accept   = false

  tags = {
    Name        = "synaxis-brazil-to-eu-${var.environment}"
    Side        = "Requester"
    Environment = var.environment
  }
}

# Accept VPC Peering Connection in EU Region
resource "aws_vpc_peering_connection_accepter" "eu_accept_brazil" {
  count                     = var.eu_vpc_id != "" ? 1 : 0
  provider                  = aws.eu
  vpc_peering_connection_id = aws_vpc_peering_connection.brazil_to_eu[0].id
  auto_accept               = true

  tags = {
    Name        = "synaxis-eu-accept-brazil-${var.environment}"
    Side        = "Accepter"
    Environment = var.environment
  }
}

# Route to US VPC from Brazil Private Subnets
resource "aws_route" "brazil_private_to_us" {
  count                     = var.us_vpc_id != "" ? var.availability_zones_count : 0
  route_table_id            = aws_route_table.private[count.index].id
  destination_cidr_block    = var.us_vpc_cidr
  vpc_peering_connection_id = aws_vpc_peering_connection.brazil_to_us[0].id
}

# Route to EU VPC from Brazil Private Subnets
resource "aws_route" "brazil_private_to_eu" {
  count                     = var.eu_vpc_id != "" ? var.availability_zones_count : 0
  route_table_id            = aws_route_table.private[count.index].id
  destination_cidr_block    = var.eu_vpc_cidr
  vpc_peering_connection_id = aws_vpc_peering_connection.brazil_to_eu[0].id
}

# Route to US VPC from Brazil Database Subnets
resource "aws_route" "brazil_database_to_us" {
  count                     = var.us_vpc_id != "" ? var.availability_zones_count : 0
  route_table_id            = aws_route_table.database[count.index].id
  destination_cidr_block    = var.us_vpc_cidr
  vpc_peering_connection_id = aws_vpc_peering_connection.brazil_to_us[0].id
}

# Route to EU VPC from Brazil Database Subnets
resource "aws_route" "brazil_database_to_eu" {
  count                     = var.eu_vpc_id != "" ? var.availability_zones_count : 0
  route_table_id            = aws_route_table.database[count.index].id
  destination_cidr_block    = var.eu_vpc_cidr
  vpc_peering_connection_id = aws_vpc_peering_connection.brazil_to_eu[0].id
}

# Output peering connection IDs
output "vpc_peering_connection_us_id" {
  description = "VPC peering connection ID for Brazil to US"
  value       = var.us_vpc_id != "" ? aws_vpc_peering_connection.brazil_to_us[0].id : null
}

output "vpc_peering_connection_eu_id" {
  description = "VPC peering connection ID for Brazil to EU"
  value       = var.eu_vpc_id != "" ? aws_vpc_peering_connection.brazil_to_eu[0].id : null
}
