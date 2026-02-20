// <copyright file="AzureOptionsTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Tests.Options;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Synaxis.Configuration.Options;
using Xunit;

/// <summary>
/// Unit tests for <see cref="AzureOptions"/>.
/// </summary>
public class AzureOptionsTests
{
    [Fact]
    public void AzureOptions_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var options = new AzureOptions
        {
            SubscriptionId = "12345678-1234-1234-1234-123456789012",
            ResourceGroup = "test-rg",
            Region = "eastus",
            TenantId = "12345678-1234-1234-1234-123456789012",
            UseManagedIdentity = true,
        };

        // Act
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Fact]
    public void AzureOptions_WithMissingRequiredFields_ShouldFailValidation()
    {
        // Arrange
        var options = new AzureOptions
        {
            SubscriptionId = string.Empty,
            ResourceGroup = string.Empty,
            Region = string.Empty,
            TenantId = string.Empty,
        };

        // Act
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Count.Should().Be(4);
    }

    [Fact]
    public void AzureOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new AzureOptions();

        // Assert
        options.SubscriptionId.Should().BeEmpty();
        options.ResourceGroup.Should().BeEmpty();
        options.Region.Should().BeEmpty();
        options.TenantId.Should().BeEmpty();
        options.ClientId.Should().BeEmpty();
        options.ClientSecret.Should().BeEmpty();
        options.UseManagedIdentity.Should().BeTrue();
    }
}
