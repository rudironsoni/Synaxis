# Azure DDoS Protection Module
# Provides DDoS protection with rate limiting and WAF integration

# DDoS Protection Plan
resource "azurerm_network_ddos_protection_plan" "main" {
  name                = "${var.name}-ddos-${var.environment}"
  location            = var.location
  resource_group_name = var.resource_group_name

  tags = var.tags
}

# DDoS Custom Policy
resource "azurerm_ddos_custom_policy" "main" {
  name                = "${var.name}-ddos-policy-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location

  # Rate limiting rules
  rate_limit_rule {
    name      = "synaxis-api-rate-limit"
    rate_limit_duration = 60
    rate_limit_threshold = var.api_rate_limit_threshold

    protocol = "Tcp"
    source   = "*"
    destination = "*"
    destination_port = "443"
  }

  rate_limit_rule {
    name      = "synaxis-api-rate-limit-http"
    rate_limit_duration = 60
    rate_limit_threshold = var.api_rate_limit_threshold

    protocol = "Tcp"
    source   = "*"
    destination = "*"
    destination_port = "80"
  }

  tags = var.tags
}

# DDoS Protection Profile for Front Door
resource "azurerm_frontdoor_firewall_policy" "main" {
  name                = "${var.name}-waf-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku_name            = "Premium_AzureFrontDoor"
  enabled             = true

  # Custom rules for rate limiting
  custom_rule {
    name     = "RateLimitRule"
    priority = 1
    action   = "Block"

    match_condition {
      match_variable     = "RemoteAddr"
      operator           = "IPMatch"
      negation_condition = false
      match_values       = ["*"]
    }

    rate_limit_duration_in_minutes = var.rate_limit_duration_minutes
    rate_limit_threshold           = var.rate_limit_threshold
  }

  # Managed rule sets
  managed_rule {
    type    = "DefaultRuleSet"
    version = "1.0"

    override {
      rule_group_name = "REQUEST-942-APPLICATION-ATTACK-SQLI"
      disabled_rules  = []
    }

    override {
      rule_group_name = "REQUEST-920-PROTOCOL-ENFORCEMENT"
      disabled_rules  = []
    }

    override {
      rule_group_name = "REQUEST-921-URI-ATTACK"
      disabled_rules  = []
    }
  }

  # Bot protection
  managed_rule {
    type    = "Microsoft_BotManagerRuleSet"
    version = "1.0"
  }

  tags = var.tags
}

# Front Door Profile
resource "azurerm_frontdoor" "main" {
  name                = "${var.name}-fd-${var.environment}"
  resource_group_name = var.resource_group_name
  location            = var.location

  # Routing rules
  routing_rule {
    name               = "synaxis-api-routing"
    accepted_protocols = ["Http", "Https"]
    patterns_to_match  = ["/*"]
    frontend_endpoints = ["synaxis-api-frontend"]

    forward_configuration {
      forwarding_protocol = "MatchRequest"
      backend_pool_name   = "synaxis-api-backend"
    }
  }

  # Backend pool
  backend_pool {
    name = "synaxis-api-backend"

    backend {
      host_header = var.api_backend_host_header
      address     = var.api_backend_address
      http_port   = 80
      https_port  = 443
      priority    = 1
      weight      = 100
    }

    load_balancing_name = "synaxis-load-balancing"
    health_probe_name   = "synaxis-health-probe"
  }

  # Load balancing settings
  load_balancing_settings {
    name = "synaxis-load-balancing"

    sample_size                 = 4
    successful_samples_required = 3
  }

  # Health probe
  health_probe {
    name                = "synaxis-health-probe"
    path                = "/health"
    protocol            = "Https"
    interval_in_seconds = 30
  }

  # Frontend endpoints
  frontend_endpoint {
    name                              = "synaxis-api-frontend"
    host_name                         = "${var.frontend_host_name}"
    custom_https_provisioning_enabled = var.custom_https_enabled
    session_affinity_enabled          = true
    session_affinity_ttl_seconds      = 300
  }

  # WAF policy association
  firewall_policy_id = azurerm_frontdoor_firewall_policy.main.id

  tags = var.tags
}

# DDoS Protection Plan Association with VNet
resource "azurerm_network_ddos_protection_plan_association" "main" {
  count = var.associate_with_vnet ? 1 : 0

  network_ddos_protection_plan_id = azurerm_network_ddos_protection_plan.main.id
  virtual_network_id              = var.virtual_network_id
}

# Diagnostic Settings for DDoS Protection Plan
resource "azurerm_monitor_diagnostic_setting" "ddos" {
  name                       = "${var.name}-ddos-diag-${var.environment}"
  target_resource_id         = azurerm_network_ddos_protection_plan.main.id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  log {
    category = "DDoSProtectionNotifications"
    enabled  = true

    retention_policy {
      days    = var.log_retention_days
      enabled = true
    }
  }

  log {
    category = "DDoSMitigationFlowLogs"
    enabled  = true

    retention_policy {
      days    = var.log_retention_days
      enabled = true
    }
  }

  log {
    category = "DDoSMitigationReports"
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

# Diagnostic Settings for Front Door
resource "azurerm_monitor_diagnostic_setting" "frontdoor" {
  name                       = "${var.name}-fd-diag-${var.environment}"
  target_resource_id         = azurerm_frontdoor.main.id
  log_analytics_workspace_id = var.log_analytics_workspace_id

  log {
    category = "FrontdoorAccessLog"
    enabled  = true

    retention_policy {
      days    = var.log_retention_days
      enabled = true
    }
  }

  log {
    category = "FrontdoorWebApplicationFirewallLog"
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
