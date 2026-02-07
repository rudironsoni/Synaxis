
namespace Synaxis.InferenceGateway.Infrastructure.Tests.Identity.Strategies.GitHub
{
    public class GhConfigWriterTests : IDisposable
    {
        private readonly string _tempHome;
        private readonly string _origHome;
    using Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub;
    using System.IO;
    using System.Threading.Tasks;
    using System;
    using Xunit;

        public GhConfigWriterTests()
        {
            this._origHome = Environment.GetEnvironmentVariable("HOME") ?? string.Empty;
            this._tempHome = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(this._tempHome);
            Environment.SetEnvironmentVariable("HOME", this._tempHome);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("HOME", this._origHome);
            if (Directory.Exists(this._tempHome))
            {
                Directory.Delete(this._tempHome, recursive: true);
            }
        }

        private string GetConfigPath()
        {
            return Path.Combine(this._tempHome, ".config", "gh", "hosts.yml");
        }

        [Fact]
        public async Task WriteTokenAsync_CreatesNewFile_WhenFileDoesNotExist()
        {
            var token = "ghp_testtoken123";
            await GhConfigWriter.WriteTokenAsync(token).ConfigureAwait(false);

            var configPath = this.GetConfigPath();
            Assert.True(File.Exists(configPath));
            var content = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
            Assert.Contains("github.com:", content, StringComparison.Ordinal);
            Assert.Contains($"oauth_token: {token}", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task WriteTokenAsync_WritesCustomUser_WhenUserSpecified()
        {
            var token = "ghp_testtoken456";
            var customUser = "custom-user";
            await GhConfigWriter.WriteTokenAsync(token, customUser).ConfigureAwait(false);

            var content = await File.ReadAllTextAsync(this.GetConfigPath()).ConfigureAwait(false);
            Assert.Contains($"user: {customUser}", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task WriteTokenAsync_ReplacesExistingGithubBlock()
        {
            var oldToken = "ghp_oldtoken";
            var newToken = "ghp_newtoken";
            var configPath = this.GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            await File.WriteAllTextAsync(configPath, $@"github.com:
  user: old-user
  oauth_token: {oldToken}");

            await GhConfigWriter.WriteTokenAsync(newToken).ConfigureAwait(false);

            var content = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
            Assert.Contains($"oauth_token: {newToken}", content, StringComparison.Ordinal);
            Assert.DoesNotContain(oldToken, content);
        }

        [Fact]
        public async Task WriteTokenAsync_PreservesOtherHosts()
        {
            var githubToken = "ghp_githubtoken";
            var configPath = this.GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            await File.WriteAllTextAsync(configPath, @"enterprise.github.com:
  user: enterprise-user
  oauth_token: ghp_enterprisetoken");

            await GhConfigWriter.WriteTokenAsync(githubToken).ConfigureAwait(false);

            var content = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
            Assert.Contains("enterprise.github.com:", content, StringComparison.Ordinal);
            Assert.Contains("ghp_enterprisetoken", content, StringComparison.Ordinal);
            Assert.Contains($"oauth_token: {githubToken}", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task WriteTokenAsync_InitializesEmptyFile_WhenFileIsCorrupted()
        {
            var token = "ghp_testtoken789";
            var configPath = this.GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            await File.WriteAllTextAsync(configPath, "   ").ConfigureAwait(false);

            await GhConfigWriter.WriteTokenAsync(token).ConfigureAwait(false);

            var content = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
            Assert.Contains("github.com:", content, StringComparison.Ordinal);
            Assert.Contains($"oauth_token: {token}", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task WriteTokenAsync_AppendsBlock_WhenNoExistingGithubBlock()
        {
            var token = "ghp_testtokenabc";
            var configPath = this.GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            await File.WriteAllTextAsync(configPath, @"gitlab.com:
  user: gitlab-user
  oauth_token: glp_token123");

            await GhConfigWriter.WriteTokenAsync(token).ConfigureAwait(false);

            var content = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
            Assert.Contains("gitlab.com:", content, StringComparison.Ordinal);
            Assert.Contains("github.com:", content, StringComparison.Ordinal);
            Assert.Contains($"oauth_token: {token}", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task WriteTokenAsync_HandlesMixedLineEndings()
        {
            var token = "ghp_testtokendef";
            var configPath = this.GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            await File.WriteAllTextAsync(configPath, "github.com:\r\n  user: test\r\n  oauth_token: old").ConfigureAwait(false);

            await GhConfigWriter.WriteTokenAsync(token).ConfigureAwait(false);

            var content = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
            Assert.Contains($"oauth_token: {token}", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task WriteTokenAsync_CreatesDirectory_WhenNotExists()
        {
            var token = "ghp_testtokenghi";
            var configDir = Path.Combine(this._tempHome, ".config", "gh");
            Assert.False(Directory.Exists(configDir));

            await GhConfigWriter.WriteTokenAsync(token).ConfigureAwait(false);

            Assert.True(Directory.Exists(configDir));
            Assert.True(File.Exists(Path.Combine(configDir, "hosts.yml")));
        }

        [Fact]
        public async Task WriteTokenAsync_HandlesSpecialCharactersInToken()
        {
            var token = "ghp_special=token-with_underscores.and+dots";
            await GhConfigWriter.WriteTokenAsync(token).ConfigureAwait(false);

            var content = await File.ReadAllTextAsync(this.GetConfigPath()).ConfigureAwait(false);
            Assert.Contains($"oauth_token: {token}", content, StringComparison.Ordinal);
        }

        [Fact]
        public async Task WriteTokenAsync_OverwritesExistingGithubConfiguration()
        {
            var oldUser = "old-user";
            var newUser = "new-user";
            var newToken = "ghp_newtokenxyz";
            var configPath = this.GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            await File.WriteAllTextAsync(configPath, $@"github.com:
  user: {oldUser}
  oauth_token: ghp_oldtoken");

            await GhConfigWriter.WriteTokenAsync(newToken, newUser).ConfigureAwait(false);

            var content = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
            Assert.Contains($"user: {newUser}", content, StringComparison.Ordinal);
            Assert.Contains($"oauth_token: {newToken}", content, StringComparison.Ordinal);
            Assert.DoesNotContain(oldUser, content);
        }
    }
}
