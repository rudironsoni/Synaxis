using System.Text.Json;
using Synaxis.InferenceGateway.Application.ControlPlane.Entities;
using Xunit;

namespace Synaxis.InferenceGateway.Application.Tests.ControlPlane;

public class EntityTests
{
    [Fact]
    public void ApiKey_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            KeyHash = "hashed-key-value",
            Name = "Test API Key",
            Status = ApiKeyStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        Assert.NotEqual(Guid.Empty, apiKey.Id);
        Assert.NotEqual(Guid.Empty, apiKey.ProjectId);
        Assert.Equal("hashed-key-value", apiKey.KeyHash);
        Assert.Equal("Test API Key", apiKey.Name);
        Assert.Equal(ApiKeyStatus.Active, apiKey.Status);
        Assert.True(apiKey.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ApiKey_Creation_SetsDefaultValues()
    {
        // Arrange & Act
        var apiKey = new ApiKey();

        // Assert
        Assert.Equal(Guid.Empty, apiKey.Id);
        Assert.Equal(Guid.Empty, apiKey.ProjectId);
        Assert.Equal(string.Empty, apiKey.KeyHash);
        Assert.Equal(string.Empty, apiKey.Name);
        Assert.Equal(default(ApiKeyStatus), apiKey.Status);
        Assert.True(apiKey.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ApiKey_Validation_ProjectNavigationIsOptional()
    {
        // Arrange
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
        };

        // Act & Assert
        Assert.Null(apiKey.Project);
        apiKey.Project = new Project();
        Assert.NotNull(apiKey.Project);
    }

    [Fact]
    public void AuditLog_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Action = "user.login",
            PayloadJson = "{\"ip\":\"127.0.0.1\"}",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        Assert.NotEqual(Guid.Empty, auditLog.Id);
        Assert.NotEqual(Guid.Empty, auditLog.TenantId);
        Assert.NotNull(auditLog.UserId);
        Assert.Equal("user.login", auditLog.Action);
        Assert.Equal("{\"ip\":\"127.0.0.1\"}", auditLog.PayloadJson);
        Assert.True(auditLog.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void AuditLog_Timestamp_IsSetAutomatically()
    {
        // Arrange & Act
        var auditLog = new AuditLog();

        // Assert
        Assert.True(auditLog.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(auditLog.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void AuditLog_Serialization_PayloadJsonCanBeDeserialized()
    {
        // Arrange
        var auditLog = new AuditLog
        {
            PayloadJson = "{\"userId\":\"123\",\"action\":\"login\"}",
        };

        // Act
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(auditLog.PayloadJson!);

        // Assert
        Assert.NotNull(payload);
        Assert.Equal("123", payload["userId"]);
        Assert.Equal("login", payload["action"]);
    }

    [Fact]
    public void GlobalModel_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var globalModel = new GlobalModel
        {
            Id = "llama-3.3-70b",
            Name = "Llama 3.3 70B",
            Family = "Llama",
            Description = "Large language model",
            ReleaseDate = new DateTime(2024, 1, 1),
            ContextWindow = 128000,
            MaxOutputTokens = 4096,
            InputPrice = 0.001m,
            OutputPrice = 0.002m,
            IsOpenWeights = true,
            SupportsTools = true,
            SupportsReasoning = false,
            SupportsVision = false,
            SupportsAudio = false,
            SupportsStructuredOutput = true,
        };

        // Assert
        Assert.Equal("llama-3.3-70b", globalModel.Id);
        Assert.Equal("Llama 3.3 70B", globalModel.Name);
        Assert.Equal("Llama", globalModel.Family);
        Assert.Equal("Large language model", globalModel.Description);
        Assert.Equal(new DateTime(2024, 1, 1), globalModel.ReleaseDate);
        Assert.Equal(128000, globalModel.ContextWindow);
        Assert.Equal(4096, globalModel.MaxOutputTokens);
        Assert.Equal(0.001m, globalModel.InputPrice);
        Assert.Equal(0.002m, globalModel.OutputPrice);
        Assert.True(globalModel.IsOpenWeights);
        Assert.True(globalModel.SupportsTools);
        Assert.False(globalModel.SupportsReasoning);
        Assert.False(globalModel.SupportsVision);
        Assert.False(globalModel.SupportsAudio);
        Assert.True(globalModel.SupportsStructuredOutput);
    }

    [Fact]
    public void GlobalModel_ProviderMapping_NavigationPropertyWorks()
    {
        // Arrange
        var globalModel = new GlobalModel();
        var providerModel = new ProviderModel
        {
            ProviderId = "nvidia",
            ProviderSpecificId = "nvidia/llama-3.3-70b",
        };

        // Act
        globalModel.ProviderModels.Add(providerModel);

        // Assert
        Assert.Single(globalModel.ProviderModels);
        Assert.Equal("nvidia", globalModel.ProviderModels[0].ProviderId);
    }

    [Fact]
    public void GlobalModel_Capabilities_CanBeCombined()
    {
        // Arrange
        var globalModel = new GlobalModel
        {
            SupportsTools = true,
            SupportsVision = true,
            SupportsStructuredOutput = true,
        };

        // Assert
        Assert.True(globalModel.SupportsTools);
        Assert.True(globalModel.SupportsVision);
        Assert.True(globalModel.SupportsStructuredOutput);
        Assert.False(globalModel.SupportsReasoning);
        Assert.False(globalModel.SupportsAudio);
    }

    [Fact]
    public void OAuthAccount_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var oauthAccount = new OAuthAccount
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Provider = "github",
            AccessTokenEncrypted = new byte[] { 1, 2, 3, 4, 5 },
            RefreshTokenEncrypted = new byte[] { 6, 7, 8, 9, 10 },
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            Status = OAuthAccountStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        Assert.NotEqual(Guid.Empty, oauthAccount.Id);
        Assert.NotEqual(Guid.Empty, oauthAccount.TenantId);
        Assert.Equal("github", oauthAccount.Provider);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, oauthAccount.AccessTokenEncrypted);
        Assert.Equal(new byte[] { 6, 7, 8, 9, 10 }, oauthAccount.RefreshTokenEncrypted);
        Assert.True(oauthAccount.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Equal(OAuthAccountStatus.Active, oauthAccount.Status);
        Assert.True(oauthAccount.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void OAuthAccount_TokenHandling_RefreshTokenIsOptional()
    {
        // Arrange
        var oauthAccount = new OAuthAccount
        {
            AccessTokenEncrypted = new byte[] { 1, 2, 3 },
        };

        // Assert
        Assert.NotNull(oauthAccount.AccessTokenEncrypted);
        Assert.Null(oauthAccount.RefreshTokenEncrypted);
    }

    [Fact]
    public void OAuthAccount_Expiration_CanBeNull()
    {
        // Arrange
        var oauthAccount = new OAuthAccount();

        // Assert
        Assert.Null(oauthAccount.ExpiresAt);
        oauthAccount.ExpiresAt = DateTimeOffset.UtcNow.AddDays(30);
        Assert.NotNull(oauthAccount.ExpiresAt);
    }

    [Fact]
    public void ModelCost_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var modelCost = new ModelCost
        {
            Provider = "openai",
            Model = "gpt-4",
            CostPerToken = 0.03m,
            FreeTier = false,
        };

        // Assert
        Assert.Equal("openai", modelCost.Provider);
        Assert.Equal("gpt-4", modelCost.Model);
        Assert.Equal(0.03m, modelCost.CostPerToken);
        Assert.False(modelCost.FreeTier);
    }

    [Fact]
    public void ModelCost_CostCalculation_CanBeUsedForBilling()
    {
        // Arrange
        var modelCost = new ModelCost
        {
            CostPerToken = 0.001m,
        };

        // Act
        var costFor1000Tokens = modelCost.CostPerToken * 1000;

        // Assert
        Assert.Equal(1.0m, costFor1000Tokens);
    }

    [Fact]
    public void ProviderModel_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var providerModel = new ProviderModel
        {
            Id = 1,
            ProviderId = "nvidia",
            GlobalModelId = "llama-3.3-70b",
            ProviderSpecificId = "nvidia/llama-3.3-70b",
            IsAvailable = true,
            OverrideInputPrice = 0.0005m,
            OverrideOutputPrice = 0.001m,
            RateLimitRPM = 1000,
            RateLimitTPM = 100000,
        };

        // Assert
        Assert.Equal(1, providerModel.Id);
        Assert.Equal("nvidia", providerModel.ProviderId);
        Assert.Equal("llama-3.3-70b", providerModel.GlobalModelId);
        Assert.Equal("nvidia/llama-3.3-70b", providerModel.ProviderSpecificId);
        Assert.True(providerModel.IsAvailable);
        Assert.Equal(0.0005m, providerModel.OverrideInputPrice);
        Assert.Equal(0.001m, providerModel.OverrideOutputPrice);
        Assert.Equal(1000, providerModel.RateLimitRPM);
        Assert.Equal(100000, providerModel.RateLimitTPM);
    }

    [Fact]
    public void ProviderModel_Availability_CanBeToggled()
    {
        // Arrange
        var providerModel = new ProviderModel { IsAvailable = true };

        // Act & Assert
        Assert.True(providerModel.IsAvailable);
        providerModel.IsAvailable = false;
        Assert.False(providerModel.IsAvailable);
    }

    [Fact]
    public void Tenant_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Test Tenant",
            Region = TenantRegion.Us,
            Status = TenantStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal("Test Tenant", tenant.Name);
        Assert.Equal(TenantRegion.Us, tenant.Region);
        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.True(tenant.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Tenant_Isolation_EachTenantHasUniqueId()
    {
        // Arrange
        var tenant1 = new Tenant { Id = Guid.NewGuid() };
        var tenant2 = new Tenant { Id = Guid.NewGuid() };

        // Assert
        Assert.NotEqual(tenant1.Id, tenant2.Id);
        Assert.NotEqual(Guid.Empty, tenant1.Id);
        Assert.NotEqual(Guid.Empty, tenant2.Id);
    }

    [Fact]
    public void Project_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Test Project",
            Status = ProjectStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.NotEqual(Guid.Empty, project.TenantId);
        Assert.Equal("Test Project", project.Name);
        Assert.Equal(ProjectStatus.Active, project.Status);
        Assert.True(project.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void TokenUsage_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var tokenUsage = new TokenUsage
        {
            Id = Guid.NewGuid(),
            RequestId = "req-123",
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            InputTokens = 1000,
            OutputTokens = 500,
            CostEstimate = 0.015m,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        Assert.NotEqual(Guid.Empty, tokenUsage.Id);
        Assert.Equal("req-123", tokenUsage.RequestId);
        Assert.NotEqual(Guid.Empty, tokenUsage.TenantId);
        Assert.NotEqual(Guid.Empty, tokenUsage.ProjectId);
        Assert.NotNull(tokenUsage.UserId);
        Assert.Equal(1000, tokenUsage.InputTokens);
        Assert.Equal(500, tokenUsage.OutputTokens);
        Assert.Equal(0.015m, tokenUsage.CostEstimate);
        Assert.True(tokenUsage.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void DeviationEntry_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var deviationEntry = new DeviationEntry
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Endpoint = "/v1/chat/completions",
            Field = "model",
            Reason = "Model not available",
            Mitigation = "Use fallback model",
            Status = DeviationStatus.Closed,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        Assert.NotEqual(Guid.Empty, deviationEntry.Id);
        Assert.NotEqual(Guid.Empty, deviationEntry.TenantId);
        Assert.Equal("/v1/chat/completions", deviationEntry.Endpoint);
        Assert.Equal("model", deviationEntry.Field);
        Assert.Equal("Model not available", deviationEntry.Reason);
        Assert.Equal("Use fallback model", deviationEntry.Mitigation);
        Assert.Equal(DeviationStatus.Closed, deviationEntry.Status);
        Assert.True(deviationEntry.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ModelAlias_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var modelAlias = new ModelAlias
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Alias = "fast-model",
            TargetModel = "llama-3.3-70b",
        };

        // Assert
        Assert.NotEqual(Guid.Empty, modelAlias.Id);
        Assert.NotEqual(Guid.Empty, modelAlias.TenantId);
        Assert.Equal("fast-model", modelAlias.Alias);
        Assert.Equal("llama-3.3-70b", modelAlias.TargetModel);
    }

    [Fact]
    public void ModelCombo_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var modelCombo = new ModelCombo
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Name = "Smart Combo",
            OrderedModelsJson = "[\"model1\",\"model2\"]",
        };

        // Assert
        Assert.NotEqual(Guid.Empty, modelCombo.Id);
        Assert.NotEqual(Guid.Empty, modelCombo.TenantId);
        Assert.Equal("Smart Combo", modelCombo.Name);
        Assert.Equal("[\"model1\",\"model2\"]", modelCombo.OrderedModelsJson);
    }

    [Fact]
    public void ProviderAccount_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var providerAccount = new ProviderAccount
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Provider = "openai",
            AccountId = "acc-123",
            Status = ProviderAccountStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        Assert.NotEqual(Guid.Empty, providerAccount.Id);
        Assert.NotEqual(Guid.Empty, providerAccount.TenantId);
        Assert.Equal("openai", providerAccount.Provider);
        Assert.Equal("acc-123", providerAccount.AccountId);
        Assert.Equal(ProviderAccountStatus.Active, providerAccount.Status);
        Assert.True(providerAccount.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RequestLog_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var requestLog = new RequestLog
        {
            Id = Guid.NewGuid(),
            RequestId = "req-123",
            TenantId = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Endpoint = "/v1/chat/completions",
            Model = "gpt-4",
            Provider = "openai",
            LatencyMs = 1500,
            StatusCode = 200,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        Assert.NotEqual(Guid.Empty, requestLog.Id);
        Assert.Equal("req-123", requestLog.RequestId);
        Assert.NotEqual(Guid.Empty, requestLog.TenantId);
        Assert.NotEqual(Guid.Empty, requestLog.ProjectId);
        Assert.NotNull(requestLog.UserId);
        Assert.Equal("/v1/chat/completions", requestLog.Endpoint);
        Assert.Equal("gpt-4", requestLog.Model);
        Assert.Equal("openai", requestLog.Provider);
        Assert.Equal(1500, requestLog.LatencyMs);
        Assert.Equal(200, requestLog.StatusCode);
        Assert.True(requestLog.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void QuotaSnapshot_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var quotaSnapshot = new QuotaSnapshot
        {
            Id = Guid.NewGuid(),
            Provider = "openai",
            AccountId = "acc-123",
            QuotaJson = "{\"tokens\":100000}",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        // Assert
        Assert.NotEqual(Guid.Empty, quotaSnapshot.Id);
        Assert.Equal("openai", quotaSnapshot.Provider);
        Assert.Equal("acc-123", quotaSnapshot.AccountId);
        Assert.Equal("{\"tokens\":100000}", quotaSnapshot.QuotaJson);
        Assert.True(quotaSnapshot.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void TenantModelLimit_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var tenantModelLimit = new TenantModelLimit
        {
            Id = 1,
            TenantId = "tenant-123",
            GlobalModelId = "gpt-4",
            AllowedRPM = 1000,
            MonthlyBudget = 1000.50m,
        };

        // Assert
        Assert.Equal(1, tenantModelLimit.Id);
        Assert.Equal("tenant-123", tenantModelLimit.TenantId);
        Assert.Equal("gpt-4", tenantModelLimit.GlobalModelId);
        Assert.Equal(1000, tenantModelLimit.AllowedRPM);
        Assert.Equal(1000.50m, tenantModelLimit.MonthlyBudget);
    }
}
