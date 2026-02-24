// <copyright file="EmailTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Identity.UnitTests.ValueObjects;

using Synaxis.Identity.Domain.ValueObjects;
using Xunit;
using FluentAssertions;

[Trait("Category", "Unit")]
public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.com")]
    [InlineData("user123@test-domain.com")]
    [InlineData("USER@EXAMPLE.COM")]
    public void Create_ValidEmail_CreatesInstance(string email)
    {
        // Act
        var result = Email.Create(email);

        // Assert
        result.Value.Should().Be(email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhitespaceEmail_ThrowsException(string email)
    {
        // Act
        var act = () => Email.Create(email);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email cannot be null or whitespace.*");
    }

    [Fact]
    public void Create_NullEmail_ThrowsException()
    {
        // Act
        var act = () => Email.Create(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Email cannot be null or whitespace.*");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@example.com")]
    [InlineData("invalid@com")]
    [InlineData("invalid@.com")]
    [InlineData("invalid@domain.")]
    [InlineData("invalid @example.com")]
    [InlineData("invalid@example com")]
    public void Create_InvalidEmail_ThrowsException(string email)
    {
        // Act
        var act = () => Email.Create(email);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage($"Invalid email address: {email}*");
    }

    [Fact]
    public void ToString_ReturnsEmailValue()
    {
        // Arrange
        var email = Email.Create("test@example.com");

        // Act
        var result = email.ToString();

        // Assert
        result.Should().Be("test@example.com");
    }

    [Fact]
    public void Equality_SameValue_ReturnsTrue()
    {
        // Arrange
        var email1 = Email.Create("test@example.com");
        var email2 = Email.Create("test@example.com");

        // Act
        var result = email1 == email2;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var email1 = Email.Create("test1@example.com");
        var email2 = Email.Create("test2@example.com");

        // Act
        var result = email1 == email2;

        // Assert
        result.Should().BeFalse();
    }
}
