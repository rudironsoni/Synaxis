using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Synaxis.InferenceGateway.Infrastructure.Identity.Strategies.GitHub;

namespace Synaxis.InferenceGateway.Infrastructure.Tests.Identity.Strategies.GitHub
{
    public class GhConfigWriterTests : IDisposable
    {
        private readonly string _tempHome;
        private readonly string _origHome;

        public GhConfigWriterTests()
        {
            _origHome = Environment.GetEnvironmentVariable("HOME") ?? string.Empty;
            _tempHome = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempHome);
            Environment.SetEnvironmentVariable("HOME", _tempHome);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("HOME", _origHome);
            if (Directory.Exists(_tempHome))
            {
                Directory.Delete(_tempHome, recursive: true);
            }
        }

        private string GetConfigPath()
        {
            return Path.Combine(_tempHome, ".config", "gh", "hosts.yml");
        }

        [Fact]
        public async Task WriteTokenAsync_CreatesNewFile_WhenFileDoesNotExist()
        {
            var token = "ghp_testtoken123";
            await GhConfigWriter.WriteTokenAsync(token);

            var configPath = GetConfigPath();
            Assert.True(File.Exists(configPath));
            var content = await File.ReadAllTextAsync(configPath);
            Assert.Contains("github.com:", content);
            Assert.Contains($"oauth_token: {token}", content);
        }

        [Fact]
        public async Task WriteTokenAsync_WritesCustomUser_WhenUserSpecified()
        {
            var token = "ghp_testtoken456";
            var customUser = "custom-user";
            await GhConfigWriter.WriteTokenAsync(token, customUser);

            var content = await File.ReadAllTextAsync(GetConfigPath());
            Assert.Contains($"user: {customUser}", content);
        }

        [Fact]
        public async Task WriteTokenAsync_ReplacesExistingGithubBlock()
        {
            var oldToken = "ghp_oldtoken";
            var newToken = "ghp_newtoken";
            var configPath = GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            await File.WriteAllTextAsync(configPath, $@"github.com:
  user: old-user
  oauth_token: {oldToken}");

            await GhConfigWriter.WriteTokenAsync(newToken);

            var content = await File.ReadAllTextAsync(configPath);
            Assert.Contains($"oauth_token: {newToken}", content);
            Assert.DoesNotContain(oldToken, content);
        }

        [Fact]
        public async Task WriteTokenAsync_PreservesOtherHosts()
        {
            var githubToken = "ghp_githubtoken";
            var configPath = GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            await File.WriteAllTextAsync(configPath, @"enterprise.github.com:
  user: enterprise-user
  oauth_token: ghp_enterprisetoken");

            await GhConfigWriter.WriteTokenAsync(githubToken);

            var content = await File.ReadAllTextAsync(configPath);
            Assert.Contains("enterprise.github.com:", content);
            Assert.Contains("ghp_enterprisetoken", content);
            Assert.Contains($"oauth_token: {githubToken}", content);
        }

        [Fact]
        public async Task WriteTokenAsync_InitializesEmptyFile_WhenFileIsCorrupted()
        {
            var token = "ghp_testtoken789";
            var configPath = GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            await File.WriteAllTextAsync(configPath, "   ");

            await GhConfigWriter.WriteTokenAsync(token);

            var content = await File.ReadAllTextAsync(configPath);
            Assert.Contains("github.com:", content);
            Assert.Contains($"oauth_token: {token}", content);
        }

        [Fact]
        public async Task WriteTokenAsync_AppendsBlock_WhenNoExistingGithubBlock()
        {
            var token = "ghp_testtokenabc";
            var configPath = GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            await File.WriteAllTextAsync(configPath, @"gitlab.com:
  user: gitlab-user
  oauth_token: glp_token123");

            await GhConfigWriter.WriteTokenAsync(token);

            var content = await File.ReadAllTextAsync(configPath);
            Assert.Contains("gitlab.com:", content);
            Assert.Contains("github.com:", content);
            Assert.Contains($"oauth_token: {token}", content);
        }

        [Fact]
        public async Task WriteTokenAsync_HandlesMixedLineEndings()
        {
            var token = "ghp_testtokendef";
            var configPath = GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            await File.WriteAllTextAsync(configPath, "github.com:\r\n  user: test\r\n  oauth_token: old");

            await GhConfigWriter.WriteTokenAsync(token);

            var content = await File.ReadAllTextAsync(configPath);
            Assert.Contains($"oauth_token: {token}", content);
        }

        [Fact]
        public async Task WriteTokenAsync_CreatesDirectory_WhenNotExists()
        {
            var token = "ghp_testtokenghi";
            var configDir = Path.Combine(_tempHome, ".config", "gh");
            Assert.False(Directory.Exists(configDir));

            await GhConfigWriter.WriteTokenAsync(token);

            Assert.True(Directory.Exists(configDir));
            Assert.True(File.Exists(Path.Combine(configDir, "hosts.yml")));
        }

        [Fact]
        public async Task WriteTokenAsync_HandlesSpecialCharactersInToken()
        {
            var token = "ghp_special=token-with_underscores.and+dots";
            await GhConfigWriter.WriteTokenAsync(token);

            var content = await File.ReadAllTextAsync(GetConfigPath());
            Assert.Contains($"oauth_token: {token}", content);
        }

        [Fact]
        public async Task WriteTokenAsync_OverwritesExistingGithubConfiguration()
        {
            var oldUser = "old-user";
            var newUser = "new-user";
            var newToken = "ghp_newtokenxyz";
            var configPath = GetConfigPath();
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            await File.WriteAllTextAsync(configPath, $@"github.com:
  user: {oldUser}
  oauth_token: ghp_oldtoken");

            await GhConfigWriter.WriteTokenAsync(newToken, newUser);

            var content = await File.ReadAllTextAsync(configPath);
            Assert.Contains($"user: {newUser}", content);
            Assert.Contains($"oauth_token: {newToken}", content);
            Assert.DoesNotContain(oldUser, content);
        }
    }
}
