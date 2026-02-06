# EKS Cluster for Synaxis EU Region
# Production-grade Kubernetes cluster with managed node groups
# IRSA (IAM Roles for Service Accounts) enabled for secure workload access

# EKS Cluster IAM Role
resource "aws_iam_role" "cluster" {
  name_prefix = "synaxis-${var.environment}-eks-cluster-"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "eks.amazonaws.com"
        }
      }
    ]
  })

  tags = { Name = "synaxis-${var.environment}", Environment = var.environment }
}

# Attach required policies to cluster role
resource "aws_iam_role_policy_attachment" "cluster_policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSClusterPolicy"
  role       = aws_iam_role.cluster.name
}

resource "aws_iam_role_policy_attachment" "cluster_vpc_policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSVPCResourceController"
  role       = aws_iam_role.cluster.name
}

# Security Group for EKS Cluster
resource "aws_security_group" "cluster" {
  name_prefix = "synaxis-${var.environment}-eks-cluster-"
  description = "Security group for Synaxis EKS cluster"
  vpc_id      = var.vpc_id

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
    description = "Allow all outbound traffic"
  }

  tags = {
    
    {
      Name = "synaxis-${var.environment}-eks-cluster-sg"
    }
  )
}

# Security Group for EKS Nodes
resource "aws_security_group" "node" {
  name_prefix = "synaxis-${var.environment}-eks-node-"
  description = "Security group for Synaxis EKS worker nodes"
  vpc_id      = var.vpc_id

  # Allow nodes to communicate with each other
  ingress {
    from_port = 0
    to_port   = 65535
    protocol  = "-1"
    self      = true
    description = "Allow node to node communication"
  }

  # Allow worker Kubelets and pods to receive communication from the cluster control plane
  ingress {
    from_port       = 1025
    to_port         = 65535
    protocol        = "tcp"
    security_groups = [aws_security_group.cluster.id]
    description     = "Allow worker Kubelets and pods"
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
    description = "Allow all outbound traffic"
  }

  tags = {
    
    {
      Name = "synaxis-${var.environment}-eks-node-sg"
      "kubernetes.io/cluster/synaxis-${var.environment}" = "owned"
    }
  )
}

# EKS Cluster
resource "aws_eks_cluster" "main" {
  name     = "synaxis-${var.environment}"
  role_arn = aws_iam_role.cluster.arn
  version  = var.cluster_version

  vpc_config {
    subnet_ids              = var.private_subnet_ids
    endpoint_private_access = true
    endpoint_public_access  = true # Set to false for production with VPN
    public_access_cidrs     = ["0.0.0.0/0"] # Restrict to office IPs in production
    security_group_ids      = [aws_security_group.cluster.id]
  }

  # Encryption configuration (secrets encryption at rest)
  encryption_config {
    provider {
      key_arn = var.kms_key_id
    }
    resources = ["secrets"]
  }

  # Enable control plane logging (GDPR compliance)
  enabled_cluster_log_types = ["api", "audit", "authenticator", "controllerManager", "scheduler"]

  depends_on = [
    aws_iam_role_policy_attachment.cluster_policy,
    aws_iam_role_policy_attachment.cluster_vpc_policy,
  ]

  tags = {
    
    {
      Name       = "synaxis-${var.environment}-eks"
      Compliance = "GDPR"
    }
  )
}

# OIDC Provider for IRSA (IAM Roles for Service Accounts)
data "tls_certificate" "cluster" {
  url = aws_eks_cluster.main.identity[0].oidc[0].issuer
}

resource "aws_iam_openid_connect_provider" "cluster" {
  client_id_list  = ["sts.amazonaws.com"]
  thumbprint_list = [data.tls_certificate.cluster.certificates[0].sha1_fingerprint]
  url             = aws_eks_cluster.main.identity[0].oidc[0].issuer

  tags = { Name = "synaxis-${var.environment}", Environment = var.environment }
}

# EKS Node Group IAM Role
resource "aws_iam_role" "node" {
  name_prefix = "synaxis-${var.environment}-eks-node-"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ec2.amazonaws.com"
        }
      }
    ]
  })

  tags = { Name = "synaxis-${var.environment}", Environment = var.environment }
}

# Attach required policies to node role
resource "aws_iam_role_policy_attachment" "node_policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKSWorkerNodePolicy"
  role       = aws_iam_role.node.name
}

resource "aws_iam_role_policy_attachment" "node_cni_policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEKS_CNI_Policy"
  role       = aws_iam_role.node.name
}

resource "aws_iam_role_policy_attachment" "node_registry_policy" {
  policy_arn = "arn:aws:iam::aws:policy/AmazonEC2ContainerRegistryReadOnly"
  role       = aws_iam_role.node.name
}

