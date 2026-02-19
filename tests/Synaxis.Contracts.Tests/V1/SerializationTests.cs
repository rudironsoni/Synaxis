using System.Text.Json;
using FluentAssertions;
using Synaxis.Contracts.V1.Commands;
using Synaxis.Contracts.V1.Common;
using Synaxis.Contracts.V1.DomainEvents;
using Synaxis.Contracts.V1.DTOs;
using Synaxis.Contracts.V1.Queries;

namespace Synaxis.Contracts.Tests.V1;

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
            Email = "test@example.com",
            DisplayName = "Test User",
            Status = UserStatus.Active,
            Roles = new[] { "user", "admin" },
            CreatedAt = DateTimeOffset.UtcNow,
            Version = 1
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<UserCreated>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Email.Should().Be(original.Email);
        deserialized.DisplayName.Should().Be(original.DisplayName);
        deserialized.Status.Should().Be(original.Status);
        deserialized.Roles.Should().BeEquivalentTo(original.Roles);
    }

    [Fact]
    public void CreateUserCommand_ShouldRoundtrip()
    {
        var original = new CreateUserCommand
        {
            UserId = "admin",
            Email = "newuser@example.com",
            DisplayName = "New User",
            Password = "securepassword123",
            Roles = new[] { "user" }
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<CreateUserCommand>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Email.Should().Be(original.Email);
        deserialized.DisplayName.Should().Be(original.DisplayName);
        deserialized.Password.Should().Be(original.Password);
        deserialized.Roles.Should().BeEquivalentTo(original.Roles);
        deserialized.UserId.Should().Be(original.UserId);
    }

    [Fact]
    public void GetUserByIdQuery_ShouldRoundtrip()
    {
        var original = new GetUserByIdQuery
        {
            UserId = "admin",
            TargetUserId = Guid.NewGuid(),
            IncludeDeleted = true
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<GetUserByIdQuery>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.TargetUserId.Should().Be(original.TargetUserId);
        deserialized.IncludeDeleted.Should().BeTrue();
    }

    [Fact]
    public void UserDto_ShouldRoundtrip()
    {
        var original = new UserDto
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            DisplayName = "Test User",
            Status = UserStatus.Active,
            Roles = new[] { "user" },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            LastActiveAt = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<UserDto>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Id.Should().Be(original.Id);
        deserialized.Email.Should().Be(original.Email);
        deserialized.DisplayName.Should().Be(original.DisplayName);
        deserialized.Status.Should().Be(original.Status);
    }

    [Fact]
    public void AgentCreated_ShouldRoundtrip()
    {
        var original = new AgentCreated
        {
            AggregateId = Guid.NewGuid(),
            Name = "Test Agent",
            Description = "A test agent",
            AgentType = "chat",
            Configuration = new Dictionary<string, object>
            {
                ["model"] = "gpt-4",
                ["temperature"] = 0.7
            },
            CreatedByUserId = "admin",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<AgentCreated>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be(original.Name);
        deserialized.AgentType.Should().Be(original.AgentType);
        deserialized.Configuration.Should().ContainKey("model");
    }

    [Fact]
    public void ExecuteAgentCommand_ShouldRoundtrip()
    {
        var original = new ExecuteAgentCommand
        {
            UserId = "admin",
            TargetAgentId = Guid.NewGuid(),
            Input = new Dictionary<string, object>
            {
                ["prompt"] = "Hello, world!"
            },
            Priority = 1,
            Timeout = TimeSpan.FromMinutes(5),
            WaitForCompletion = true
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<ExecuteAgentCommand>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.TargetAgentId.Should().Be(original.TargetAgentId);
        deserialized.Priority.Should().Be(1);
        deserialized.WaitForCompletion.Should().BeTrue();
    }

    [Fact]
    public void PaginatedResult_ShouldRoundtrip()
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
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "user2@example.com",
                DisplayName = "User 2",
                Status = UserStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var original = new PaginatedResult<UserDto>
        {
            Items = items,
            Page = 1,
            PageSize = 10,
            TotalCount = 100
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<PaginatedResult<UserDto>>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.Items.Should().HaveCount(2);
        deserialized.Page.Should().Be(1);
        deserialized.PageSize.Should().Be(10);
        deserialized.TotalCount.Should().Be(100);
        deserialized.TotalPages.Should().Be(10);
        deserialized.HasPreviousPage.Should().BeFalse();
        deserialized.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void ErrorResponse_ShouldRoundtrip()
    {
        var original = new ErrorResponse
        {
            StatusCode = 400,
            Code = "VALIDATION_ERROR",
            Message = "Validation failed",
            Details = "One or more fields are invalid",
            TraceId = "abc123",
            ValidationErrors = new List<ValidationError>
            {
                new() { Property = "email", Message = "Email is required", Code = "REQUIRED" }
            }
        };

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<ErrorResponse>(json, _options);

        deserialized.Should().NotBeNull();
        deserialized!.StatusCode.Should().Be(400);
        deserialized.Code.Should().Be("VALIDATION_ERROR");
        deserialized.ValidationErrors.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(UserStatus.Pending)]
    [InlineData(UserStatus.Active)]
    [InlineData(UserStatus.Suspended)]
    [InlineData(UserStatus.Deactivated)]
    public void UserStatus_ShouldSerializeCorrectly(UserStatus status)
    {
        var json = JsonSerializer.Serialize(status, _options);
        var deserialized = JsonSerializer.Deserialize<UserStatus>(json, _options);

        deserialized.Should().Be(status);
    }

    [Theory]
    [InlineData(AgentStatus.Creating)]
    [InlineData(AgentStatus.Idle)]
    [InlineData(AgentStatus.Executing)]
    [InlineData(AgentStatus.Error)]
    [InlineData(AgentStatus.Disabled)]
    public void AgentStatus_ShouldSerializeCorrectly(AgentStatus status)
    {
        var json = JsonSerializer.Serialize(status, _options);
        var deserialized = JsonSerializer.Deserialize<AgentStatus>(json, _options);

        deserialized.Should().Be(status);
    }
}
