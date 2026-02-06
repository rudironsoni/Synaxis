#!/bin/bash
set -e

# Update system
apt-get update
apt-get upgrade -y

# Install required packages
apt-get install -y \
    docker.io \
    awscli \
    jq \
    curl

# Start and enable Docker
systemctl start docker
systemctl enable docker

# Format and mount EBS volume for Qdrant data
if [ ! -d /var/lib/qdrant ]; then
    mkfs -t ext4 /dev/nvme1n1
    mkdir -p /var/lib/qdrant
    mount /dev/nvme1n1 /var/lib/qdrant
    echo '/dev/nvme1n1 /var/lib/qdrant ext4 defaults,nofail 0 2' >> /etc/fstab
fi

# Set permissions
chown -R 1000:1000 /var/lib/qdrant

# Pull and run Qdrant
docker pull qdrant/qdrant:latest

# Create Qdrant configuration
cat > /etc/qdrant-config.yaml <<EOF
service:
  host: 0.0.0.0
  http_port: 6333
  grpc_port: 6334

storage:
  storage_path: /qdrant/storage
  snapshots_path: /qdrant/snapshots
  on_disk_payload: true
  wal:
    wal_capacity_mb: 256
    wal_segments_ahead: 2

cluster:
  enabled: false

tls:
  enabled: false
EOF

# Run Qdrant as Docker container
docker run -d \
    --name qdrant \
    --restart always \
    -p 6333:6333 \
    -p 6334:6334 \
    -v /var/lib/qdrant:/qdrant/storage:z \
    -v /etc/qdrant-config.yaml:/qdrant/config/production.yaml \
    qdrant/qdrant:latest

# Install CloudWatch agent
wget https://s3.${region}.amazonaws.com/amazoncloudwatch-agent-${region}/ubuntu/amd64/latest/amazon-cloudwatch-agent.deb
dpkg -i -E ./amazon-cloudwatch-agent.deb

# Configure CloudWatch agent
cat > /opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.json <<EOF
{
  "metrics": {
    "namespace": "Synaxis/Qdrant",
    "metrics_collected": {
      "disk": {
        "measurement": [
          {
            "name": "used_percent",
            "rename": "DiskUsedPercent",
            "unit": "Percent"
          }
        ],
        "metrics_collection_interval": 60,
        "resources": [
          "/var/lib/qdrant"
        ]
      },
      "mem": {
        "measurement": [
          {
            "name": "mem_used_percent",
            "rename": "MemoryUsedPercent",
            "unit": "Percent"
          }
        ],
        "metrics_collection_interval": 60
      }
    }
  },
  "logs": {
    "logs_collected": {
      "files": {
        "collect_list": [
          {
            "file_path": "/var/log/syslog",
            "log_group_name": "/aws/ec2/qdrant",
            "log_stream_name": "{instance_id}/syslog"
          }
        ]
      }
    }
  }
}
EOF

# Start CloudWatch agent
/opt/aws/amazon-cloudwatch-agent/bin/amazon-cloudwatch-agent-ctl \
    -a fetch-config \
    -m ec2 \
    -s \
    -c file:/opt/aws/amazon-cloudwatch-agent/etc/amazon-cloudwatch-agent.json

echo "Qdrant installation completed successfully"