# EKS Managed Node Group
resource "aws_eks_node_group" "main" {
  cluster_name    = aws_eks_cluster.main.name
  node_group_name = "synaxis-${var.environment}-node-group"
  node_role_arn   = aws_iam_role.node.arn
  subnet_ids      = var.private_subnet_ids

  # Scaling configuration
  scaling_config {
    desired_size = var.node_desired_size
    max_size     = var.node_max_size
    min_size     = var.node_min_size
  }

  # Instance configuration
  instance_types = var.node_instance_types
  capacity_type  = "ON_DEMAND"
  disk_size      = 50

  # Update configuration
  update_config {
    max_unavailable = 1
  }

  # Launch template for custom configuration
  launch_template {
    id      = aws_launch_template.node.id
    version = "$Latest"
  }

  # Labels
  labels = {
    role        = "worker"
    environment = var.environment
  }

  # Taints for specific workloads (none for general purpose)
  # taint {
  #   key    = "dedicated"
  #   value  = "gpu"
  #   effect = "NO_SCHEDULE"
  # }

  depends_on = [
    aws_iam_role_policy_attachment.node_policy,
    aws_iam_role_policy_attachment.node_cni_policy,
    aws_iam_role_policy_attachment.node_registry_policy,
  ]

  tags = {
    
    {
      Name = "synaxis-${var.environment}-node-group"
    }
  )

  lifecycle {
    create_before_destroy = true
  }
}

# Launch Template for Node Group
resource "aws_launch_template" "node" {
  name_prefix = "synaxis-${var.environment}-eks-node-"
  description = "Launch template for Synaxis EKS nodes"

  block_device_mappings {
    device_name = "/dev/xvda"

    ebs {
      volume_size           = 50
      volume_type           = "gp3"
      encrypted             = true
      kms_key_id            = var.kms_key_id
      delete_on_termination = true
    }
  }

  metadata_options {
    http_endpoint               = "enabled"
    http_tokens                 = "required" # IMDSv2 required
    http_put_response_hop_limit = 1
  }

  monitoring {
    enabled = true
  }

  network_interfaces {
    associate_public_ip_address = false
    security_groups             = [aws_security_group.node.id]
    delete_on_termination       = true
  }

  tag_specifications {
    resource_type = "instance"

    tags = {
      
      {
        Name = "synaxis-${var.environment}-eks-node"
      }
    )
  }

  tag_specifications {
    resource_type = "volume"

    tags = {
      
      {
        Name       = "synaxis-${var.environment}-eks-node-volume"
        Encryption = "AES-256"
      }
    )
  }

  user_data = base64encode(<<-EOF
    #!/bin/bash
    set -o xtrace
    /etc/eks/bootstrap.sh synaxis-${var.environment}
  EOF
  )

  tags = { Name = "synaxis-${var.environment}", Environment = var.environment }
}

# EKS Add-ons
resource "aws_eks_addon" "vpc_cni" {
  cluster_name = aws_eks_cluster.main.name
  addon_name   = "vpc-cni"
  addon_version = "v1.15.1-eksbuild.1"
  resolve_conflicts_on_update = "PRESERVE"

  tags = { Name = "synaxis-${var.environment}", Environment = var.environment }
}

resource "aws_eks_addon" "coredns" {
  cluster_name = aws_eks_cluster.main.name
  addon_name   = "coredns"
  addon_version = "v1.10.1-eksbuild.6"
  resolve_conflicts_on_update = "PRESERVE"

  depends_on = [aws_eks_node_group.main]

  tags = { Name = "synaxis-${var.environment}", Environment = var.environment }
}

resource "aws_eks_addon" "kube_proxy" {
  cluster_name = aws_eks_cluster.main.name
  addon_name   = "kube-proxy"
  addon_version = "v1.28.2-eksbuild.2"
  resolve_conflicts_on_update = "PRESERVE"

  tags = { Name = "synaxis-${var.environment}", Environment = var.environment }
}

resource "aws_eks_addon" "ebs_csi_driver" {
  cluster_name = aws_eks_cluster.main.name
  addon_name   = "aws-ebs-csi-driver"
  addon_version = "v1.26.0-eksbuild.1"
  resolve_conflicts_on_update = "PRESERVE"
  service_account_role_arn = aws_iam_role.ebs_csi_driver.arn

  tags = { Name = "synaxis-${var.environment}", Environment = var.environment }
}

# IAM Role for EBS CSI Driver
resource "aws_iam_role" "ebs_csi_driver" {
  name_prefix = "synaxis-${var.environment}-ebs-csi-driver-"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRoleWithWebIdentity"
        Effect = "Allow"
        Principal = {
          Federated = aws_iam_openid_connect_provider.cluster.arn
        }
        Condition = {
          StringEquals = {
            "${replace(aws_iam_openid_connect_provider.cluster.url, "https://", "")}:sub" = "system:serviceaccount:kube-system:ebs-csi-controller-sa"
            "${replace(aws_iam_openid_connect_provider.cluster.url, "https://", "")}:aud" = "sts.amazonaws.com"
          }
        }
      }
    ]
  })

  tags = { Name = "synaxis-${var.environment}", Environment = var.environment }
}

resource "aws_iam_role_policy_attachment" "ebs_csi_driver" {
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonEBSCSIDriverPolicy"
  role       = aws_iam_role.ebs_csi_driver.name
}
