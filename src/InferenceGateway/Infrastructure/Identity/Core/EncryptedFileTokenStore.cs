using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;

namespace Synaxis.InferenceGateway.Infrastructure.Identity.Core
{
    public class EncryptedFileTokenStore : ISecureTokenStore
    {
        private readonly string _path;
        private readonly IDataProtector _protector;

        public EncryptedFileTokenStore(IDataProtectionProvider provider, string path)
        {
            _protector = provider.CreateProtector("Synaxis.Identity.TokenStore.v1");
            _path = path;
        }

        public async Task<List<IdentityAccount>> LoadAsync()
        {
            if (!File.Exists(_path))
            {
                return new List<IdentityAccount>();
            }

            var encrypted = await File.ReadAllTextAsync(_path).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(encrypted))
                return new List<IdentityAccount>();

            try
            {
                var json = _protector.Unprotect(encrypted);
                var accounts = JsonSerializer.Deserialize<List<IdentityAccount>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
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
            var encrypted = _protector.Protect(json);
            var dir = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(_path, encrypted).ConfigureAwait(false);
        }
    }
}
