using FluentAssertions;
using Synaxis.Contracts.V2.Converters;

namespace Synaxis.Contracts.Tests.V2;

public class ConverterTests
{
    [Fact]
    public void UserConverter_ShouldConvertV1ToV2()
    {
        var v1User = new global::Synaxis.Contracts.V1.DTOs.UserDto
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            DisplayName = "Test User",
            Status = global::Synaxis.Contracts.V1.Common.UserStatus.Active,
            Roles = new[] { "user", "admin" },
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var converter = new UserConverter();
        var v2User = converter.Convert(v1User);

        v2User.Should().NotBeNull();
        v2User.Id.Should().Be(v1User.Id);
        v2User.Email.Should().Be(v1User.Email);
        v2User.DisplayName.Should().Be(v1User.DisplayName);
        v2User.IsAdmin.Should().BeTrue();
        v2User.Status.Should().Be(global::Synaxis.Contracts.V2.Common.UserStatus.Active);
        v2User.TenantId.Should().BeNull();
    }

    [Fact]
    public void UserConverter_ShouldConvertPendingStatus()
    {
        var v1User = new global::Synaxis.Contracts.V1.DTOs.UserDto
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            DisplayName = "Test User",
            Status = global::Synaxis.Contracts.V1.Common.UserStatus.Pending,
            Roles = Array.Empty<string>(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var converter = new UserConverter();
        var v2User = converter.Convert(v1User);

        v2User.Status.Should().Be(global::Synaxis.Contracts.V2.Common.UserStatus.PendingVerification);
    }

    [Fact]
    public void UserConverter_ShouldConvertDeactivatedStatus()
    {
        var v1User = new global::Synaxis.Contracts.V1.DTOs.UserDto
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            DisplayName = "Test User",
            Status = global::Synaxis.Contracts.V1.Common.UserStatus.Deactivated,
            Roles = Array.Empty<string>(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var converter = new UserConverter();
        var v2User = converter.Convert(v1User);

        v2User.Status.Should().Be(global::Synaxis.Contracts.V2.Common.UserStatus.Inactive);
    }

    [Fact]
    public void UserConverter_ExtensionMethod_ShouldWork()
    {
        var v1User = new global::Synaxis.Contracts.V1.DTOs.UserDto
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            DisplayName = "Test User",
            Status = global::Synaxis.Contracts.V1.Common.UserStatus.Active,
            Roles = new[] { "user" },
            CreatedAt = DateTimeOffset.UtcNow
        };

        var v2User = v1User.ToV2();

        v2User.Should().NotBeNull();
        v2User.Email.Should().Be(v1User.Email);
        v2User.IsAdmin.Should().BeFalse();
    }

    [Fact]
    public void UserConverter_ExtensionMethodEnumerable_ShouldWork()
    {
        var v1Users = new List<global::Synaxis.Contracts.V1.DTOs.UserDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = "user1@example.com",
                DisplayName = "User 1",
                Status = global::Synaxis.Contracts.V1.Common.UserStatus.Active,
                Roles = new[] { "user" },
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "user2@example.com",
                DisplayName = "User 2",
                Status = global::Synaxis.Contracts.V1.Common.UserStatus.Active,
                Roles = new[] { "admin" },
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var v2Users = v1Users.ToV2().ToList();

        v2Users.Should().HaveCount(2);
        v2Users[0].IsAdmin.Should().BeFalse();
        v2Users[1].IsAdmin.Should().BeTrue();
    }

    [Fact]
    public void AgentConverter_ShouldConvertV1ToV2()
    {
        var v1Agent = new global::Synaxis.Contracts.V1.DTOs.AgentDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            Description = "A test agent",
            AgentType = "chat",
            Status = global::Synaxis.Contracts.V1.Common.AgentStatus.Idle,
            Configuration = new Dictionary<string, object> { ["model"] = "gpt-4" },
            Tags = new[] { "production", "ai" },
            CreatedByUserId = "admin",
            CreatedAt = DateTimeOffset.UtcNow,
            ExecutionCount = 42
        };

        var converter = new AgentConverter();
        var v2Agent = converter.Convert(v1Agent);

        v2Agent.Should().NotBeNull();
        v2Agent.Id.Should().Be(v1Agent.Id);
        v2Agent.Name.Should().Be(v1Agent.Name);
        v2Agent.Status.Should().Be(global::Synaxis.Contracts.V2.Common.AgentStatus.Idle);
        v2Agent.Labels.Should().NotBeNull();
        v2Agent.Labels.Should().ContainKey("production");
        v2Agent.Labels.Should().ContainKey("ai");
        v2Agent.Stats.Should().NotBeNull();
        v2Agent.Stats!.TotalCount.Should().Be(42);
    }

    [Fact]
    public void AgentConverter_ShouldConvertCreatingToProvisioning()
    {
        var v1Agent = new global::Synaxis.Contracts.V1.DTOs.AgentDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            AgentType = "chat",
            Status = global::Synaxis.Contracts.V1.Common.AgentStatus.Creating,
            CreatedByUserId = "admin",
            CreatedAt = DateTimeOffset.UtcNow,
            ExecutionCount = 0
        };

        var converter = new AgentConverter();
        var v2Agent = converter.Convert(v1Agent);

        v2Agent.Status.Should().Be(global::Synaxis.Contracts.V2.Common.AgentStatus.Provisioning);
    }

    [Fact]
    public void AgentConverter_ShouldConvertExecutingToProcessing()
    {
        var v1Agent = new global::Synaxis.Contracts.V1.DTOs.AgentDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            AgentType = "chat",
            Status = global::Synaxis.Contracts.V1.Common.AgentStatus.Executing,
            CreatedByUserId = "admin",
            CreatedAt = DateTimeOffset.UtcNow,
            ExecutionCount = 0
        };

        var converter = new AgentConverter();
        var v2Agent = converter.Convert(v1Agent);

        v2Agent.Status.Should().Be(global::Synaxis.Contracts.V2.Common.AgentStatus.Processing);
    }

    [Fact]
    public void AgentConverter_ShouldHandleEmptyTags()
    {
        var v1Agent = new global::Synaxis.Contracts.V1.DTOs.AgentDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            AgentType = "chat",
            Status = global::Synaxis.Contracts.V1.Common.AgentStatus.Idle,
            Tags = Array.Empty<string>(),
            CreatedByUserId = "admin",
            CreatedAt = DateTimeOffset.UtcNow,
            ExecutionCount = 0
        };

        var converter = new AgentConverter();
        var v2Agent = converter.Convert(v1Agent);

        v2Agent.Labels.Should().BeNull();
    }

    [Fact]
    public void AgentConverter_ExtensionMethod_ShouldWork()
    {
        var v1Agent = new global::Synaxis.Contracts.V1.DTOs.AgentDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Agent",
            AgentType = "chat",
            Status = global::Synaxis.Contracts.V1.Common.AgentStatus.Idle,
            Tags = new[] { "test" },
            CreatedByUserId = "admin",
            CreatedAt = DateTimeOffset.UtcNow,
            ExecutionCount = 0
        };

        var v2Agent = v1Agent.ToV2();

        v2Agent.Should().NotBeNull();
        v2Agent.Name.Should().Be(v1Agent.Name);
        v2Agent.Labels.Should().ContainKey("test");
    }

    [Fact]
    public void UserConverter_ShouldThrowOnNull()
    {
        var converter = new UserConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Action act = () => converter.Convert(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AgentConverter_ShouldThrowOnNull()
    {
        var converter = new AgentConverter();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Action act = () => converter.Convert(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        act.Should().Throw<ArgumentNullException>();
    }
}
