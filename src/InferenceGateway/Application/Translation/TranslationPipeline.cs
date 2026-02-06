// <copyright file="TranslationPipeline.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.Application.Translation
{
    using System.Linq;
    using Microsoft.Extensions.AI;

    /// <summary>
    /// Implementation of translation pipeline that orchestrates request, response, and streaming translators.
    /// </summary>
    public sealed class TranslationPipeline : ITranslationPipeline
    {
        private readonly IReadOnlyList<IRequestTranslator> requestTranslators;
        private readonly IReadOnlyList<IResponseTranslator> responseTranslators;
        private readonly IReadOnlyList<IStreamingTranslator> streamingTranslators;
        private readonly IToolNormalizer toolNormalizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationPipeline"/> class.
        /// </summary>
        /// <param name="requestTranslators">The collection of request translators.</param>
        /// <param name="responseTranslators">The collection of response translators.</param>
        /// <param name="streamingTranslators">The collection of streaming translators.</param>
        /// <param name="toolNormalizer">The tool normalizer for processing tools.</param>
        public TranslationPipeline(
            IEnumerable<IRequestTranslator> requestTranslators,
            IEnumerable<IResponseTranslator> responseTranslators,
            IEnumerable<IStreamingTranslator> streamingTranslators,
            IToolNormalizer toolNormalizer)
        {
            this.requestTranslators = requestTranslators.ToList();
            this.responseTranslators = responseTranslators.ToList();
            this.streamingTranslators = streamingTranslators.ToList();
            this.toolNormalizer = toolNormalizer;
        }

        /// <summary>
        /// Translates a canonical request using the first applicable request translator.
        /// </summary>
        /// <param name="request">The canonical request to translate.</param>
        /// <returns>The translated canonical request, or the original if no translator can handle it.</returns>
        public CanonicalRequest TranslateRequest(CanonicalRequest request)
        {
            var applicableTranslator = this.requestTranslators.FirstOrDefault(t => t.CanHandle(request));
            if (applicableTranslator != null)
            {
                return applicableTranslator.Translate(request);
            }

            return request;
        }

        /// <summary>
        /// Translates a canonical response using the first applicable response translator.
        /// </summary>
        /// <param name="response">The canonical response to translate.</param>
        /// <returns>The translated canonical response, or the normalized response if no translator can handle it.</returns>
        public CanonicalResponse TranslateResponse(CanonicalResponse response)
        {
            var normalized = this.toolNormalizer.NormalizeResponse(response);
            var applicableTranslator = this.responseTranslators.FirstOrDefault(t => t.CanHandle(normalized));
            if (applicableTranslator != null)
            {
                return applicableTranslator.Translate(normalized);
            }

            return normalized;
        }

        /// <summary>
        /// Translates a chat response update using the first applicable streaming translator.
        /// </summary>
        /// <param name="update">The chat response update to translate.</param>
        /// <returns>The translated chat response update, or the normalized update if no translator can handle it.</returns>
        public ChatResponseUpdate TranslateUpdate(ChatResponseUpdate update)
        {
            var normalized = this.toolNormalizer.NormalizeUpdate(update);
            var applicableTranslator = this.streamingTranslators.FirstOrDefault(t => t.CanHandle(normalized));
            if (applicableTranslator != null)
            {
                return applicableTranslator.Translate(normalized);
            }

            return normalized;
        }
    }
}
