# Variables for Azure Firewall Module

variable "name" {
  description = "Name prefix for firewall resources"
  type        = string
}

variable "environment" {
  description = "Environment name (e.g., dev, staging, prod)"
  type        = string
}

variable "location" {
  description = "Azure region for firewall deployment"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "firewall_subnet_id" {
  description = "ID of the AzureFirewallSubnet"
  type        = string
}

variable "zones" {
  description = "Availability zones for firewall deployment"
  type        = list(string)
  default     = ["1", "2", "3"]
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}

variable "dns_servers" {
  description = "DNS servers for firewall DNS proxy"
  type        = list(string)
  default     = ["168.63.129.16"] # Azure DNS
}

variable "idps_mode" {
  description = "Intrusion Detection and Prevention System mode"
  type        = string
  default     = "Alert"
  validation {
    condition     = contains(["Off", "Alert", "Deny"], var.idps_mode)
    error_message = "IDPS mode must be Off, Alert, or Deny."
  }
}

variable "tls_inspection_enabled" {
  description = "Enable TLS inspection for outbound traffic"
  type        = bool
  default     = false
}

variable "aks_node_cidr_blocks" {
  description = "CIDR blocks of AKS node subnets"
  type        = list(string)
}

variable "aks_required_fqdns" {
  description = "FQDNs required for AKS operation"
  type        = list(string)
  default = [
    "*.azmk8s.io",
    "*.azurecr.io",
    "*.blob.core.windows.net",
    "*.azureedge.net",
    "*.azurefd.net",
    "*.azurewebsites.net",
    "*.microsoftonline.com",
    "*.msauth.net",
    "*.msftauth.net",
    "*.windows.net",
    "management.azure.com",
    "login.microsoftonline.com",
    "packages.microsoft.com",
    "acs-mirror.azureedge.net"
  ]
}

variable "container_registry_fqdns" {
  description = "FQDNs for container registries"
  type        = list(string)
  default     = ["*.azurecr.io", "*.docker.io", "*.gcr.io", "*.ghcr.io"]
}

variable "monitoring_fqdns" {
  description = "FQDNs for monitoring services"
  type        = list(string)
  default = [
    "*.monitoring.azure.com",
    "*.oms.opinsights.azure.com",
    "*.applicationinsights.azure.com",
    "*.dc.services.visualstudio.com"
  ]
}

variable "azure_services_fqdns" {
  description = "FQDNs for Azure services"
  type        = list(string)
  default = [
    "*.azure.com",
    "*.azure.net",
    "*.azure.us",
    "*.azure.cn",
    "*.azure.microsoft.com",
    "*.microsoft.com",
    "*.msauth.net",
    "*.msftauth.net",
    "*.windows.net"
  ]
}

variable "openai_fqdns" {
  description = "FQDNs for OpenAI API"
  type        = list(string)
  default     = ["api.openai.com", "*.openai.com"]
}

variable "anthropic_fqdns" {
  description = "FQDNs for Anthropic API"
  type        = list(string)
  default     = ["api.anthropic.com", "*.anthropic.com"]
}

variable "google_ai_fqdns" {
  description = "FQDNs for Google AI API"
  type        = list(string)
  default     = ["generativelanguage.googleapis.com", "*.googleapis.com"]
}

variable "database_addresses" {
  description = "IP addresses of database servers"
  type        = list(string)
  default     = []
}

variable "redis_addresses" {
  description = "IP addresses of Redis servers"
  type        = list(string)
  default     = []
}

variable "allowed_inbound_cidrs" {
  description = "CIDR blocks allowed for inbound traffic"
  type        = list(string)
  default     = ["0.0.0.0/0"]
}

variable "api_load_balancer_ip" {
  description = "IP address of the API load balancer"
  type        = string
}

variable "threat_intelligence_allowlist_fqdns" {
  description = "FQDNs to allowlist in threat intelligence"
  type        = list(string)
  default     = []
}

variable "threat_intelligence_allowlist_ips" {
  description = "IP addresses to allowlist in threat intelligence"
  type        = list(string)
  default     = []
}

variable "log_analytics_workspace_id" {
  description = "ID of the Log Analytics workspace for diagnostics"
  type        = string
}

variable "log_retention_days" {
  description = "Number of days to retain logs"
  type        = number
  default     = 90
}
