// <copyright file="ModelDto.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Infrastructure.External.ModelsDev.Dto
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Data transfer object representing a model from models.dev.
    /// </summary>
    public sealed class ModelDto
    {
        /// <summary>
        /// Gets or sets the model identifier.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the model family.
        /// </summary>
        [JsonPropertyName("family")]
        public string? Family { get; set; }

        /// <summary>
        /// Gets or sets the Limit.
        /// </summary>
        [JsonPropertyName("limit")]
        public LimitDto? Limit { get; set; }

        /// <summary>
        /// Gets or sets the Cost.
        /// </summary>
        [JsonPropertyName("cost")]
        public CostDto? Cost { get; set; }

        /// <summary>
        /// Gets or sets the Modalities.
        /// </summary>
        [JsonPropertyName("modalities")]
        public ModalitiesDto? Modalities { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model has open weights.
        /// </summary>
        [JsonPropertyName("open_weights")]
        public bool? OpenWeights { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model supports tool calls.
        /// </summary>
        [JsonPropertyName("tool_call")]
        public bool? ToolCall { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model supports reasoning.
        /// </summary>
        [JsonPropertyName("reasoning")]
        public bool? Reasoning { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the model supports structured output.
        /// </summary>
        [JsonPropertyName("structured_output")]
        public bool? StructuredOutput { get; set; }

        /// <summary>
        /// Gets or sets the release date of the model.
        /// </summary>
        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        /// <summary>
        /// Data transfer object for model limits.
        /// </summary>
        public sealed class LimitDto
        {
            /// <summary>
            /// Gets or sets the Context.
            /// </summary>
            [JsonPropertyName("context")]
            public int? Context { get; set; }

            /// <summary>
            /// Gets or sets the Output.
            /// </summary>
            [JsonPropertyName("output")]
            public int? Output { get; set; }
        }

        /// <summary>
        /// Data transfer object for model cost.
        /// </summary>
        public sealed class CostDto
        {
            /// <summary>
            /// Gets or sets the input cost per token.
            /// </summary>
            [JsonPropertyName("input")]
            public decimal? Input { get; set; }

            /// <summary>
            /// Gets or sets the output cost per token.
            /// </summary>
            [JsonPropertyName("output")]
            public decimal? Output { get; set; }
        }

        /// <summary>
        /// Data transfer object for model modalities.
        /// </summary>
        public sealed class ModalitiesDto
        {
            /// <summary>
            /// Gets or sets the Input.
            /// </summary>
            [JsonPropertyName("input")]
            public string[] ? Input { get; set; }

            /// <summary>
            /// Gets or sets the Output.
            /// </summary>
            [JsonPropertyName("output")]
            public string[] ? Output { get; set; }
        }
    }
}
