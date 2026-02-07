// <copyright file="CanonicalModelId.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Routing;

/// <summary>
/// Represents a canonical model identifier with provider and model path.
/// </summary>
/// <param name="provider">The provider name.</param>
/// <param name="modelPath">The model path.</param>
public record CanonicalModelId(string provider, string modelPath)
{
    /// <summary>
    /// Returns the string representation of the canonical model identifier.
    /// </summary>
    /// <returns>A string in the format "provider/modelPath".</returns>
    public override string ToString() => $"{this.provider}/{this.modelPath}";

    /// <summary>
    /// Parses a string into a CanonicalModelId.
    /// </summary>
    /// <param name="input">The input string to parse.</param>
    /// <returns>A CanonicalModelId instance.</returns>
    public static CanonicalModelId Parse(string input)
    {
        // Special handling for models starting with @ (e.g. Cloudflare @cf/...)
        if (input.StartsWith("@", StringComparison.Ordinal))
        {
            return new CanonicalModelId("unknown", input);
        }

        var parts = input.Split('/', 2);
        return parts.Length == 2 ? new CanonicalModelId(parts[0], parts[1]) : new CanonicalModelId("unknown", input);
    }
}
