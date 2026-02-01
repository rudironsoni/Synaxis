using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Synaxis.InferenceGateway.WebApi.Middleware;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.Middleware;

public class OpenAIErrorHandlerMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<OpenAIErrorHandlerMiddleware>> _mockLogger;
    private readonly OpenAIErrorHandlerMiddleware _middleware;

    public OpenAIErrorHandlerMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<OpenAIErrorHandlerMiddleware>>();
        _middleware = new OpenAIErrorHandlerMiddleware(_mockNext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InvokeAsync_NoException_CallsNextMiddleware()
    {
        // Arrange
        var context = CreateMockHttpContext();
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400WithInvalidRequestError()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var exception = new ArgumentException("Invalid parameter");
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var responseBody = await GetResponseBody(context);
        Assert.False(string.IsNullOrEmpty(responseBody), "Response body should not be empty");
        
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, _jsonOptions);
        
        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal("Invalid parameter", errorResponse.Error.Message);
        Assert.Equal("invalid_request_error", errorResponse.Error.Type);
        Assert.Equal("invalid_value", errorResponse.Error.Code);
    }

    [Fact]
    public async Task InvokeAsync_GenericException_Returns500WithServerError()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var exception = new InvalidOperationException("Something went wrong");
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var responseBody = await GetResponseBody(context);
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, _jsonOptions);
        
        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal("Something went wrong", errorResponse.Error.Message);
        Assert.Equal("server_error", errorResponse.Error.Type);
        Assert.Equal("internal_error", errorResponse.Error.Code);
    }

    [Fact]
    public async Task InvokeAsync_AggregateException_WithHttpRequestException_Returns502WithDetails()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var innerException = new HttpRequestException("Request failed", null, HttpStatusCode.TooManyRequests);
        var aggregateException = new AggregateException("Multiple failures", innerException);
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(aggregateException);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadGateway, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var responseBody = await GetResponseBody(context);
        var errorResponse = JsonSerializer.Deserialize<AggregateErrorResponse>(responseBody, _jsonOptions);
        
        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Error);
        Assert.Contains("Routing failed", errorResponse.Error.Message);
        Assert.Equal("upstream_routing_failure", errorResponse.Error.Code);
        Assert.NotNull(errorResponse.Error.Details);
        Assert.Single(errorResponse.Error.Details);
    }

    [Fact]
    public async Task InvokeAsync_AggregateException_WithMultipleExceptions_ReturnsAppropriateStatusCode()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var exceptions = new Exception[]
        {
            new HttpRequestException("Rate limited", null, HttpStatusCode.TooManyRequests),
            new HttpRequestException("Server error", null, HttpStatusCode.InternalServerError)
        };
        var aggregateException = new AggregateException("Multiple failures", exceptions);
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(aggregateException);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadGateway, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_AggregateException_WithClientErrorsOnly_Returns400()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var exceptions = new Exception[]
        {
            new HttpRequestException("Bad request", null, HttpStatusCode.BadRequest),
            new HttpRequestException("Not found", null, HttpStatusCode.NotFound)
        };
        var aggregateException = new AggregateException("Multiple failures", exceptions);
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(aggregateException);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ResponseHasStarted_ThrowsException()
    {
        // Arrange
        var context = CreateMockHttpContext();
        // Mock IHttpResponseFeature to simulate response has started
        var responseFeature = new Mock<IHttpResponseFeature>();
        responseFeature.SetupGet(f => f.HasStarted).Returns(true);
        context.Features.Set(responseFeature.Object);

        var exception = new InvalidOperationException("Should throw");
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task InvokeAsync_WithRequestId_IncludesRequestIdInError()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-request-id";
        var exception = new ArgumentException("Test error");
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        var responseBody = await GetResponseBody(context);
        Assert.Contains("Test error", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_StreamRequest_HandlesStreamingError()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Request.Headers["Accept"] = "text/event-stream";
        var exception = new InvalidOperationException("Stream error");
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Equal("text/event-stream", context.Response.ContentType);

        var responseBody = await GetResponseBody(context);
        Assert.Contains("data: {", responseBody);
        Assert.Contains("data: [DONE]", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_InvalidModelException_Returns404()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var exception = new ArgumentException("Model not found");
        exception.Data["ProviderName"] = "TestProvider";
        var aggregateException = new AggregateException("Model resolution failed", exception);
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(aggregateException);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        var responseBody = await GetResponseBody(context);
        Assert.Contains("Model not found", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_OpenAIFormat_ReturnsOpenAICompatibleError()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var exception = new ArgumentException("Invalid request parameters");
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        var responseBody = await GetResponseBody(context);
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, _jsonOptions);

        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal("Invalid request parameters", errorResponse.Error.Message);
        Assert.Equal("invalid_request_error", errorResponse.Error.Type);
        Assert.Equal("invalid_value", errorResponse.Error.Code);
        Assert.Null(errorResponse.Error.Param);
    }

    [Fact]
    public async Task InvokeAsync_WithRequestId_IncludesRequestIdInErrorResponse()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-request-id-123";
        var exception = new ArgumentException("Test error");
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        var responseBody = await GetResponseBody(context);
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, _jsonOptions);

        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Error);
        Assert.Equal("test-request-id-123", errorResponse.Error.RequestId);
    }

    [Fact]
    public async Task InvokeAsync_WithoutRequestId_GeneratesRequestIdFromTraceIdentifier()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var exception = new ArgumentException("Test error");
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        var responseBody = await GetResponseBody(context);
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, _jsonOptions);

        Assert.NotNull(errorResponse);
        Assert.NotNull(errorResponse.Error);
        Assert.NotNull(errorResponse.Error.RequestId);
        Assert.Equal(context.TraceIdentifier, errorResponse.Error.RequestId);
    }

    [Fact]
    public async Task InvokeAsync_LogsStructuredErrorWithRequestId()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Request.Headers["X-Request-ID"] = "test-request-id-456";
        var exception = new InvalidOperationException("Test error for logging");
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("test-request-id-456")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InvokeAsync_StreamingRequest_ReturnsSSEFormatError()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Request.Headers["Accept"] = "text/event-stream";
        var exception = new InvalidOperationException("Stream error");
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Equal("text/event-stream", context.Response.ContentType);

        var responseBody = await GetResponseBody(context);
        Assert.Contains("data: {", responseBody);
        Assert.Contains("data: [DONE]", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_StreamingRequestWithStreamQuery_ReturnsSSEFormatError()
    {
        // Arrange
        var context = CreateMockHttpContext();
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "stream", "true" }
        });
        var exception = new InvalidOperationException("Stream error");
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
        Assert.Equal("text/event-stream", context.Response.ContentType);

        var responseBody = await GetResponseBody(context);
        Assert.Contains("data: {", responseBody);
        Assert.Contains("data: [DONE]", responseBody);
    }

    [Fact]
    public async Task InvokeAsync_AggregateException_LogsStructuredErrorWithInnerExceptionCount()
    {
        // Arrange
        var context = CreateMockHttpContext();
        var exceptions = new Exception[]
        {
            new HttpRequestException("Error 1", null, HttpStatusCode.BadRequest),
            new HttpRequestException("Error 2", null, HttpStatusCode.NotFound)
        };
        var aggregateException = new AggregateException("Multiple failures", exceptions);
        _mockNext.Setup(next => next(It.IsAny<HttpContext>())).ThrowsAsync(aggregateException);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Inner exceptions: 2")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private HttpContext CreateMockHttpContext()
    {
        var context = new DefaultHttpContext();

        // Set up the response body
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        // Initialize response headers by accessing them (this creates the dictionary)
        context.Response.Headers.ContentType = "application/json";

        return context;
    }

    private async Task<string> GetResponseBody(HttpContext context)
    {
        if (context.Response.Body is MemoryStream memoryStream)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(memoryStream);
            return await reader.ReadToEndAsync();
        }
        return string.Empty;
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    // Helper classes for deserialization
    private class ErrorResponse
    {
        public ErrorDetail? Error { get; set; }
    }

    private class AggregateErrorResponse
    {
        public AggregateErrorDetail? Error { get; set; }
    }

    private class ErrorDetail
    {
        public string? Message { get; set; }
        public string? Type { get; set; }
        public string? Param { get; set; }
        public string? Code { get; set; }
        public string? RequestId { get; set; }
    }

    private class AggregateErrorDetail
    {
        public string? Message { get; set; }
        public string? Code { get; set; }
        public List<object>? Details { get; set; }
        public string? RequestId { get; set; }
    }
}