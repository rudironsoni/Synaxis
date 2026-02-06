// <copyright file="OpenAIToolNormalizer.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.AI;

    /// <summary>
    /// OpenAI-specific tool normalizer implementation.
    /// </summary>
    public class OpenAIToolNormalizer : IToolNormalizer
    {
        /// <summary>
        /// Normalizes tool calls in a canonical response.
        /// </summary>
        /// <param name="response">The response to normalize.</param>
        /// <returns>The normalized response.</returns>
        public CanonicalResponse NormalizeResponse(CanonicalResponse response)
        {
            if (response.toolCalls == null || response.toolCalls.Count == 0)
            {
                return response;
            }

            var normalizedCalls = new List<FunctionCallContent>();
            foreach (var toolCall in response.toolCalls)
            {
                var id = !string.IsNullOrWhiteSpace(toolCall.CallId) ? toolCall.CallId : $"call_{Guid.NewGuid().ToString("N").Substring(0, 24)}";

                // FunctionCallContent usually expects IDictionary<string, object?> for arguments
                // If arguments are already parsed, good. If not, we might need to ensure they are valid.
                // Since CanonicalResponse now uses FunctionCallContent, we assume they are already in the correct type.
                // We just ensure ID is present.

                // Create new FunctionCallContent with ID if missing
                // Note: FunctionCallContent properties might be read-only, so we create new instance.
                var newCall = new FunctionCallContent(id, toolCall.Name, toolCall.Arguments);
                normalizedCalls.Add(newCall);
            }

#pragma warning disable SA1101 // Prefix local calls with this - False positive: 'response' is a parameter, not a member
            return response with { toolCalls = normalizedCalls };
#pragma warning restore SA1101 // Prefix local calls with this
        }

        /// <summary>
        /// Normalizes tool calls in a chat response update.
        /// </summary>
        /// <param name="update">The update to normalize.</param>
        /// <returns>The normalized update.</returns>
        public ChatResponseUpdate NormalizeUpdate(ChatResponseUpdate update)
        {
            // For streaming, we just pass through for now as ID generation needs state tracking
            // which is complex for a stateless normalizer.
            // In a full implementation, we'd need a stateful wrapper or context.
            return update;
        }
    }
}
