// <copyright file="ConfigurationStartupFilterLoggerMessages.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Configuration.StartupValidation;

using Microsoft.Extensions.Logging;

/// <summary>
/// Logger messages for ConfigurationStartupFilter.
/// </summary>
internal static partial class ConfigurationStartupFilterLoggerMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Validating Synaxis configuration at startup...")]
    internal static partial void ConfigurationValidationStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Configuration validation failed with {ErrorCount} error(s)")]
    internal static partial void ConfigurationValidationFailed(this ILogger logger, int errorCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Synaxis configuration validated successfully")]
    internal static partial void ConfigurationValidationSucceeded(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{OptionName} not registered, skipping validation")]
    internal static partial void ConfigurationOptionNotRegistered(this ILogger logger, string optionName);
}
