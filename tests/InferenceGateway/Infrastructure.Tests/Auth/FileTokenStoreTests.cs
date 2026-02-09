// <copyright file="FileTokenStoreTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Auth
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Synaxis.InferenceGateway.Infrastructure.Auth;
    using Xunit;

    public class FileTokenStoreTests : IDisposable
    {
        private readonly string _tmpPath;
        private readonly string _tmpDir;

        public FileTokenStoreTests()
        {
            this._tmpDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            this._tmpPath = Path.Combine(this._tmpDir, "tokens.json");
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(this._tmpPath))
                {
                    File.Delete(this._tmpPath);
                }

                if (Directory.Exists(this._tmpDir))
                {
                    Directory.Delete(this._tmpDir, true);
                }
            }
            catch
            {
            }
        }

        [Fact]
        public async Task SaveAsync_WritesSerializedAccountsToFile()
        {
            var logger = Mock.Of<ILogger<FileTokenStore>>();
            var store = new FileTokenStore(this._tmpPath, logger);

            var accounts = new List<AntigravityAccount>
            {
                new AntigravityAccount { Email = "a@x.com", ProjectId = "p1", Token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse { AccessToken = "t1" } },
            };

            await store.SaveAsync(accounts);

            Assert.True(File.Exists(this._tmpPath));
            var content = await File.ReadAllTextAsync(this._tmpPath);
            var deserialized = JsonSerializer.Deserialize<List<AntigravityAccount>>(content);
            Assert.NotNull(deserialized);
            Assert.Single(deserialized);
            Assert.Equal("a@x.com", deserialized[0].Email);
        }

        [Fact]
        public async Task LoadAsync_LoadsAndDeserializesAccounts()
        {
            var logger = Mock.Of<ILogger<FileTokenStore>>();

            Directory.CreateDirectory(this._tmpDir);
            var accounts = new List<AntigravityAccount>
            {
                new AntigravityAccount { Email = "b@x.com", ProjectId = "p2", Token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse { AccessToken = "t2" } },
            };
            var json = JsonSerializer.Serialize(accounts);
            await File.WriteAllTextAsync(this._tmpPath, json);

            var store = new FileTokenStore(this._tmpPath, logger);
            var loaded = await store.LoadAsync();

            Assert.NotNull(loaded);
            Assert.Single(loaded);
            Assert.Equal("b@x.com", loaded[0].Email);
        }

        [Fact]
        public async Task LoadAsync_ReturnsEmptyList_WhenFileDoesNotExist()
        {
            var logger = Mock.Of<ILogger<FileTokenStore>>();

            // Ensure no file
            if (File.Exists(this._tmpPath))
            {
                File.Delete(this._tmpPath);
            }

            var store = new FileTokenStore(this._tmpPath, logger);
            var loaded = await store.LoadAsync();
            Assert.NotNull(loaded);
            Assert.Empty(loaded);
        }

        [Fact]
        public async Task LoadAsync_ReturnsEmptyList_OnException()
        {
            var loggerMock = new Mock<ILogger<FileTokenStore>>();

            // Write invalid JSON to force JsonSerializer.Deserialize to throw
            Directory.CreateDirectory(this._tmpDir);
            await File.WriteAllTextAsync(this._tmpPath, "{ invalid json");

            var store = new FileTokenStore(this._tmpPath, loggerMock.Object);
            var loaded = await store.LoadAsync();

            Assert.NotNull(loaded);
            Assert.Empty(loaded);

            // Don't verify LogError via extension method (not overridable). Just ensure method handles exception and returns empty list.
        }

        [Fact]
        public async Task SaveAsync_CreatesDirectory_IfNotExists()
        {
            var logger = Mock.Of<ILogger<FileTokenStore>>();

            // Ensure directory doesn't exist
            if (Directory.Exists(this._tmpDir))
            {
                Directory.Delete(this._tmpDir, true);
            }

            var store = new FileTokenStore(this._tmpPath, logger);
            var accounts = new List<AntigravityAccount>();
            await store.SaveAsync(accounts);

            Assert.True(Directory.Exists(this._tmpDir));
            Assert.True(File.Exists(this._tmpPath));
            var content = await File.ReadAllTextAsync(this._tmpPath);

            // empty list serializes to []
            Assert.Equal("[]", content);
        }

        [Fact]
        public async Task SaveAsync_And_LoadAsync_HandleEmptyList()
        {
            var logger = Mock.Of<ILogger<FileTokenStore>>();
            var store = new FileTokenStore(this._tmpPath, logger);

            var accounts = new List<AntigravityAccount>();
            await store.SaveAsync(accounts);

            var loaded = await store.LoadAsync();
            Assert.NotNull(loaded);
            Assert.Empty(loaded);
        }
    }
}
