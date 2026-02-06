// <copyright file="ControlPlaneOptions.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.ControlPlane
{
    public sealed class ControlPlaneOptions
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string Region { get; set; } = "us";
        public bool UseInMemory { get; set; }
    }
}