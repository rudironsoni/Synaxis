// <copyright file="CloudProviderOptionsTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.Tests.Options;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Synaxis.Configuration.Options;
using Xunit;

/// <summary>
/// Unit tests for <see cref="CloudProviderOptions"/>.
/// </summary>
public class CloudProviderOptionsTests
{
    [Fact]
    public void CloudProviderOptions_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var options = new CloudProviderOptions
        {
            DefaultProvider = "Azure",
            Azure = new AzureOptions
            {
                SubscriptionId = "12345678-1234-1234-1234-123456789012",
                ResourceGroup = "test-rg",
                Region = "eastus",
                TenantId = "12345678-1234-1234-1234-123456789012",
            },
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
    public void CloudProviderOptions_WithMissingDefaultProvider_ShouldFailValidation()
    {
        // Arrange
        var options = new CloudProviderOptions
        {
            DefaultProvider = string.Empty,
        };

        // Act
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, context, results, true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.ErrorMessage != null && r.ErrorMessage.Contains("Default provider"));
    }

    [Fact]
    public void CloudProviderOptions_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var options = new CloudProviderOptions();

        // Assert
        options.DefaultProvider.Should().BeEmpty();
        options.Azure.Should().BeNull();
        options.Aws.Should().BeNull();
        options.Gcp.Should().BeNull();
        options.OnPremise.Should().BeNull();
        options.EventStore.Should().BeNull();
        options.KeyVault.Should().BeNull();
        options.MessageBus.Should().BeNull();
    }
}
