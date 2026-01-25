using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Infrastructure.Auth;
using Xunit;

namespace Synaxis.InferenceGateway.Infrastructure.Tests;

public class AntigravityAuthManagerTests : IDisposable
{
    private readonly string _tempAuthPath;
    private readonly Mock<ILogger<AntigravityAuthManager>> _loggerMock;

    public AntigravityAuthManagerTests()
    {
        _tempAuthPath = Path.GetTempFileName();
        _loggerMock = new Mock<ILogger<AntigravityAuthManager>>();
    }

    public void Dispose()
    {
        if (File.Exists(_tempAuthPath)) File.Delete(_tempAuthPath);
    }

    [Fact]
    public async Task ListAccounts_ReturnsLoadedAccounts()
    {
        // Arrange
        var accounts = new List<AntigravityAccount>
        {
            new() { Email = "user1@test.com", Token = new() { AccessToken = "token1", ExpiresInSeconds = 3600, IssuedUtc = DateTime.UtcNow } },
            new() { Email = "user2@test.com", Token = new() { AccessToken = "token2", ExpiresInSeconds = 3600, IssuedUtc = DateTime.UtcNow } }
        };
        await File.WriteAllTextAsync(_tempAuthPath, System.Text.Json.JsonSerializer.Serialize(accounts));

        var manager = new AntigravityAuthManager("proj", _tempAuthPath, _loggerMock.Object);

        // Act
        // Force load by calling GetTokenAsync. 
        // Since tokens are valid, it should just return the first one and not hit Google.
        var token = await manager.GetTokenAsync(); 
        var list = manager.ListAccounts().ToList();

        // Assert
        // The manager implements round robin logic. 
        // We verify that accounts are loaded correctly from disk.
        Assert.Equal(2, list.Count);
        Assert.Contains(list, a => a.Email == "user1@test.com");
        Assert.Contains(list, a => a.Email == "user2@test.com");
    }

    [Fact]
    public async Task GetTokenAsync_InjectsEnvVarToken()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ANTIGRAVITY_REFRESH_TOKEN", "env-token");
        try 
        {
            // Empty file
            await File.WriteAllTextAsync(_tempAuthPath, "[]");
            var manager = new AntigravityAuthManager("proj", _tempAuthPath, _loggerMock.Object);

            // Act
            // GetTokenAsync will detect env var, inject it.
            // But the token is expired (ExpiresInSeconds=0, IssuedUtc=-1h).
            // So it will try to Refresh. 
            // This will throw because we can't refresh against real Google API without a valid refresh token and network.
            // So we expect an Exception, but we verify the account was added to the list.
            
            await Assert.ThrowsAnyAsync<Exception>(() => manager.GetTokenAsync());

            var list = manager.ListAccounts().ToList();
            Assert.Contains(list, a => a.Email == "env-var-user@system");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ANTIGRAVITY_REFRESH_TOKEN", null);
        }
    }
}
