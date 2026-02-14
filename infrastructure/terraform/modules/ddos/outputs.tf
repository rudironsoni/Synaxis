# Outputs for DDoS Protection Module

output "ddos_protection_plan_id" {
  description = "ID of the DDoS protection plan"
  value       = azurerm_network_ddos_protection_plan.main.id
}

output "ddos_protection_plan_name" {
  description = "Name of the DDoS protection plan"
  value       = azurerm_network_ddos_protection_plan.main.name
}

output "ddos_custom_policy_id" {
  description = "ID of the DDoS custom policy"
  value       = azurerm_ddos_custom_policy.main.id
}

output "ddos_custom_policy_name" {
  description = "Name of the DDoS custom policy"
  value       = azurerm_ddos_custom_policy.main.name
}

output "frontdoor_id" {
  description = "ID of the Front Door"
  value       = azurerm_frontdoor.main.id
}

output "frontdoor_name" {
  description = "Name of the Front Door"
  value       = azurerm_frontdoor.main.name
}

output "frontdoor_host_name" {
  description = "Frontend host name of the Front Door"
  value       = azurerm_frontdoor.main.frontend_endpoints[0].host_name
}

output "frontdoor_cname" {
  description = "CNAME of the Front Door"
  value       = azurerm_frontdoor.main.frontend_endpoints[0].cname
}

output "waf_policy_id" {
  description = "ID of the WAF policy"
  value       = azurerm_frontdoor_firewall_policy.main.id
}

output "waf_policy_name" {
  description = "Name of the WAF policy"
  value       = azurerm_frontdoor_firewall_policy.main.name
}

output "ddos_diagnostic_settings_id" {
  description = "ID of the DDoS diagnostic settings"
  value       = azurerm_monitor_diagnostic_setting.ddos.id
}

output "frontdoor_diagnostic_settings_id" {
  description = "ID of the Front Door diagnostic settings"
  value       = azurerm_monitor_diagnostic_setting.frontdoor.id
}
