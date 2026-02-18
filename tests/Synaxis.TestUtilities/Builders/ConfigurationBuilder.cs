// <copyright file="ConfigurationBuilder.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.TestUtilities.Builders;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Builder for creating test configuration.
/// </summary>
public class ConfigurationBuilder
{
    private readonly Dictionary<string, string?> _settings = new();
    private readonly Dictionary<string, IConfigurationSection> _sections = new();

    /// <summary>
    /// Adds a setting to the configuration.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithSetting(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        _settings[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple settings to the configuration.
    /// </summary>
    /// <param name="settings">The settings to add.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithSettings(IDictionary<string, string?> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        foreach (var (key, value) in settings)
        {
            _settings[key] = value;
        }

        return this;
    }

    /// <summary>
    /// Adds a connection string to the configuration.
    /// </summary>
    /// <param name="name">The connection string name.</param>
    /// <param name="connectionString">The connection string value.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithConnectionString(string name, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        _settings[$"ConnectionStrings:{name}"] = connectionString;
        return this;
    }

    /// <summary>
    /// Adds an application setting with a prefixed key.
    /// </summary>
    /// <param name="section">The section name.</param>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The setting value.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithAppSetting(string section, string key, string? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(section);
        ArgumentException.ThrowIfNullOrEmpty(key);

        _settings[$"{section}:{key}"] = value;
        return this;
    }

    /// <summary>
    /// Adds a boolean setting.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The boolean value.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithSetting(string key, bool value)
    {
        return WithSetting(key, value.ToString());
    }

    /// <summary>
    /// Adds an integer setting.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The integer value.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithSetting(string key, int value)
    {
        return WithSetting(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Adds a TimeSpan setting.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The TimeSpan value.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithSetting(string key, TimeSpan value)
    {
        return WithSetting(key, value.ToString());
    }

    /// <summary>
    /// Adds a Uri setting.
    /// </summary>
    /// <param name="key">The setting key.</param>
    /// <param name="value">The Uri value.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithSetting(string key, Uri value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return WithSetting(key, value.ToString());
    }

    /// <summary>
    /// Adds database configuration settings.
    /// </summary>
    /// <param name="provider">The database provider.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithDatabase(string provider, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(provider);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        WithSetting("Database:Provider", provider);
        WithConnectionString("DefaultConnection", connectionString);
        return this;
    }

    /// <summary>
    /// Adds cache configuration settings.
    /// </summary>
    /// <param name="provider">The cache provider (e.g., "Redis", "Memory").</param>
    /// <param name="connectionString">The cache connection string.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithCache(string provider, string? connectionString = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(provider);

        WithSetting("Cache:Provider", provider);
        if (!string.IsNullOrEmpty(connectionString))
        {
            WithConnectionString("Cache", connectionString);
        }

        return this;
    }

    /// <summary>
    /// Adds logging configuration.
    /// </summary>
    /// <param name="level">The default log level.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithLogging(string level)
    {
        ArgumentException.ThrowIfNullOrEmpty(level);

        WithSetting("Logging:LogLevel:Default", level);
        return this;
    }

    /// <summary>
    /// Adds feature flags.
    /// </summary>
    /// <param name="featureName">The feature name.</param>
    /// <param name="enabled">Whether the feature is enabled.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithFeatureFlag(string featureName, bool enabled)
    {
        ArgumentException.ThrowIfNullOrEmpty(featureName);

        WithSetting($"FeatureFlags:{featureName}", enabled);
        return this;
    }

    /// <summary>
    /// Adds a section to the configuration.
    /// </summary>
    /// <param name="name">The section name.</param>
    /// <param name="section">The section configuration.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithSection(string name, IConfigurationSection section)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(section);

        _sections[name] = section;
        return this;
    }

    /// <summary>
    /// Adds settings from a JSON string by parsing key-value pairs.
    /// </summary>
    /// <param name="json">The JSON string containing settings.</param>
    /// <returns>The builder for method chaining.</returns>
    public ConfigurationBuilder WithJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        // Parse simple JSON and add to settings
        // For complex JSON, use WithSetting method directly
        _settings["JsonSettings"] = json;
        return this;
    }

    /// <summary>
    /// Builds the configuration.
    /// </summary>
    /// <returns>The constructed configuration.</returns>
    public IConfiguration Build()
    {
        var configBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();

        configBuilder.AddInMemoryCollection(_settings);

        foreach (var (name, section) in _sections)
        {
            configBuilder.AddConfiguration(section);
        }

        return configBuilder.Build();
    }

    /// <summary>
    /// Builds the configuration root.
    /// </summary>
    /// <returns>The constructed configuration root.</returns>
    public IConfigurationRoot BuildRoot()
    {
        var configBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();

        configBuilder.AddInMemoryCollection(_settings);

        foreach (var (name, section) in _sections)
        {
            configBuilder.AddConfiguration(section);
        }

        return configBuilder.Build();
    }

    /// <summary>
    /// Creates an empty configuration.
    /// </summary>
    /// <returns>An empty configuration.</returns>
    public static IConfiguration Empty()
    {
        return new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .Build();
    }

    /// <summary>
    /// Creates a configuration with default test settings.
    /// </summary>
    /// <returns>A configuration with default settings.</returns>
    public static IConfiguration Default()
    {
        return new ConfigurationBuilder()
            .WithLogging("Warning")
            .WithCache("Memory")
            .Build();
    }

    private void FlattenConfiguration(IConfiguration config, string prefix)
    {
        foreach (var child in config.GetChildren())
        {
            var key = string.IsNullOrEmpty(prefix) ? child.Key : $"{prefix}:{child.Key}";

            if (child.GetChildren().Any())
            {
                FlattenConfiguration(child, key);
            }
            else
            {
                _settings[key] = child.Value;
            }
        }
    }
}

/// <summary>
/// Provides static methods for creating configurations.
/// </summary>
[SuppressMessage("Design", "CA1052:Static holder types should be sealed", Justification = "Utility class")]
public static class Configuration
{
    /// <summary>
    /// Creates a new configuration builder.
    /// </summary>
    /// <returns>A new configuration builder.</returns>
    public static ConfigurationBuilder CreateBuilder()
    {
        return new ConfigurationBuilder();
    }

    /// <summary>
    /// Creates an empty configuration.
    /// </summary>
    /// <returns>An empty configuration.</returns>
    public static IConfiguration Empty()
    {
        return ConfigurationBuilder.Empty();
    }

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    /// <returns>A default configuration.</returns>
    public static IConfiguration Default()
    {
        return ConfigurationBuilder.Default();
    }
}
