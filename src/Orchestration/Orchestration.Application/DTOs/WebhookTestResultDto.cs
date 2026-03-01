// <copyright file="WebhookTestResultDto.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Orchestration.Application.DTOs;

/// <summary>
/// Represents a webhook test result.
/// </summary>
/// <param name="Success">Whether the test succeeded.</param>
/// <param name="StatusCode">The HTTP status code returned.</param>
/// <param name="ResponseBody">The response body.</param>
/// <param name="ResponseTimeMs">The response time in milliseconds.</param>
public record WebhookTestResultDto(
    bool Success,
    int StatusCode,
    string? ResponseBody,
    long ResponseTimeMs);
