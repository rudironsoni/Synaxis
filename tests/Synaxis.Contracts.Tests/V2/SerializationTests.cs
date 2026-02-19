using System.Text.Json;
using FluentAssertions;
using Synaxis.Contracts.V2.Commands;
using Synaxis.Contracts.V2.Common;
using Synaxis.Contracts.V2.DomainEvents;
using Synaxis.Contracts.V2.DTOs;
using Synaxis.Contracts.V2.Queries;

namespace Synaxis.Contracts.Tests.V2;

public class SerializationTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    [Fact]
    public void UserCreated_ShouldRoundtrip()
    {
        var original = new UserCreated
        {
            AggregateId = Guid.NewGuid(),
            TenantId = "tenant-1",
            Email = "test@example.com",
            DisplayName = "Test User",
            Status = UserStatus.Active,
            IsAdmin = true,
            Metadata = new Dictionary<string, string> { ["department"] = "engineering" },
            CreatedAt = DateTimeOffset.UtcNow,
            Version = 2
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<UserCreated>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Email.Should().Be(original.Email);
        deserialized.DisplayName.Should().Be(original.DisplayName);
        deserialized.Status.Should().Be(original.Status);
        deserialized.IsAdmin.Should().BeTrue();
        deserialized.TenantId.Should().Be("tenant-1");
        deserialized.Metadata.Should().ContainKey("department");
    }

    [Fact]
    public void CreateUserCommand_ShouldRoundtrip()
    {
        var original = new CreateUserCommand
        {
            UserId = "admin",
            TenantId = "tenant-1",
            Email = "newuser@example.com",
            DisplayName = "New User",
            Password = "securepassword123",
            IsAdmin = false,
            Metadata = new Dictionary<string, string> { ["source"] = "api" }
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<CreateUserCommand>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Email.Should().Be(original.Email);
        deserialized.DisplayName.Should().Be(original.DisplayName);
        deserialized.IsAdmin.Should().BeFalse();
        deserialized.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void UpdateUserCommand_WithUpdateMask_ShouldRoundtrip()
    {
        var original = new UpdateUserCommand
        {
            UserId = "admin",
            TargetUserId = Guid.NewGuid(),
            UpdateMask = new[] { "displayName", "status" },
            DisplayName = "Updated Name",
            Status = "Active"
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<UpdateUserCommand>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.UpdateMask.Should().Contain("displayName");
        deserialized.UpdateMask.Should().Contain("status");
        deserialized.DisplayName.Should().Be("Updated Name");
    }

    [Fact]
    public void GetUsersQuery_WithCursor_ShouldRoundtrip()
    {
        var original = new GetUsersQuery
        {
            UserId = "admin",
            Cursor = "eyJpZCI6IDEyM30=",
            CursorDirection = "next",
            PageSize = 50,
            IsAdmin = true,
            IncludeMetadata = true
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<GetUsersQuery>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Cursor.Should().Be("eyJpZCI6IDEyM30=");
        deserialized.CursorDirection.Should().Be("next");
        deserialized.IsAdmin.Should().BeTrue();
        deserialized.IncludeMetadata.Should().BeTrue();
    }

    [Fact]
    public void UserDto_ShouldRoundtrip()
    {
        var original = new UserDto
        {
            Id = Guid.NewGuid(),
            TenantId = "tenant-1",
            Email = "user@example.com",
            DisplayName = "Test User",
            Status = UserStatus.Active,
            IsAdmin = false,
            Metadata = new Dictionary<string, string>(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<UserDto>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(original.Id);
        deserialized.TenantId.Should().Be("tenant-1");
        deserialized.IsAdmin.Should().BeFalse();
    }

    [Fact]
    public void AgentCreated_WithResources_ShouldRoundtrip()
    {
        var original = new AgentCreated
        {
            AggregateId = Guid.NewGuid(),
            TenantId = "tenant-1",
            Name = "Test Agent",
            Description = "A test agent",
            AgentType = "chat",
            Configuration = new Dictionary<string, object> { ["model"] = "gpt-4" },
            Resources = new global::Synaxis.Contracts.V2.DomainEvents.ResourceRequirements
            {
                Cpu = "100m",
                Memory = "512Mi"
            },
            Labels = new Dictionary<string, string> { ["env"] = "production" },
            CreatedByUserId = "admin",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<AgentCreated>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Resources.Should().NotBeNull();
        deserialized.Resources!.Cpu.Should().Be("100m");
        deserialized.Resources.Memory.Should().Be("512Mi");
        deserialized.Labels.Should().ContainKey("env");
    }

    [Fact]
    public void ExecuteAgentCommand_WithParentExecution_ShouldRoundtrip()
    {
        var original = new ExecuteAgentCommand
        {
            UserId = "admin",
            TenantId = "tenant-1",
            TargetAgentId = Guid.NewGuid(),
            ParentExecutionId = Guid.NewGuid(),
            WorkflowId = Guid.NewGuid(),
            Input = new Dictionary<string, object> { ["prompt"] = "Hello!" },
            Priority = 1,
            Resources = new global::Synaxis.Contracts.V2.Commands.ResourceRequirements { Cpu = "200m" }
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<ExecuteAgentCommand>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.ParentExecutionId.Should().NotBeNull();
        deserialized.WorkflowId.Should().NotBeNull();
        deserialized.Resources.Should().NotBeNull();
    }

    [Fact]
    public void PaginatedResult_WithCursors_ShouldRoundtrip()
    {
        var items = new List<UserDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = "user1@example.com",
                DisplayName = "User 1",
                Status = UserStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var original = new PaginatedResult<UserDto>
        {
            Items = items,
            Page = 2,
            PageSize = 10,
            TotalCount = 100,
            Cursor = "next-cursor",
            PreviousCursor = "prev-cursor"
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<PaginatedResult<UserDto>>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Cursor.Should().Be("next-cursor");
        deserialized.PreviousCursor.Should().Be("prev-cursor");
        deserialized.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void ErrorResponse_WithValidationErrorsDictionary_ShouldRoundtrip()
    {
        var original = new ErrorResponse
        {
            StatusCode = 400,
            Code = "VALIDATION_ERROR",
            Message = "Validation failed",
            RequestId = "req-123",
            Path = "/api/users",
            ValidationErrors = new Dictionary<string, IReadOnlyList<string>>
            {
                ["email"] = new[] { "Email is required", "Invalid format" }
            }
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<ErrorResponse>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.RequestId.Should().Be("req-123");
        deserialized.Path.Should().Be("/api/users");
        deserialized.ValidationErrors.Should().ContainKey("email");
    }

    [Theory]
    [InlineData(UserStatus.PendingVerification)]
    [InlineData(UserStatus.Active)]
    [InlineData(UserStatus.Suspended)]
    [InlineData(UserStatus.Inactive)]
    public void UserStatus_ShouldSerializeCorrectly(UserStatus status)
    {
        var json = JsonSerializer.Serialize(status, _options);
        var deserialized = JsonSerializer.Deserialize<UserStatus>(json, _options);

        deserialized.Should().Be(status);
    }

    [Theory]
    [InlineData(AgentStatus.Provisioning)]
    [InlineData(AgentStatus.Idle)]
    [InlineData(AgentStatus.Running)]
    [InlineData(AgentStatus.Processing)]
    [InlineData(AgentStatus.Draining)]
    [InlineData(AgentStatus.Error)]
    [InlineData(AgentStatus.Disabled)]
    public void AgentStatus_ShouldSerializeCorrectly(AgentStatus status)
    {
        var json = JsonSerializer.Serialize(status, _options);
        var deserialized = JsonSerializer.Deserialize<AgentStatus>(json, _options);

        deserialized.Should().Be(status);
    }

    [Theory]
    [InlineData(ExecutionStatus.Pending)]
    [InlineData(ExecutionStatus.Running)]
    [InlineData(ExecutionStatus.Paused)]
    [InlineData(ExecutionStatus.Completed)]
    [InlineData(ExecutionStatus.Failed)]
    [InlineData(ExecutionStatus.Cancelled)]
    [InlineData(ExecutionStatus.Timeout)]
    public void ExecutionStatus_ShouldSerializeCorrectly(ExecutionStatus status)
    {
        var json = JsonSerializer.Serialize(status, _options);
        var deserialized = JsonSerializer.Deserialize<ExecutionStatus>(json, _options);

        deserialized.Should().Be(status);
    }
}
