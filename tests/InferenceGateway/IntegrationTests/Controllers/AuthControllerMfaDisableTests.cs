// <copyright file="AuthControllerMfaDisableTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.IntegrationTests.Controllers;

using System;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Synaxis.Core.Models;
using Synaxis.Infrastructure.Data;
using Xunit;

/// <summary>
/// Tests for the MFA disable endpoint.
/// </summary>
[Collection("Integration")]
public class AuthControllerMfaDisableTests
{
    private readonly SynaxisWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerMfaDisableTests(SynaxisWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task MfaDisable_WithValidTotpCode_Returns204AndDisablesMfa()
    {
        // Arrange: Setup and enable MFA first
        var email = $"mfa_disable_totp_{Guid.NewGuid()}@example.com";
        var loginResponse = await _client.PostAsJsonAsync("/auth/dev-login", new { Email = email });
        loginResponse.EnsureSuccessStatusCode();
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginContent.GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var setupResponse = await _client.PostAsync("/api/auth/mfa/setup", null);
        setupResponse.EnsureSuccessStatusCode();
        var setupContent = await setupResponse.Content.ReadFromJsonAsync<JsonElement>();
        var secret = setupContent.GetProperty("secret").GetString();

        // Generate valid TOTP code
        var key = OtpNet.Base32Encoding.ToBytes(secret!);
        var totp = new OtpNet.Totp(key);
        var code = totp.ComputeTotp();

        await _client.PostAsJsonAsync("/api/auth/mfa/enable", new { code });

        // Act: Disable MFA with valid TOTP code
        var disableCode = totp.ComputeTotp();
        var response = await _client.PostAsJsonAsync("/api/auth/mfa/disable", new { code = disableCode });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify MFA is disabled in database
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
        var user = await dbContext.Users.FirstAsync(u => u.Email == email);
        user.MfaEnabled.Should().BeFalse();
        user.MfaSecret.Should().BeNull();
    }

    [Fact]
    public async Task MfaDisable_WithInvalidTotpCode_Returns400()
    {
        // Arrange: Enable MFA first
        var email = $"mfa_disable_invalid_{Guid.NewGuid()}@example.com";
        var loginResponse = await _client.PostAsJsonAsync("/auth/dev-login", new { Email = email });
        loginResponse.EnsureSuccessStatusCode();
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginContent.GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await _client.PostAsync("/api/auth/mfa/setup", null);
        var setupResponse = await _client.PostAsync("/api/auth/mfa/setup", null);
        var setupContent = await setupResponse.Content.ReadFromJsonAsync<JsonElement>();
        var secret = setupContent.GetProperty("secret").GetString();

        var key = OtpNet.Base32Encoding.ToBytes(secret!);
        var totp = new OtpNet.Totp(key);
        var enableCode = totp.ComputeTotp();
        await _client.PostAsJsonAsync("/api/auth/mfa/enable", new { code = enableCode });

        // Act: Try to disable with invalid code
        var response = await _client.PostAsJsonAsync("/api/auth/mfa/disable", new { code = "000000" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify MFA is still enabled
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
        var user = await dbContext.Users.FirstAsync(u => u.Email == email);
        user.MfaEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task MfaDisable_WithValidBackupCode_Returns204AndDisablesMfa()
    {
        // Arrange: Setup MFA with backup codes
        var email = $"mfa_disable_backup_{Guid.NewGuid()}@example.com";
        var loginResponse = await _client.PostAsJsonAsync("/auth/dev-login", new { Email = email });
        loginResponse.EnsureSuccessStatusCode();
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginContent.GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var setupResponse = await _client.PostAsync("/api/auth/mfa/setup", null);
        setupResponse.EnsureSuccessStatusCode();
        var setupContent = await setupResponse.Content.ReadFromJsonAsync<JsonElement>();
        var secret = setupContent.GetProperty("secret").GetString();

        var key = OtpNet.Base32Encoding.ToBytes(secret!);
        var totp = new OtpNet.Totp(key);
        var enableCode = totp.ComputeTotp();
        var enableResponse = await _client.PostAsJsonAsync("/api/auth/mfa/enable", new { code = enableCode });
        enableResponse.EnsureSuccessStatusCode();
        var enableContent = await enableResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Get backup codes from enable response
        var backupCodesElement = enableContent.GetProperty("backupCodes");
        var backupCodes = new string[backupCodesElement.GetArrayLength()];
        for (int i = 0; i < backupCodes.Length; i++)
        {
            backupCodes[i] = backupCodesElement[i].GetString() ?? string.Empty;
        }

        // Act: Disable MFA with backup code
        var response = await _client.PostAsJsonAsync("/api/auth/mfa/disable", new { code = backupCodes[0] });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify MFA is disabled and backup codes are cleared
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
        var updatedUser = await dbContext.Users.FirstAsync(u => u.Email == email);
        updatedUser.MfaEnabled.Should().BeFalse();
        updatedUser.MfaSecret.Should().BeNull();
        updatedUser.MfaBackupCodes.Should().BeNull();
    }

    [Fact]
    public async Task MfaDisable_WithUsedBackupCode_Returns400()
    {
        // Arrange: Setup MFA with backup codes and use one
        var email = $"mfa_disable_used_backup_{Guid.NewGuid()}@example.com";
        var loginResponse = await _client.PostAsJsonAsync("/auth/dev-login", new { Email = email });
        loginResponse.EnsureSuccessStatusCode();
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginContent.GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var setupResponse = await _client.PostAsync("/api/auth/mfa/setup", null);
        setupResponse.EnsureSuccessStatusCode();
        var setupContent = await setupResponse.Content.ReadFromJsonAsync<JsonElement>();
        var secret = setupContent.GetProperty("secret").GetString();

        var key = OtpNet.Base32Encoding.ToBytes(secret!);
        var totp = new OtpNet.Totp(key);
        var enableCode = totp.ComputeTotp();
        var enableResponse = await _client.PostAsJsonAsync("/api/auth/mfa/enable", new { code = enableCode });
        enableResponse.EnsureSuccessStatusCode();
        var enableContent = await enableResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Get backup codes from enable response
        var backupCodesElement = enableContent.GetProperty("backupCodes");
        var backupCodes = new string[backupCodesElement.GetArrayLength()];
        for (int i = 0; i < backupCodes.Length; i++)
        {
            backupCodes[i] = backupCodesElement[i].GetString() ?? string.Empty;
        }

        // Use the first backup code by removing it from the database
        var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynaxisDbContext>();
        var user = await dbContext.Users.FirstAsync(u => u.Email == email);
        var hashedBackupCodes = (user.MfaBackupCodes ?? string.Empty).Split(',');
        user.MfaBackupCodes = string.Join(",", hashedBackupCodes.Skip(1));
        await dbContext.SaveChangesAsync();

        // Act: Try to disable with already used backup code
        var response = await _client.PostAsJsonAsync("/api/auth/mfa/disable", new { code = backupCodes[0] });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify MFA is still enabled
        var updatedUser = await dbContext.Users.FirstAsync(u => u.Email == email);
        updatedUser.MfaEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task MfaDisable_WithoutAuthentication_Returns401()
    {
        // Arrange: Clear auth header
        _client.DefaultRequestHeaders.Authorization = null;

        // Act: Try to disable MFA without authentication
        var response = await _client.PostAsJsonAsync("/api/auth/mfa/disable", new { code = "123456" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MfaDisable_WhenMfaNotEnabled_Returns400()
    {
        // Arrange: Login without enabling MFA
        var email = $"mfa_disable_not_enabled_{Guid.NewGuid()}@example.com";
        var loginResponse = await _client.PostAsJsonAsync("/auth/dev-login", new { Email = email });
        loginResponse.EnsureSuccessStatusCode();
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginContent.GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act: Try to disable MFA when it's not enabled
        var response = await _client.PostAsJsonAsync("/api/auth/mfa/disable", new { code = "123456" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MfaDisable_WithEmptyCode_Returns400()
    {
        // Arrange: Enable MFA first
        var email = $"mfa_disable_empty_{Guid.NewGuid()}@example.com";
        var loginResponse = await _client.PostAsJsonAsync("/auth/dev-login", new { Email = email });
        loginResponse.EnsureSuccessStatusCode();
        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginContent.GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var setupResponse = await _client.PostAsync("/api/auth/mfa/setup", null);
        setupResponse.EnsureSuccessStatusCode();
        var setupContent = await setupResponse.Content.ReadFromJsonAsync<JsonElement>();
        var secret = setupContent.GetProperty("secret").GetString();

        var key = OtpNet.Base32Encoding.ToBytes(secret!);
        var totp = new OtpNet.Totp(key);
        var enableCode = totp.ComputeTotp();
        await _client.PostAsJsonAsync("/api/auth/mfa/enable", new { code = enableCode });

        // Act: Try to disable with empty code
        var response = await _client.PostAsJsonAsync("/api/auth/mfa/disable", new { code = string.Empty });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
