// <copyright file="EncryptedFileTokenStoreTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Identity
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.DataProtection;
    using Moq;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Core;
    using Xunit;

    public sealed class EncryptedFileTokenStoreTests : IDisposable
    {
        private readonly string _tmpPath;

        public EncryptedFileTokenStoreTests()
        {
            this._tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(this._tmpPath))
                {
                    File.Delete(this._tmpPath);
                }
            }
            catch
            {
            }
        }

        [Fact]
        public async Task SaveAsync_CallsProtectAndWritesFile()
        {
            var provider = new FakeDataProtectionProvider();
            var store = new EncryptedFileTokenStore(provider, this._tmpPath);

            var accounts = new List<IdentityAccount>
            {
                new IdentityAccount { Id = "1", Provider = "P", AccessToken = "t" },
            };

            await store.SaveAsync(accounts);

            // Reload using a new store with the same provider to verify roundtrip
            var loader = new EncryptedFileTokenStore(provider, this._tmpPath);
            var loaded = await loader.LoadAsync();
            Assert.NotNull(loaded);
            Assert.Single(loaded);
            Assert.Equal("1", loaded[0].Id);
        }

        [Fact]
        public async Task LoadAsync_CallsUnprotectAndReturnsAccounts()
        {
            var provider = new FakeDataProtectionProvider();

            var sample = System.Text.Json.JsonSerializer.Serialize(new List<IdentityAccount>
            {
                new IdentityAccount { Id = "1", Provider = "P", AccessToken = "t" },
            });
            var protector = provider.CreateProtector("Synaxis.Identity.TokenStore.v1");
            var protectedContent = protector.Protect(sample);
            await File.WriteAllTextAsync(this._tmpPath, protectedContent);

            var store = new EncryptedFileTokenStore(provider, this._tmpPath);

            var loaded = await store.LoadAsync();
            Assert.NotNull(loaded);
            Assert.Single(loaded);
            Assert.Equal("1", loaded[0].Id);
        }
    }
}
