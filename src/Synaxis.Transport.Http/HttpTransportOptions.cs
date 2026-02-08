// <copyright file="HttpTransportOptions.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.Http
{
    /// <summary>
    /// Configuration options for HTTP transport layer.
    /// </summary>
    public class HttpTransportOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether authentication is required.
        /// </summary>
        public bool RequireAuthentication { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether streaming responses are enabled.
        /// </summary>
        public bool EnableStreaming { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum allowed request body size in bytes.
        /// </summary>
        public long MaxRequestBodySize { get; set; } = 10 * 1024 * 1024;
    }
}
