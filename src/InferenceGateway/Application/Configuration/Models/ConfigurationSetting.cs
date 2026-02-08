// <copyright file="ConfigurationSetting.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Configuration.Models
{
    /// <summary>
    /// Represents a configuration setting value with its source.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    public class ConfigurationSetting<T>
    {
        /// <summary>
        /// Gets or sets the setting value.
        /// </summary>
        public T? Value { get; set; }

        /// <summary>
        /// Gets or sets the source of the setting (User, Group, Organization, Global).
        /// </summary>
        public required string Source { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the setting was found.
        /// </summary>
        public bool Found { get; set; }
    }
}
