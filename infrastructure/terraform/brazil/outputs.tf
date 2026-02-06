output "vpc_id" {
  description = "VPC ID"
  value       = aws_vpc.brazil.id
}

output "vpc_cidr" {
  description = "VPC CIDR block"
  value       = aws_vpc.brazil.cidr_block
}

output "private_subnet_ids" {
  description = "Private subnet IDs"
  value       = aws_subnet.private[*].id
}

output "public_subnet_ids" {
  description = "Public subnet IDs"
  value       = aws_subnet.public[*].id
}

output "database_subnet_ids" {
  description = "Database subnet IDs"
  value       = aws_subnet.database[*].id
}

output "eks_cluster_id" {
  description = "EKS cluster ID"
  value       = aws_eks_cluster.brazil.id
}

output "eks_cluster_name" {
  description = "EKS cluster name"
  value       = aws_eks_cluster.brazil.name
}

output "eks_cluster_endpoint" {
  description = "EKS cluster endpoint"
  value       = aws_eks_cluster.brazil.endpoint
}

output "eks_cluster_security_group_id" {
  description = "Security group ID attached to the EKS cluster"
  value       = aws_eks_cluster.brazil.vpc_config[0].cluster_security_group_id
}

output "eks_cluster_certificate_authority_data" {
  description = "Base64 encoded certificate data required to communicate with the cluster"
  value       = aws_eks_cluster.brazil.certificate_authority[0].data
  sensitive   = true
}

output "rds_endpoint" {
  description = "RDS instance endpoint"
  value       = aws_db_instance.brazil.endpoint
}

output "rds_address" {
  description = "RDS instance address"
  value       = aws_db_instance.brazil.address
}

output "rds_port" {
  description = "RDS instance port"
  value       = aws_db_instance.brazil.port
}

output "rds_database_name" {
  description = "RDS database name"
  value       = aws_db_instance.brazil.db_name
}

output "redis_endpoint" {
  description = "Redis cluster endpoint"
  value       = aws_elasticache_cluster.brazil.cache_nodes[0].address
}

output "redis_port" {
  description = "Redis cluster port"
  value       = aws_elasticache_cluster.brazil.cache_nodes[0].port
}

output "alb_dns_name" {
  description = "ALB DNS name"
  value       = aws_lb.brazil.dns_name
}

output "alb_arn" {
  description = "ALB ARN"
  value       = aws_lb.brazil.arn
}

output "alb_zone_id" {
  description = "ALB hosted zone ID"
  value       = aws_lb.brazil.zone_id
}

output "kms_key_id" {
  description = "KMS key ID"
  value       = aws_kms_key.synaxis_brazil.id
}

output "kms_key_arn" {
  description = "KMS key ARN"
  value       = aws_kms_key.synaxis_brazil.arn
}

output "qdrant_instance_id" {
  description = "Qdrant EC2 instance ID"
  value       = aws_instance.qdrant.id
}

output "qdrant_private_ip" {
  description = "Qdrant private IP address"
  value       = aws_instance.qdrant.private_ip
}

output "db_password_secret_arn" {
  description = "ARN of the Secrets Manager secret containing database password"
  value       = aws_secretsmanager_secret.db_password.arn
  sensitive   = true
}
