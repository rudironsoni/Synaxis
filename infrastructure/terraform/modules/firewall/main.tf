# Azure Firewall Premium Module
# Provides network security with advanced threat protection

# Azure Firewall Premium
resource "azurerm_firewall" "main" {
  name                = "${var.name}-fw-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku_name            = "AZFW_VNet"
  sku_tier            = "Premium"
  firewall_policy_id  = azurerm_firewall_policy.main.id
  zones               = var.zones

  ip_configuration {
    name                 = "fw-ip-config"
    subnet_id            = var.firewall_subnet_id
    public_ip_address_id = azurerm_public_ip.main.id
  }

  tags = var.tags
}

# Public IP for Firewall
resource "azurerm_public_ip" "main" {
  name                = "${var.name}-fw-pip-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "Standard"
  allocation_method   = "Static"
  zones               = var.zones

  tags = var.tags
}

# Firewall Policy
resource "azurerm_firewall_policy" "main" {
  name                = "${var.name}-fw-policy-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "Premium"

  # DNS settings
  dns_proxy_enabled = true
  dns_servers       = var.dns_servers

  # Intrusion Detection and Prevention System (IDPS)
  intrusion_detection {
    mode = var.idps_mode
  }

  # TLS Inspection
  tls_inspection {
    enabled = var.tls_inspection_enabled
  }

  tags = var.tags
}

# Application Rule Collection for AKS Egress
resource "azurerm_firewall_policy_rule_collection_group" "aks_egress" {
  name               = "${var.name}-aks-egress-${var.environment}"
  firewall_policy_id = azurerm_firewall_policy.main.id
  priority           = 100

  application_rule_collection {
    name     = "aks-required-domains"
    priority = 100
    action   = "Allow"

    rule {
      name              = "aks-core-domains"
      source_addresses  = var.aks_node_cidr_blocks
      destination_fqdns = var.aks_required_fqdns

      protocols {
        type = "Https"
        port = 443
      }

      protocols {
        type = "Http"
        port = 80
      }
    }

    rule {
      name              = "aks-container-registry"
      source_addresses  = var.aks_node_cidr_blocks
      destination_fqdns = var.container_registry_fqdns

      protocols {
        type = "Https"
        port = 443
      }
    }

    rule {
      name              = "aks-monitoring"
      source_addresses  = var.aks_node_cidr_blocks
      destination_fqdns = var.monitoring_fqdns

      protocols {
        type = "Https"
        port = 443
      }
    }

    rule {
      name              = "aks-azure-services"
      source_addresses  = var.aks_node_cidr_blocks
      destination_fqdns = var.azure_services_fqdns

      protocols {
        type = "Https"
        port = 443
      }
    }
  }

  application_rule_collection {
    name     = "aks-external-apis"
    priority = 200
    action   = "Allow"

    rule {
      name              = "openai-api"
      source_addresses  = var.aks_node_cidr_blocks
      destination_fqdns = var.openai_fqdns

      protocols {
        type = "Https"
        port = 443
      }
    }

    rule {
      name              = "anthropic-api"
      source_addresses  = var.aks_node_cidr_blocks
      destination_fqdns = var.anthropic_fqdns

      protocols {
        type = "Https"
        port = 443
      }
    }

    rule {
      name              = "google-ai-api"
      source_addresses  = var.aks_node_cidr_blocks
      destination_fqdns = var.google_ai_fqdns

      protocols {
        type = "Https"
        port = 443
      }
    }
  }
}

# Network Rule Collection for AKS Egress
resource "azurerm_firewall_policy_rule_collection_group" "aks_network_egress" {
  name               = "${var.name}-aks-network-egress-${var.environment}"
  firewall_policy_id = azurerm_firewall_policy.main.id
  priority           = 200

  network_rule_collection {
    name     = "aks-required-ports"
    priority = 100
    action   = "Allow"

    rule {
      name                  = "aks-time-sync"
      source_addresses      = var.aks_node_cidr_blocks
      destination_addresses = ["*"]
      destination_ports     = ["123"]
      protocols             = ["UDP"]
    }

    rule {
      name                  = "aks-dns"
      source_addresses      = var.aks_node_cidr_blocks
      destination_addresses = var.dns_servers
      destination_ports     = ["53"]
      protocols             = ["UDP", "TCP"]
    }
  }

  network_rule_collection {
    name     = "aks-database-access"
    priority = 200
    action   = "Allow"

    rule {
      name                  = "aks-to-postgresql"
      source_addresses      = var.aks_node_cidr_blocks
      destination_addresses = var.database_addresses
      destination_ports     = ["5432"]
      protocols             = ["TCP"]
    }

    rule {
      name                  = "aks-to-redis"
      source_addresses      = var.aks_node_cidr_blocks
      destination_addresses = var.redis_addresses
      destination_ports     = ["6379"]
      protocols             = ["TCP"]
    }
  }
}

# NAT Rule Collection for Inbound Traffic
resource "azurerm_firewall_policy_rule_collection_group" "inbound_nat" {
  name               = "${var.name}-inbound-nat-${var.environment}"
  firewall_policy_id = azurerm_firewall_policy.main.id
  priority           = 300

  nat_rule_collection {
    name     = "api-ingress"
    priority = 100
    action   = "Dnat"

    rule {
      name                = "api-https"
      source_addresses    = var.allowed_inbound_cidrs
      destination_address = azurerm_public_ip.main.ip_address
      destination_ports   = ["443"]
      translated_address  = var.api_load_balancer_ip
      translated_port     = "443"
      protocols           = ["TCP"]
    }
  }
}

# Threat Intelligence Allowlist
resource "azurerm_firewall_policy_threat_intelligence_allowlist" "main" {
  name               = "${var.name}-ti-allowlist-${var.environment}"
  firewall_policy_id = azurerm_firewall_policy.main.id

  fqdns        = var.threat_intelligence_allowlist_fqdns
  ip_addresses = var.threat_intelligence_allowlist_ips
}

# Firewall Diagnostic Settings
resource "azurerm_monitor_diagnostic_setting" "firewall" {
  name                       = "${var.name}-fw-diag-${var.environment}"
  target_resource_id         = azurerm_firewall.main.id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  log {
    category = "AzureFirewallApplicationRule"
    enabled  = true

    retention_policy {
      days    = var.log_retention_days
      enabled = true
    }
  }

  log {
    category = "AzureFirewallNetworkRule"
    enabled  = true

    retention_policy {
      days    = var.log_retention_days
      enabled = true
    }
  }

  log {
    category = "AzureFirewallDnsProxy"
    enabled  = true

    retention_policy {
      days    = var.log_retention_days
      enabled = true
    }
  }

  metric {
    category = "AllMetrics"
    enabled  = true

    retention_policy {
      days    = var.log_retention_days
      enabled = true
    }
  }
}
