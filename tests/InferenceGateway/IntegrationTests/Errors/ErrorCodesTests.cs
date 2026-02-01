using Synaxis.InferenceGateway.WebApi.Errors;
using Xunit;

namespace Synaxis.InferenceGateway.IntegrationTests.Errors;

public class ErrorCodesTests
{
    [Theory]
    [InlineData(ErrorCodes.InvalidRequestError, 400)]
    [InlineData(ErrorCodes.InvalidValue, 400)]
    [InlineData(ErrorCodes.AuthenticationError, 401)]
    [InlineData(ErrorCodes.AuthorizationError, 403)]
    [InlineData(ErrorCodes.NotFound, 404)]
    [InlineData(ErrorCodes.RateLimitExceeded, 429)]
    [InlineData(ErrorCodes.UpstreamRoutingFailure, 502)]
    [InlineData(ErrorCodes.ProviderError, 502)]
    [InlineData(ErrorCodes.ServiceUnavailable, 503)]
    [InlineData(ErrorCodes.InternalError, 500)]
    public void GetStatusCode_ReturnsCorrectStatusCode(string errorCode, int expectedStatusCode)
    {
        var statusCode = ErrorCodes.GetStatusCode(errorCode);
        Assert.Equal(expectedStatusCode, statusCode);
    }

    [Theory]
    [InlineData(ErrorCodes.InvalidRequestError, "invalid_request_error")]
    [InlineData(ErrorCodes.InvalidValue, "invalid_request_error")]
    [InlineData(ErrorCodes.AuthenticationError, "authentication_error")]
    [InlineData(ErrorCodes.AuthorizationError, "permission_error")]
    [InlineData(ErrorCodes.NotFound, "not_found_error")]
    [InlineData(ErrorCodes.RateLimitExceeded, "rate_limit_error")]
    [InlineData(ErrorCodes.UpstreamRoutingFailure, "api_error")]
    [InlineData(ErrorCodes.ProviderError, "api_error")]
    [InlineData(ErrorCodes.ServiceUnavailable, "api_error")]
    [InlineData(ErrorCodes.InternalError, "server_error")]
    public void GetErrorType_ReturnsCorrectErrorType(string errorCode, string expectedErrorType)
    {
        var errorType = ErrorCodes.GetErrorType(errorCode);
        Assert.Equal(expectedErrorType, errorType);
    }

    [Theory]
    [InlineData(ErrorCodes.InvalidRequestError)]
    [InlineData(ErrorCodes.InvalidValue)]
    [InlineData(ErrorCodes.AuthenticationError)]
    [InlineData(ErrorCodes.AuthorizationError)]
    [InlineData(ErrorCodes.NotFound)]
    [InlineData(ErrorCodes.RateLimitExceeded)]
    [InlineData(ErrorCodes.UpstreamRoutingFailure)]
    [InlineData(ErrorCodes.ProviderError)]
    [InlineData(ErrorCodes.ServiceUnavailable)]
    [InlineData(ErrorCodes.InternalError)]
    public void GetUserMessage_ReturnsNonEmptyMessage(string errorCode)
    {
        var message = ErrorCodes.GetUserMessage(errorCode);
        Assert.False(string.IsNullOrEmpty(message));
    }

    [Fact]
    public void GetStatusCode_UnknownErrorCode_Returns500()
    {
        var statusCode = ErrorCodes.GetStatusCode("unknown_error_code");
        Assert.Equal(500, statusCode);
    }

    [Fact]
    public void GetErrorType_UnknownErrorCode_ReturnsApiError()
    {
        var errorType = ErrorCodes.GetErrorType("unknown_error_code");
        Assert.Equal("api_error", errorType);
    }

    [Fact]
    public void GetUserMessage_UnknownErrorCode_ReturnsDefaultMessage()
    {
        var message = ErrorCodes.GetUserMessage("unknown_error_code");
        Assert.Equal("An unexpected error occurred.", message);
    }

    [Fact]
    public void InvalidRequestError_HasCorrectValue()
    {
        Assert.Equal("invalid_request_error", ErrorCodes.InvalidRequestError);
    }

    [Fact]
    public void InvalidValue_HasCorrectValue()
    {
        Assert.Equal("invalid_value", ErrorCodes.InvalidValue);
    }

    [Fact]
    public void UpstreamRoutingFailure_HasCorrectValue()
    {
        Assert.Equal("upstream_routing_failure", ErrorCodes.UpstreamRoutingFailure);
    }

    [Fact]
    public void ProviderError_HasCorrectValue()
    {
        Assert.Equal("provider_error", ErrorCodes.ProviderError);
    }

    [Fact]
    public void RateLimitExceeded_HasCorrectValue()
    {
        Assert.Equal("rate_limit_exceeded", ErrorCodes.RateLimitExceeded);
    }

    [Fact]
    public void AuthenticationError_HasCorrectValue()
    {
        Assert.Equal("authentication_error", ErrorCodes.AuthenticationError);
    }

    [Fact]
    public void AuthorizationError_HasCorrectValue()
    {
        Assert.Equal("authorization_error", ErrorCodes.AuthorizationError);
    }

    [Fact]
    public void NotFound_HasCorrectValue()
    {
        Assert.Equal("not_found", ErrorCodes.NotFound);
    }

    [Fact]
    public void ServiceUnavailable_HasCorrectValue()
    {
        Assert.Equal("service_unavailable", ErrorCodes.ServiceUnavailable);
    }

    [Fact]
    public void InternalError_HasCorrectValue()
    {
        Assert.Equal("internal_error", ErrorCodes.InternalError);
    }
}
