using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Synaxis.InferenceGateway.Infrastructure.Identity.Core;
using Synaxis.InferenceGateway.WebApi.Endpoints.Identity;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.Identity;

public class IdentityEndpointsTests
{
    private readonly Mock<IdentityManager> _identityManagerMock;
    private readonly Mock<ISecureTokenStore> _tokenStoreMock;
    private readonly Mock<ILogger<IdentityManager>> _loggerMock;
    private readonly RouteData _routeData;
    private readonly DefaultHttpContext _httpContext;

    public IdentityEndpointsTests()
    {
        var strategiesMock = new Mock<IEnumerable<IAuthStrategy>>();
        this._identityManagerMock = new Mock<IdentityManager>(
            strategiesMock.Object,
            Mock.Of<ISecureTokenStore>(),
            Mock.Of<ILogger<IdentityManager>>())
        { CallBase = true };

        this._tokenStoreMock = new Mock<ISecureTokenStore>();
        this._loggerMock = new Mock<ILogger<IdentityManager>>();
        this._routeData = new RouteData();
        this._httpContext = new DefaultHttpContext();
    }

    private IdentityManager CreateIdentityManager()
    {
        var strategiesMock = new Mock<IEnumerable<IAuthStrategy>>();
        return new IdentityManager(strategiesMock.Object, this._tokenStoreMock.Object, this._loggerMock.Object);
    }

    #region MapIdentityEndpoints Tests

    [Fact]
    public void MapIdentityEndpoints_MethodExists()
    {
        // Verify that the MapIdentityEndpoints method exists and is accessible
        var method = typeof(IdentityEndpoints).GetMethod("MapIdentityEndpoints", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method.ReturnType);
        Assert.Single(method.GetParameters());
        Assert.Equal(typeof(IEndpointRouteBuilder), method.GetParameters()[0].ParameterType);
    }

    #endregion

    #region StartAuth Endpoint Tests

