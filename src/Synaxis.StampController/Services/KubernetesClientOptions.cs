// <copyright file="KubernetesClientOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.StampController.Services;

using Microsoft.Extensions.Options;

/// <summary>
/// Configuration options for the Kubernetes client.
/// </summary>
public class KubernetesClientOptions
{
    /// <summary>
    /// Gets or sets the Kubernetes namespace to operate in.
    /// </summary>
    public string? Namespace { get; set; }
}
