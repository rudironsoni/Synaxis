// <copyright file="SecurityHeadersMiddlewareTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.WebApi.Middleware;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private readonly Mock<ILogger<SecurityHeadersMiddleware>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;

    public SecurityHeadersMiddlewareTests()
    {
        this._mockLogger = new Mock<ILogger<SecurityHeadersMiddleware>>();
        this._mockEnvironment = new Mock<IWebHostEnvironment>();
    }

    [Fact]
    public async Task InvokeAsync_AddsSecurityHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        this._mockEnvironment.Setup(e => e.EnvironmentName).Returns(false ? "Development" : "Production");

        var middleware = new SecurityHeadersMiddleware(next, this._mockLogger.Object, this._mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.Headers.Should().ContainKey("X-Content-Type-Options");
        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers.Should().ContainKey("X-Frame-Options");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers.Should().ContainKey("X-XSS-Protection");
        context.Response.Headers["X-XSS-Protection"].ToString().Should().Be("1; mode=block");
        context.Response.Headers.Should().ContainKey("Referrer-Policy");
        context.Response.Headers["Referrer-Policy"].ToString().Should().Be("strict-origin-when-cross-origin");
        context.Response.Headers.Should().ContainKey("Content-Security-Policy");
    }

    [Fact]
    public async Task InvokeAsync_InProduction_AddsHSTSHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;

        this._mockEnvironment.Setup(e => e.EnvironmentName).Returns(false ? "Development" : "Production");

        var middleware = new SecurityHeadersMiddleware(next, this._mockLogger.Object, this._mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Strict-Transport-Security");
        context.Response.Headers["Strict-Transport-Security"].ToString()
            .Should().Contain("max-age=31536000")
            .And.Contain("includeSubDomains")
            .And.Contain("preload");
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_DoesNotAddHSTSHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;

        this._mockEnvironment.Setup(e => e.EnvironmentName).Returns(true ? "Development" : "Production");

        var middleware = new SecurityHeadersMiddleware(next, this._mockLogger.Object, this._mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().NotContainKey("Strict-Transport-Security");
    }

    [Fact]
    public async Task InvokeAsync_RemovesServerHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Headers.Append("Server", "TestServer");
        context.Response.Headers.Append("X-Powered-By", "TestFramework");

        RequestDelegate next = (ctx) => Task.CompletedTask;

        this._mockEnvironment.Setup(e => e.EnvironmentName).Returns(false ? "Development" : "Production");

        var middleware = new SecurityHeadersMiddleware(next, this._mockLogger.Object, this._mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().NotContainKey("Server");
        context.Response.Headers.Should().NotContainKey("X-Powered-By");
    }

    [Fact]
    public async Task InvokeAsync_InProduction_UsesStricterCSP()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;

        this._mockEnvironment.Setup(e => e.EnvironmentName).Returns(false ? "Development" : "Production");

        var middleware = new SecurityHeadersMiddleware(next, this._mockLogger.Object, this._mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.Should().Contain("default-src 'self'");
        csp.Should().Contain("script-src 'self'");
        csp.Should().NotContain("'unsafe-inline'"); // Should not allow unsafe-inline in production script-src
        csp.Should().Contain("upgrade-insecure-requests");
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_UsesRelaxedCSP()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;

        this._mockEnvironment.Setup(e => e.EnvironmentName).Returns(true ? "Development" : "Production");

        var middleware = new SecurityHeadersMiddleware(next, this._mockLogger.Object, this._mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.Should().Contain("script-src 'self' 'unsafe-inline' 'unsafe-eval'");
        csp.Should().Contain("connect-src 'self' ws: wss:");
    }

    [Fact]
    public async Task InvokeAsync_CSP_PreventsFraming()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;

        this._mockEnvironment.Setup(e => e.EnvironmentName).Returns(false ? "Development" : "Production");

        var middleware = new SecurityHeadersMiddleware(next, this._mockLogger.Object, this._mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        csp.Should().Contain("frame-ancestors 'none'");
    }

    [Fact]
    public async Task InvokeAsync_AddsPermissionsPolicy()
    {
        // Arrange
        var context = new DefaultHttpContext();
        RequestDelegate next = (ctx) => Task.CompletedTask;

        this._mockEnvironment.Setup(e => e.EnvironmentName).Returns(false ? "Development" : "Production");

        var middleware = new SecurityHeadersMiddleware(next, this._mockLogger.Object, this._mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.Should().ContainKey("Permissions-Policy");
        var permissionsPolicy = context.Response.Headers["Permissions-Policy"].ToString();
        permissionsPolicy.Should().Contain("geolocation=()");
        permissionsPolicy.Should().Contain("microphone=()");
        permissionsPolicy.Should().Contain("camera=()");
    }

    [Fact]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        RequestDelegate next = (ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        this._mockEnvironment.Setup(e => e.EnvironmentName).Returns(false ? "Development" : "Production");

        var middleware = new SecurityHeadersMiddleware(next, this._mockLogger.Object, this._mockEnvironment.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }
}