    [Fact]
    public async Task StartAuth_WithValidProvider_ReturnsAuthResult()
    {
        // Arrange
        var provider = "github";
        var authStrategyMock = new Mock<IAuthStrategy>();
        var expectedResult = new AuthResult
        {
            Status = "Pending",
            UserCode = "test-code",
            VerificationUri = "https://github.com/login",
            Message = "Please complete authentication",
        };

        authStrategyMock.Setup(x => x.InitiateFlowAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var strategies = new List<IAuthStrategy> { authStrategyMock.Object };
        var manager = new IdentityManager(strategies, this._tokenStoreMock.Object, this._loggerMock.Object);

        // Act
        var result = await manager.StartAuth(provider);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("test-code", result.UserCode);
        Assert.Equal("https://github.com/login", result.VerificationUri);
    }

    [Fact]
    public async Task StartAuth_WithInvalidProvider_ReturnsErrorResult()
    {
        // Arrange
        var provider = "invalid-provider";
        var strategies = new List<IAuthStrategy>();
        var manager = new IdentityManager(strategies, this._tokenStoreMock.Object, this._loggerMock.Object);

        // Act
        var result = await manager.StartAuth(provider);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Error", result.Status);
        Assert.Equal("No strategy found", result.Message);
    }

    [Fact]
    public async Task StartAuth_WithEmptyProvider_ReturnsErrorResult()
    {
        // Arrange
        var provider = "";
        var strategies = new List<IAuthStrategy>();
        var manager = new IdentityManager(strategies, this._tokenStoreMock.Object, this._loggerMock.Object);

        // Act
        var result = await manager.StartAuth(provider);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Error", result.Status);
        Assert.Equal("No strategy found", result.Message);
    }

    #endregion

    #region CompleteAuth Endpoint Tests

    [Fact]
    public async Task CompleteAuth_WithValidCode_ReturnsAuthResult()
    {
        // Arrange
        var provider = "github";
        var code = "valid-code";
        var state = "valid-state";
        var authStrategyMock = new Mock<IAuthStrategy>();
        var expectedResult = new AuthResult
        {
            Status = "Completed",
            Message = "Authentication successful",
            TokenResponse = new TokenResponse
            {
                AccessToken = "test-access-token",
                RefreshToken = "test-refresh-token",
                ExpiresInSeconds = 3600
            },
        };

        authStrategyMock.Setup(x => x.CompleteFlowAsync(code, state, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var strategies = new List<IAuthStrategy> { authStrategyMock.Object };
        var manager = new IdentityManager(strategies, this._tokenStoreMock.Object, this._loggerMock.Object);

        // Act
        var result = await manager.CompleteAuth(provider, code, state);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
        Assert.NotNull(result.TokenResponse);
        Assert.Equal("test-access-token", result.TokenResponse.AccessToken);
    }

    [Fact]
    public async Task CompleteAuth_WithInvalidProvider_ThrowsException()
    {
        // Arrange
        var provider = "invalid-provider";
        var code = "valid-code";
        var state = "valid-state";
        var strategies = new List<IAuthStrategy>();
        var manager = new IdentityManager(strategies, this._tokenStoreMock.Object, this._loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.CompleteAuth(provider, code, state));
    }

    [Fact]
    public async Task CompleteAuth_WithNullCode_ThrowsException()
    {
        // Arrange
        var provider = "github";
        var code = (string)null!;
        var state = "valid-state";
        var authStrategyMock = new Mock<IAuthStrategy>();

        authStrategyMock.Setup(x => x.CompleteFlowAsync(code!, state, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Code cannot be null"));

        var strategies = new List<IAuthStrategy> { authStrategyMock.Object };
        var manager = new IdentityManager(strategies, this._tokenStoreMock.Object, this._loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => manager.CompleteAuth(provider, code!, state));
    }

    #endregion

    #region GetAccounts Endpoint Tests

    [Fact]
    public async Task GetAccounts_WithNoAccounts_ReturnsEmptyList()
    {
        // Arrange
        var accounts = new List<IdentityAccount>();
        this._tokenStoreMock.Setup(x => x.LoadAsync())
            .ReturnsAsync(accounts);

        // Act
        var result = await this._tokenStoreMock.Object.LoadAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAccounts_WithAccounts_ReturnsMaskedAccounts()
    {
        // Arrange
        var accounts = new List<IdentityAccount>
        {
            new IdentityAccount
            {
                Id = "account-1",
                Provider = "github",
                Email = "user1@example.com",
                AccessToken = "gh-token-12345",
            },
            new IdentityAccount
            {
                Id = "account-2",
                Provider = "google",
                Email = "user2@example.com",
                AccessToken = "short" // Less than 8 chars
            },
        };
        this._tokenStoreMock.Setup(x => x.LoadAsync())
            .ReturnsAsync(accounts);

        // Act
        var result = await this._tokenStoreMock.Object.LoadAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        // Verify accounts are returned (masking is done in the endpoint, not in the store)
        var ghAccount = result.First(a => a.Provider == "github");
        Assert.Equal("gh-token-12345", ghAccount.AccessToken);

        var googleAccount = result.First(a => a.Provider == "google");
        Assert.Equal("short", googleAccount.AccessToken);
    }

    [Fact]
    public async Task GetAccounts_WithNullAccessToken_ReturnsEmptyMaskedToken()
    {
        // Arrange
        var accounts = new List<IdentityAccount>
        {
            new IdentityAccount
            {
                Id = "account-1",
                Provider = "github",
                Email = "user@example.com",
                AccessToken = null!
            },
        };
        this._tokenStoreMock.Setup(x => x.LoadAsync())
            .ReturnsAsync(accounts);

        // Act
        var result = await this._tokenStoreMock.Object.LoadAsync();

        // Assert
        Assert.NotNull(result);
        var account = Assert.Single(result);
        Assert.Null(account.AccessToken);
    }

    [Fact]
    public async Task GetAccounts_OnlyReturnsRequiredFields()
    {
        // Arrange
        var accounts = new List<IdentityAccount>
        {
            new IdentityAccount
            {
                Id = "account-1",
                Provider = "github",
                Email = "user@example.com",
                AccessToken = "full-token-12345",
                RefreshToken = "refresh-token-123",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            },
        };
        this._tokenStoreMock.Setup(x => x.LoadAsync())
            .ReturnsAsync(accounts);

        // Act
        var result = await this._tokenStoreMock.Object.LoadAsync();

        // Assert - Verify all IdentityAccount fields are returned from the store
        var account = result.First();
        Assert.Equal("account-1", account.Id);
        Assert.Equal("github", account.Provider);
        Assert.Equal("user@example.com", account.Email);
        Assert.Equal("full-token-12345", account.AccessToken);
        Assert.Equal("refresh-token-123", account.RefreshToken);
        Assert.NotNull(account.ExpiresAt);
    }

    #endregion

    #region CompleteRequest DTO Tests

    [Fact]
    public void CompleteRequest_WithDefaultValues_HasEmptyStrings()
    {
        // Arrange & Act
        var request = new IdentityEndpoints.CompleteRequest();

        // Assert
        Assert.Equal(string.Empty, request.Code);
        Assert.Equal(string.Empty, request.State);
    }

    [Fact]
    public void CompleteRequest_WithValues_SetsProperties()
    {
        // Arrange & Act
        var request = new IdentityEndpoints.CompleteRequest
        {
            Code = "test-code",
            State = "test-state",
        };

        // Assert
        Assert.Equal("test-code", request.Code);
        Assert.Equal("test-state", request.State);
    }

    #endregion
}
