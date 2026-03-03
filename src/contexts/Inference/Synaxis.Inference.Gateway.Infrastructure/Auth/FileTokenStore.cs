// <copyright file="FileTokenStore.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Auth
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Synaxis.InferenceGateway.Application.Configuration;

    /// <summary>
    /// File-based implementation of ITokenStore for persisting Antigravity tokens.
    /// </summary>
    public class FileTokenStore : ITokenStore
    {
        private readonly string _path;
        private readonly ILogger<FileTokenStore> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileTokenStore"/> class.
        /// </summary>
        /// <param name="path">The file path for storing tokens.</param>
        /// <param name="logger">The logger instance.</param>
        public FileTokenStore(string path, ILogger<FileTokenStore> logger)
        {
            ArgumentNullException.ThrowIfNull(path);
            this._path = path;
            this._logger = logger;
        }

        /// <inheritdoc/>
        public async Task<IList<AntigravityAccount>> LoadAsync()
        {
            if (!File.Exists(this._path))
            {
                return new List<AntigravityAccount>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(this._path).ConfigureAwait(false);
                var accounts = JsonSerializer.Deserialize<List<AntigravityAccount>>(json);
                return accounts ?? new List<AntigravityAccount>();
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to load accounts from {Path}", this._path);
                return new List<AntigravityAccount>();
            }
        }

        /// <inheritdoc/>
        public async Task SaveAsync(IList<AntigravityAccount> accounts)
        {
            try
            {
                var dir = Path.GetDirectoryName(this._path);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(accounts);
                await File.WriteAllTextAsync(this._path, json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Failed to save accounts.");
            }
        }
    }
}
