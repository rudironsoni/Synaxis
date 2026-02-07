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

    public class FileTokenStore : ITokenStore
    {
        private readonly string _path;
        private readonly ILogger<FileTokenStore> _logger;

        public FileTokenStore(string path, ILogger<FileTokenStore> logger)
        {
            this._path = path ?? throw new ArgumentNullException(nameof(path));
            this._logger = logger;
        }

        public async Task<List<AntigravityAccount>> LoadAsync()
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

        public async Task SaveAsync(List<AntigravityAccount> accounts)
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
