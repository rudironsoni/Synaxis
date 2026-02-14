# Outputs for Azure Firewall Module

output "firewall_id" {
  description = "ID of the Azure Firewall"
  value       = azurerm_firewall.main.id
}

output "firewall_name" {
  description = "Name of the Azure Firewall"
  value       = azurerm_firewall.main.name
}

output "firewall_private_ip" {
  description = "Private IP address of the Azure Firewall"
  value       = azurerm_firewall.main.ip_configuration[0].private_ip_address
}

output "firewall_public_ip" {
  description = "Public IP address of the Azure Firewall"
  value       = azurerm_public_ip.main.ip_address
}

output "firewall_public_ip_id" {
  description = "ID of the public IP address"
  value       = azurerm_public_ip.main.id
}

output "firewall_policy_id" {
  description = "ID of the firewall policy"
  value       = azurerm_firewall_policy.main.id
}

output "firewall_policy_name" {
  description = "Name of the firewall policy"
  value       = azurerm_firewall_policy.main.name
}

output "diagnostic_settings_id" {
  description = "ID of the diagnostic settings"
  value       = azurerm_monitor_diagnostic_setting.firewall.id
}
