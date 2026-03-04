// <copyright file="TestAuthHandler.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

#nullable enable

namespace Synaxis.Billing.IntegrationTests.Fixtures;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Authentication handler for integration tests that bypasses actual authentication.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<TestAuthOptions>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestAuthHandler"/> class.
    /// </summary>
    public TestAuthHandler(
        IOptionsMonitor<TestAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, Options.UserName ?? "testuser"),
            new("organization_id", Options.OrganizationId?.ToString() ?? Guid.NewGuid().ToString()),
        };

        foreach (var role in Options.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        var result = AuthenticateResult.Success(ticket);
        return Task.FromResult(result);
    }
}

/// <summary>
/// Options for configuring test authentication.
/// </summary>
public class TestAuthOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string? UserName { get; set; } = "testuser";

    /// <summary>
    /// Gets or sets the organization ID.
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// Gets or sets the roles for the test user.
    /// </summary>
    public List<string> Roles { get; set; } = new();
}
