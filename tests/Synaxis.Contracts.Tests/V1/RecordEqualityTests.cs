using FluentAssertions;
using Synaxis.Contracts.V1.Common;
using Synaxis.Contracts.V1.Commands;
using Synaxis.Contracts.V1.DomainEvents;
using Synaxis.Contracts.V1.DTOs;

namespace Synaxis.Contracts.Tests.V1;

public class RecordEqualityTests
{
    [Fact]
    public void UserCreated_WithSameValues_ShouldBeEqual()
    {
        var id = Guid.NewGuid();
        var aggregateId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var event1 = new UserCreated
        {
            EventId = id,
            AggregateId = aggregateId,
            Timestamp = timestamp,
            Email = "test@example.com",
            DisplayName = "Test User",
            Status = UserStatus.Active,
            Version = 1
        };

        var event2 = new UserCreated
        {
            EventId = id,
            AggregateId = aggregateId,
            Timestamp = timestamp,
            Email = "test@example.com",
            DisplayName = "Test User",
            Status = UserStatus.Active,
            Version = 1
        };

        event1.Should().Be(event2);
        (event1 == event2).Should().BeTrue();
    }

    [Fact]
    public void UserCreated_WithDifferentValues_ShouldNotBeEqual()
    {
        var event1 = new UserCreated
        {
            AggregateId = Guid.NewGuid(),
            Email = "test1@example.com",
            DisplayName = "Test User 1",
            Status = UserStatus.Active
        };

        var event2 = new UserCreated
        {
            AggregateId = Guid.NewGuid(),
            Email = "test2@example.com",
            DisplayName = "Test User 2",
            Status = UserStatus.Active
        };

        event1.Should().NotBe(event2);
        (event1 != event2).Should().BeTrue();
    }

    [Fact]
    public void UserDto_WithSameValues_ShouldBeEqual()
    {
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var dto1 = new UserDto
        {
            Id = id,
            Email = "user@example.com",
            DisplayName = "User",
            Status = UserStatus.Active,
            CreatedAt = now
        };

        var dto2 = new UserDto
        {
            Id = id,
            Email = "user@example.com",
            DisplayName = "User",
            Status = UserStatus.Active,
            CreatedAt = now
        };

        dto1.Should().Be(dto2);
    }

    [Fact]
    public void CreateUserCommand_WithSameValues_ShouldBeEqual()
    {
        var id = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var cmd1 = new CreateUserCommand
        {
            CommandId = id,
            CorrelationId = correlationId,
            Timestamp = timestamp,
            UserId = "admin",
            Email = "new@example.com",
            DisplayName = "New User",
            Password = "password"
        };

        var cmd2 = new CreateUserCommand
        {
            CommandId = id,
            CorrelationId = correlationId,
            Timestamp = timestamp,
            UserId = "admin",
            Email = "new@example.com",
            DisplayName = "New User",
            Password = "password"
        };

        cmd1.Should().Be(cmd2);
    }

    [Fact]
    public void PaginatedResult_WithSameValues_ShouldBeEqual()
    {
        var items = new List<UserDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                DisplayName = "User",
                Status = UserStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var result1 = new PaginatedResult<UserDto>
        {
            Items = items,
            Page = 1,
            PageSize = 10,
            TotalCount = 100
        };

        var result2 = new PaginatedResult<UserDto>
        {
            Items = items,
            Page = 1,
            PageSize = 10,
            TotalCount = 100
        };

        result1.Should().Be(result2);
    }

    [Fact]
    public void PaginatedResult_CalculatedProperties_ShouldBeCorrect()
    {
        var result = new PaginatedResult<UserDto>
        {
            Items = Array.Empty<UserDto>(),
            Page = 2,
            PageSize = 10,
            TotalCount = 100
        };

        result.TotalPages.Should().Be(10);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedResult_FirstPage_HasPreviousPage_ShouldBeFalse()
    {
        var result = new PaginatedResult<UserDto>
        {
            Items = Array.Empty<UserDto>(),
            Page = 1,
            PageSize = 10,
            TotalCount = 100
        };

        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedResult_LastPage_HasNextPage_ShouldBeFalse()
    {
        var result = new PaginatedResult<UserDto>
        {
            Items = Array.Empty<UserDto>(),
            Page = 10,
            PageSize = 10,
            TotalCount = 100
        };

        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void ErrorResponse_WithSameValues_ShouldBeEqual()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var error1 = new ErrorResponse
        {
            StatusCode = 400,
            Code = "ERROR",
            Message = "Error message",
            Timestamp = timestamp
        };

        var error2 = new ErrorResponse
        {
            StatusCode = 400,
            Code = "ERROR",
            Message = "Error message",
            Timestamp = timestamp
        };

        error1.Should().Be(error2);
    }

    [Fact]
    public void ValidationError_WithSameValues_ShouldBeEqual()
    {
        var error1 = new ValidationError
        {
            Property = "email",
            Message = "Required",
            Code = "REQUIRED"
        };

        var error2 = new ValidationError
        {
            Property = "email",
            Message = "Required",
            Code = "REQUIRED"
        };

        error1.Should().Be(error2);
    }

    [Fact]
    public void Records_ShouldSupportWithExpressions()
    {
        var original = new UserDto
        {
            Id = Guid.NewGuid(),
            Email = "original@example.com",
            DisplayName = "Original",
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var modified = original with { Email = "modified@example.com" };

        modified.Should().NotBe(original);
        modified.Id.Should().Be(original.Id);
        modified.Email.Should().Be("modified@example.com");
        modified.DisplayName.Should().Be(original.DisplayName);
    }
}
