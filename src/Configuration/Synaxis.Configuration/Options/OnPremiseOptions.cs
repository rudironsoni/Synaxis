// <copyright file="OnPremiseOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Options;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration settings for on-premise infrastructure provider.
/// </summary>
public class OnPremiseOptions
{
    /// <summary>
    /// Gets or sets the server address or hostname.
    /// </summary>
    [Required(ErrorMessage = "Server address is required")]
    public string ServerAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port number for the server connection.
    /// </summary>
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    public int Port { get; set; } = 443;

    /// <summary>
    /// Gets or sets a value indicating whether to use TLS encryption.
    /// </summary>
    public bool UseTls { get; set; } = true;

    /// <summary>
    /// Gets or sets the path to the TLS certificate file.
    /// Required when UseTls is true.
    /// </summary>
    public string CertificatePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key for authentication.
    /// This is a sensitive value and should be stored securely.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data center or rack identifier.
    /// </summary>
    public string DataCenterId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to verify SSL certificates.
    /// </summary>
    public bool VerifySsl { get; set; } = true;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// </summary>
    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int TimeoutSeconds { get; set; } = 30;
}
