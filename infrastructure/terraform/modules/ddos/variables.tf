# Variables for DDoS Protection Module

variable "name" {
  description = "Name prefix for DDoS protection resources"
  type        = string
}

variable "environment" {
  description = "Environment name (e.g., dev, staging, prod)"
  type        = string
}

variable "location" {
  description = "Azure region for DDoS protection deployment"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}

variable "api_rate_limit_threshold" {
  description = "Rate limit threshold for API requests (requests per minute)"
  type        = number
  default     = 10000
}

variable "rate_limit_duration_minutes" {
  description = "Duration for rate limiting in minutes"
  type        = number
  default     = 1
}

variable "rate_limit_threshold" {
  description = "Rate limit threshold for WAF (requests per duration)"
  type        = number
  default     = 1000
}

variable "associate_with_vnet" {
  description = "Whether to associate DDoS protection plan with VNet"
  type        = bool
  default     = true
}

variable "virtual_network_id" {
  description = "ID of the virtual network to associate with DDoS protection"
  type        = string
  default     = null
}

variable "api_backend_host_header" {
  description = "Host header for API backend"
  type        = string
}

variable "api_backend_address" {
  description = "Address of the API backend"
  type        = string
}

variable "frontend_host_name" {
  description = "Frontend host name for Front Door"
  type        = string
}

variable "custom_https_enabled" {
  description = "Enable custom HTTPS provisioning for Front Door"
  type        = bool
  default     = false
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
