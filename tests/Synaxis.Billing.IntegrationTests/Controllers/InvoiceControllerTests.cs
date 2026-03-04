// <copyright file="InvoiceControllerTests.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Billing.IntegrationTests.Controllers;

using System.Net;
using System.Net.Http.Json;

using global::Billing.Application.Services;
using global::Billing.Domain.Entities;
using global::Billing.Infrastructure.Data;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Synaxis.Billing.IntegrationTests.Fixtures;
using Xunit;

/// <summary>
/// Integration tests for the Invoices controller.
/// </summary>
[Collection("BillingIntegration")]
[Trait("Category", "Integration")]
public sealed class InvoiceControllerTests : IAsyncLifetime
{
    private readonly BillingDatabaseFixture _dbFixture;
    private readonly BillingApiFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _organizationId;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvoiceControllerTests"/> class.
    /// </summary>
    public InvoiceControllerTests(BillingDatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture ?? throw new ArgumentNullException(nameof(dbFixture));
        _factory = new BillingApiFactory();
        _organizationId = Guid.NewGuid();
        _factory.SetConnectionString(_dbFixture.ConnectionString);
        _factory.SetAuthOptions(_organizationId, "Admin", "BillingManager");
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await _dbFixture.ResetDatabaseAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetInvoices_ForOrganization_Returns200OkWithInvoices()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var invoice1 = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-001",
            OrganizationId = _organizationId,
            IssueDate = DateTime.UtcNow.AddDays(-10),
            DueDate = DateTime.UtcNow.AddDays(20),
            Amount = 100.00m,
            Currency = "USD",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<InvoiceLineItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Description = "Monthly subscription",
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    Amount = 100.00m,
                    ResourceType = "subscription",
                },
            },
        };
        var invoice2 = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-002",
            OrganizationId = _organizationId,
            IssueDate = DateTime.UtcNow.AddDays(-5),
            DueDate = DateTime.UtcNow.AddDays(25),
            Amount = 200.00m,
            Currency = "USD",
            Status = "Paid",
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<InvoiceLineItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Description = "Usage fees",
                    Quantity = 1,
                    UnitPrice = 200.00m,
                    Amount = 200.00m,
                    ResourceType = "usage",
                },
            },
        };

        context.Invoices.AddRange(invoice1, invoice2);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/api/v1/invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoices = await response.Content.ReadFromJsonAsync<List<InvoiceDto>>();
        invoices.Should().NotBeNull();
        invoices.Should().HaveCount(2);
        invoices!.Select(i => i.InvoiceNumber).Should().Contain("INV-001", "INV-002");
    }

    [Fact]
    public async Task GetInvoices_WhenNoneExist_Returns200OkWithEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/invoices");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoices = await response.Content.ReadFromJsonAsync<List<InvoiceDto>>();
        invoices.Should().NotBeNull();
        invoices.Should().BeEmpty();
    }

    [Fact]
    public async Task GetInvoice_ById_Returns200OkWithInvoice()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-003",
            OrganizationId = _organizationId,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Amount = 150.00m,
            Currency = "USD",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            LineItems = new List<InvoiceLineItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Description = "Service fee",
                    Quantity = 1,
                    UnitPrice = 150.00m,
                    Amount = 150.00m,
                    ResourceType = "service",
                },
            },
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/invoices/{invoice.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<InvoiceDto>();
        result.Should().NotBeNull();
        result!.InvoiceNumber.Should().Be("INV-003");
        result.Amount.Should().Be(150.00m);
        result.LineItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetInvoice_ById_WhenNotFound_Returns404NotFound()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/invoices/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetInvoice_ById_WrongOrganization_Returns403Forbidden()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-004",
            OrganizationId = Guid.NewGuid(), // Different organization
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Amount = 150.00m,
            Currency = "USD",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/invoices/{invoice.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateInvoice_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new GenerateInvoiceRequest(
            _organizationId,
            DateTime.UtcNow.AddMonths(-1),
            DateTime.UtcNow,
            "Test invoice");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/invoices", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<InvoiceDto>();
        result.Should().NotBeNull();
        result!.OrganizationId.Should().Be(_organizationId);
        result.Status.Should().Be("Pending");
        result.InvoiceNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ApproveInvoice_WhenPending_Returns204NoContent()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-005",
            OrganizationId = _organizationId,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Amount = 100.00m,
            Currency = "USD",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.PostAsync($"/api/v1/invoices/{invoice.Id}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify status was updated
        await using var verifyContext = _dbFixture.CreateDbContext();
        var approvedInvoice = await verifyContext.Invoices.FindAsync(invoice.Id);
        approvedInvoice!.Status.Should().Be("Approved");
    }

    [Fact]
    public async Task ApproveInvoice_WhenNotFound_Returns404NotFound()
    {
        // Act
        var response = await _client.PostAsync($"/api/v1/invoices/{Guid.NewGuid()}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelInvoice_WhenPending_Returns204NoContent()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-006",
            OrganizationId = _organizationId,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Amount = 100.00m,
            Currency = "USD",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var request = new CancelInvoiceRequest("Customer requested cancellation");

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/invoices/{invoice.Id}/cancel", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SendInvoice_WithEmail_Returns204NoContent()
    {
        // Arrange
        await using var context = _dbFixture.CreateDbContext();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-007",
            OrganizationId = _organizationId,
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            Amount = 100.00m,
            Currency = "USD",
            Status = "Approved",
            CreatedAt = DateTime.UtcNow,
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var request = new SendInvoiceRequest("billing@example.com");

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/invoices/{invoice.Id}/send", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateInvoice_WithoutAdminRole_Returns403Forbidden()
    {
        // Arrange
        var nonAdminFactory = new BillingApiFactory();
        nonAdminFactory.SetConnectionString(_dbFixture.ConnectionString);
        nonAdminFactory.SetAuthOptions(_organizationId, "User");
        var nonAdminClient = nonAdminFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var request = new GenerateInvoiceRequest(
            _organizationId,
            DateTime.UtcNow.AddMonths(-1),
            DateTime.UtcNow,
            "Test invoice");

        // Act
        var response = await nonAdminClient.PostAsJsonAsync("/api/v1/invoices", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

/// <summary>
/// Request to cancel an invoice.
/// </summary>
public record CancelInvoiceRequest(string Reason);

/// <summary>
/// Request to send an invoice.
/// </summary>
public record SendInvoiceRequest(string EmailAddress);
