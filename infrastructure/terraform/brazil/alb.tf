# Application Load Balancer Configuration for Brazil Region

resource "aws_lb" "brazil" {
  name               = "synaxis-brazil-alb-${var.environment}"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb.id]
  subnets            = aws_subnet.public[*].id

  enable_deletion_protection = var.enable_deletion_protection
  enable_http2              = true
  enable_cross_zone_load_balancing = true

  drop_invalid_header_fields = true

  access_logs {
    bucket  = aws_s3_bucket.alb_logs.id
    prefix  = "alb"
    enabled = true
  }

  tags = {
    Name = "synaxis-brazil-alb-${var.environment}"
  }
}

# S3 Bucket for ALB Access Logs
resource "aws_s3_bucket" "alb_logs" {
  bucket = "synaxis-brazil-alb-logs-${var.environment}-${random_id.suffix.hex}"

  tags = {
    Name       = "synaxis-brazil-alb-logs"
    Compliance = "LGPD"
  }
}

resource "aws_s3_bucket_versioning" "alb_logs" {
  bucket = aws_s3_bucket.alb_logs.id

  versioning_configuration {
    status = "Enabled"
  }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "alb_logs" {
  bucket = aws_s3_bucket.alb_logs.id

  rule {
    apply_server_side_encryption_by_default {
      sse_algorithm     = "aws:kms"
      kms_master_key_id = aws_kms_key.synaxis_brazil.arn
    }
  }
}

resource "aws_s3_bucket_public_access_block" "alb_logs" {
  bucket = aws_s3_bucket.alb_logs.id

  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_lifecycle_configuration" "alb_logs" {
  bucket = aws_s3_bucket.alb_logs.id

  rule {
    id     = "delete-old-logs"
    status = "Enabled"

    expiration {
      days = var.log_retention_days
    }
  }
}

# S3 Bucket Policy for ALB Access Logs
data "aws_elb_service_account" "main" {}

resource "aws_s3_bucket_policy" "alb_logs" {
  bucket = aws_s3_bucket.alb_logs.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Principal = {
          AWS = data.aws_elb_service_account.main.arn
        }
        Action   = "s3:PutObject"
        Resource = "${aws_s3_bucket.alb_logs.arn}/*"
      },
      {
        Effect = "Allow"
        Principal = {
          Service = "delivery.logs.amazonaws.com"
        }
        Action   = "s3:PutObject"
        Resource = "${aws_s3_bucket.alb_logs.arn}/*"
        Condition = {
          StringEquals = {
            "s3:x-amz-acl" = "bucket-owner-full-control"
          }
        }
      },
      {
        Effect = "Allow"
        Principal = {
          Service = "delivery.logs.amazonaws.com"
        }
        Action   = "s3:GetBucketAcl"
        Resource = aws_s3_bucket.alb_logs.arn
      }
    ]
  })
}

# Target Group for Synaxis API
resource "aws_lb_target_group" "synaxis_api" {
  name        = "synaxis-brazil-api-${var.environment}"
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = aws_vpc.brazil.id
  target_type = "ip"

  health_check {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = "200"
    path                = "/health"
    port                = "traffic-port"
    protocol            = "HTTP"
    timeout             = 5
    unhealthy_threshold = 3
  }

  deregistration_delay = 30

  stickiness {
    type            = "lb_cookie"
    cookie_duration = 86400
    enabled         = true
  }

  tags = {
    Name = "synaxis-brazil-api-tg-${var.environment}"
  }
}

# HTTPS Listener
resource "aws_lb_listener" "https" {
  load_balancer_arn = aws_lb.brazil.arn
  port              = "443"
  protocol          = "HTTPS"
  ssl_policy        = "ELBSecurityPolicy-TLS13-1-2-2021-06"
  certificate_arn   = aws_acm_certificate.brazil.arn

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.synaxis_api.arn
  }
}

# HTTP Listener (redirect to HTTPS)
resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.brazil.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type = "redirect"

    redirect {
      port        = "443"
      protocol    = "HTTPS"
      status_code = "HTTP_301"
    }
  }
}

# ACM Certificate for ALB
resource "aws_acm_certificate" "brazil" {
  domain_name       = "*.synaxis.com.br"
  validation_method = "DNS"

  subject_alternative_names = [
    "synaxis.com.br",
    "api.synaxis.com.br"
  ]

  lifecycle {
    create_before_destroy = true
  }

  tags = {
    Name = "synaxis-brazil-cert-${var.environment}"
  }
}

# Qdrant Vector Database EC2 Instance
data "aws_ami" "ubuntu" {
  most_recent = true
  owners      = ["099720109477"] # Canonical

  filter {
    name   = "name"
    values = ["ubuntu/images/hvm-ssd/ubuntu-jammy-22.04-amd64-server-*"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

resource "aws_instance" "qdrant" {
  ami           = data.aws_ami.ubuntu.id
  instance_type = var.qdrant_instance_type
  subnet_id     = aws_subnet.private[0].id

  vpc_security_group_ids = [aws_security_group.qdrant.id]

  iam_instance_profile = aws_iam_instance_profile.qdrant.name

  root_block_device {
    volume_type           = var.qdrant_volume_type
    volume_size           = var.qdrant_volume_size
    encrypted             = true
    kms_key_id            = aws_kms_key.synaxis_brazil.arn
    delete_on_termination = false
  }

  user_data = base64encode(templatefile("${path.module}/qdrant-init.sh", {
    aws_region = var.aws_region
  }))

  metadata_options {
    http_endpoint               = "enabled"
    http_tokens                 = "required"
    http_put_response_hop_limit = 1
    instance_metadata_tags      = "enabled"
  }

  monitoring = true

  tags = {
    Name = "synaxis-brazil-qdrant-${var.environment}"
  }
}

# IAM Role for Qdrant Instance
resource "aws_iam_role" "qdrant" {
  name = "synaxis-brazil-qdrant-${var.environment}"

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

  tags = {
    Name = "synaxis-brazil-qdrant-role"
  }
}

resource "aws_iam_instance_profile" "qdrant" {
  name = "synaxis-brazil-qdrant-${var.environment}"
  role = aws_iam_role.qdrant.name
}

resource "aws_iam_role_policy_attachment" "qdrant_ssm" {
  role       = aws_iam_role.qdrant.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

# IAM Policy for Qdrant CloudWatch Logs
resource "aws_iam_policy" "qdrant_logs" {
  name        = "synaxis-brazil-qdrant-logs-${var.environment}"
  description = "Allow Qdrant to write CloudWatch logs"

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:CreateLogStream",
          "logs:PutLogEvents",
          "logs:DescribeLogStreams"
        ]
        Resource = "${aws_cloudwatch_log_group.qdrant.arn}:*"
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "qdrant_logs" {
  role       = aws_iam_role.qdrant.name
  policy_arn = aws_iam_policy.qdrant_logs.arn
}

# CloudWatch Log Group for Qdrant
resource "aws_cloudwatch_log_group" "qdrant" {
  name              = "/aws/ec2/synaxis-brazil-qdrant-${var.environment}"
  retention_in_days = var.log_retention_days
  kms_key_id        = aws_kms_key.synaxis_brazil.arn

  tags = {
    Name = "synaxis-brazil-qdrant-logs"
  }
}
