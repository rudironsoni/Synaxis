// <copyright file="SignalRAdapterOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Adapters.SignalR
{
    /// <summary>
    /// Configuration options for the SignalR adapter.
    /// </summary>
    public sealed class SignalRAdapterOptions
    {
        /// <summary>
        /// Gets or sets the base path for SignalR hubs. Default is "/synaxis".
        /// </summary>
        public string Path { get; set; } = "/synaxis";

        /// <summary>
        /// Gets or sets a value indicating whether to enable detailed error messages. Default is false.
        /// </summary>
        public bool EnableDetailedErrors { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum size of incoming messages in bytes. Default is 1 MB.
        /// </summary>
        public int MaximumReceiveMessageSize { get; set; } = 1024 * 1024;
    }
}
