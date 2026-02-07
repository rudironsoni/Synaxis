// <copyright file="EncryptedFileTokenStore.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.DataProtection;

    public class EncryptedFileTokenStore : ISecureTokenStore
    {
        private readonly string _path;
        private readonly IDataProtector _protector;

        public EncryptedFileTokenStore(IDataProtectionProvider provider, string path)
        {
            this._protector = provider.CreateProtector("Synaxis.Identity.TokenStore.v1");
            this._path = path;
        }

        public async Task<List<IdentityAccount>> LoadAsync()
        {
            if (!File.Exists(this._path))
            {
                return new List<IdentityAccount>();
            }

            var encrypted = await File.ReadAllTextAsync(this._path).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(encrypted))
            {
                return new List<IdentityAccount>();
            }

            try
            {
                var json = this._protector.Unprotect(encrypted);
                var accounts = JsonSerializer.Deserialize<List<IdentityAccount>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

                return accounts ?? new List<IdentityAccount>();
            }
            catch
            {
                // If decryption or deserialization fails, return empty list
                return new List<IdentityAccount>();
            }
        }

        public async Task SaveAsync(List<IdentityAccount> accounts)
        {
            var json = JsonSerializer.Serialize(accounts);
            var encrypted = this._protector.Protect(json);
            var dir = Path.GetDirectoryName(this._path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(this._path, encrypted).ConfigureAwait(false);
        }
    }
}
