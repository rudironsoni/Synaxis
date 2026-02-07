// <copyright file="AesGcmTokenVault.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Security
{
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Options;
    using Synaxis.InferenceGateway.Application.Configuration;
    using Synaxis.InferenceGateway.Application.Security;
    using Synaxis.InferenceGateway.Infrastructure.ControlPlane;

    public sealed class AesGcmTokenVault : ITokenVault
    {
        private readonly ControlPlaneDbContext _dbContext;
        private readonly byte[] _masterKey;

        public AesGcmTokenVault(ControlPlaneDbContext dbContext, IOptions<SynaxisConfiguration> config)
        {
            this._dbContext = dbContext;
            // Derive a master key from a configured string. Do not allow an empty or missing master key.
            var masterKeyString = config.Value.MasterKey;
            if (string.IsNullOrWhiteSpace(masterKeyString))
            {
                throw new InvalidOperationException("Synaxis:InferenceGateway:MasterKey must be configured.");
            }

            using var sha = SHA256.Create();
            this._masterKey = sha.ComputeHash(Encoding.UTF8.GetBytes(masterKeyString));
        }

        public async Task<byte[]> EncryptAsync(Guid tenantId, string plaintext, CancellationToken cancellationToken = default)
        {
            var tenant = await this._dbContext.Tenants.FindAsync(new object[] { tenantId }, cancellationToken).ConfigureAwait(false);
            if (tenant == null)
            {
                throw new ArgumentException("Tenant not found", nameof(tenantId));
            }

            var tenantKey = this.DecryptTenantKey(tenant.EncryptedByokKey);
            return Encrypt(Encoding.UTF8.GetBytes(plaintext), tenantKey);
        }

        public async Task<string> DecryptAsync(Guid tenantId, byte[] ciphertext, CancellationToken cancellationToken = default)
        {
            var tenant = await this._dbContext.Tenants.FindAsync(new object[] { tenantId }, cancellationToken).ConfigureAwait(false);
            if (tenant == null)
            {
                throw new ArgumentException("Tenant not found", nameof(tenantId));
            }

            var tenantKey = this.DecryptTenantKey(tenant.EncryptedByokKey);
            var plainBytes = Decrypt(ciphertext, tenantKey);
            return Encoding.UTF8.GetString(plainBytes);
        }

        public async Task RotateKeyAsync(Guid tenantId, string newKeyBase64, CancellationToken cancellationToken = default)
        {
            var tenant = await this._dbContext.Tenants
                .Include(t => t.OAuthAccounts)
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken).ConfigureAwait(false);

            if (tenant == null)
            {
                throw new ArgumentException("Tenant not found", nameof(tenantId));
            }

            var oldKey = this.DecryptTenantKey(tenant.EncryptedByokKey);
            var newKey = Convert.FromBase64String(newKeyBase64);

            if (newKey.Length != 32)
            {
                throw new ArgumentException("Key must be 32 bytes", nameof(newKeyBase64));
            }

            // Re-encrypt all OAuth tokens
            foreach (var account in tenant.OAuthAccounts)
            {
                if (account.AccessTokenEncrypted.Length > 0)
                {
                    var token = Decrypt(account.AccessTokenEncrypted, oldKey);
                    account.AccessTokenEncrypted = Encrypt(token, newKey);
                }

                if (account.RefreshTokenEncrypted != null && account.RefreshTokenEncrypted.Length > 0)
                {
                    var token = Decrypt(account.RefreshTokenEncrypted, oldKey);
                    account.RefreshTokenEncrypted = Encrypt(token, newKey);
                }
            }

            // Update tenant key
            tenant.EncryptedByokKey = Encrypt(newKey, this._masterKey);
            await this._dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        private byte[] DecryptTenantKey(byte[] encryptedKey)
        {
            if (encryptedKey.Length == 0)
            {
                return this._masterKey; // Fallback for uninitialized tenants (MVP)
            }

            return Decrypt(encryptedKey, this._masterKey);
        }

        private static byte[] Encrypt(byte[] plaintext, byte[] key)
        {
            // Format: Nonce (12) + Tag (16) + Ciphertext (N)
            var nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);

            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];

            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

            return result;
        }

        private static byte[] Decrypt(byte[] encrypted, byte[] key)
        {
            if (encrypted.Length < 28)
            {
                throw new ArgumentException("Invalid ciphertext");
            }

            var nonce = encrypted.AsSpan(0, 12);
            var tag = encrypted.AsSpan(12, 16);
            var ciphertext = encrypted.AsSpan(28);

            var plaintext = new byte[ciphertext.Length];

            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return plaintext;
        }
    }
}
