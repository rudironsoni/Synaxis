// <copyright file="RequestIdMiddlewareTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using Microsoft.AspNetCore.Http;
using Moq;
using Synaxis.InferenceGateway.WebApi.Middleware;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.Middleware;

public class RequestIdMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly RequestIdMiddleware _middleware;

    public RequestIdMiddlewareTests()
    {
        this._mockNext = new Mock<RequestDelegate>();
        this._middleware = new RequestIdMiddleware(this._mockNext.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithXRequestIdHeader_SetsResponseHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-request-id";
        this._mockNext.Setup(next => next(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await this._middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("test-request-id", context.Response.Headers["X-Request-ID"]);
        this._mockNext.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithLowercaseXRequestIdHeader_SetsResponseHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["x-request-id"] = "test-request-id-lowercase";
        this._mockNext.Setup(next => next(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await this._middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("test-request-id-lowercase", context.Response.Headers["X-Request-ID"]);
        this._mockNext.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithoutRequestIdHeader_UsesTraceIdentifier()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var traceId = context.TraceIdentifier;
        this._mockNext.Setup(next => next(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await this._middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(traceId, context.Response.Headers["X-Request-ID"]);
        this._mockNext.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithBothHeaders_UsesLastSetValue()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // ASP.NET Core headers are case-insensitive, so the second assignment overwrites the first
        context.Request.Headers["X-Request-ID"] = "uppercase-request-id";
        context.Request.Headers["x-request-id"] = "lowercase-request-id";
        this._mockNext.Setup(next => next(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await this._middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("lowercase-request-id", context.Response.Headers["X-Request-ID"]);
        this._mockNext.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_CallsNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        this._mockNext.Setup(next => next(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await this._middleware.InvokeAsync(context);

        // Assert
        this._mockNext.Verify(next => next(context), Times.Once);
    }
}
