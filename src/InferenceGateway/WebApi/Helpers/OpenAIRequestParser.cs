// <copyright file="OpenAIRequestParser.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.InferenceGateway.WebApi.Helpers
{
    using System.ComponentModel.DataAnnotations;
    using System.Text;
    using System.Text.Json;
    using Microsoft.AspNetCore.Http;
    using Synaxis.InferenceGateway.Application.Translation;

    /// <summary>
    /// Parser for OpenAI API requests.
    /// </summary>
    public static class OpenAIRequestParser
    {
        private static readonly JsonSerializerOptions RequestSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// Parses an OpenAI request from the HTTP context.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="allowEmptyModel">Whether to allow empty model field.</param>
        /// <param name="allowEmptyMessages">Whether to allow empty messages field.</param>
        /// <returns>The parsed OpenAI request, or null if parsing failed.</returns>
        public static async Task<OpenAIRequest?> ParseAsync(HttpContext? context, CancellationToken cancellationToken = default, bool allowEmptyModel = false, bool allowEmptyMessages = false)
        {
            if (context == null)
            {
                return null;
            }

            var maxBodySize = ResolveMaxBodySize(context);
            var body = await ReadRequestBodyAsync(context, maxBodySize, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            return DeserializeAndValidate(body, allowEmptyModel, allowEmptyMessages);
        }

        private static long ResolveMaxBodySize(HttpContext context)
        {
            long maxBodySize = Synaxis.InferenceGateway.Application.Configuration.SynaxisConfiguration.DefaultMaxRequestBodySize;
            try
            {
                var opts = context.RequestServices.GetService(typeof(Microsoft.Extensions.Options.IOptions<Synaxis.InferenceGateway.Application.Configuration.SynaxisConfiguration>))
                    as Microsoft.Extensions.Options.IOptions<Synaxis.InferenceGateway.Application.Configuration.SynaxisConfiguration>;
                if (opts?.Value != null)
                {
                    maxBodySize = opts.Value.MaxRequestBodySize;
                }
            }
            catch
            {
                // swallow - use default
            }

            return maxBodySize;
        }

        private static async Task<string> ReadRequestBodyAsync(HttpContext context, long maxBodySize, CancellationToken cancellationToken)
        {
            var contentLength = context.Request.ContentLength;
            if (contentLength.HasValue && contentLength.Value > maxBodySize)
            {
                throw new BadHttpRequestException($"Request body too large. Limit is {maxBodySize} bytes.");
            }

            context.Request.EnableBuffering();
            context.Request.Body.Position = 0;

            var body = contentLength.HasValue
                ? await ReadKnownLengthBodyAsync(context, cancellationToken).ConfigureAwait(false)
                : await ReadUnknownLengthBodyAsync(context, maxBodySize, cancellationToken).ConfigureAwait(false);

            context.Request.Body.Position = 0;
            return body;
        }

        private static async Task<string> ReadKnownLengthBodyAsync(HttpContext context, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task<string> ReadUnknownLengthBodyAsync(HttpContext context, long maxBodySize, CancellationToken cancellationToken)
        {
            using var ms = new MemoryStream();
            var buffer = new byte[8192];
            int bytesRead;
            long totalRead = 0;

            while ((bytesRead = await context.Request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
            {
                totalRead += bytesRead;
                if (totalRead > maxBodySize)
                {
                    context.Request.Body.Position = 0;
                    throw new BadHttpRequestException($"Request body too large. Limit is {maxBodySize} bytes.");
                }

                await ms.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
            }

            ms.Position = 0;
            using var reader = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        private static OpenAIRequest? DeserializeAndValidate(string body, bool allowEmptyModel, bool allowEmptyMessages)
        {
            try
            {
                var request = JsonSerializer.Deserialize<OpenAIRequest>(body, RequestSerializerOptions);

                if (request != null && !string.Equals(body.Trim(), "{}", StringComparison.Ordinal))
                {
                    var shouldSkipValidation = ShouldSkipValidation(request, allowEmptyModel, allowEmptyMessages);
                    if (!shouldSkipValidation)
                    {
                        var validationErrors = ValidateRequest(request);
                        if (validationErrors.Count > 0)
                        {
                            var errorMessage = string.Join("; ", validationErrors);
                            throw new BadHttpRequestException($"Invalid request: {errorMessage}");
                        }
                    }
                }

                return request;
            }
            catch (JsonException ex)
            {
                throw new BadHttpRequestException("Invalid JSON body", ex);
            }
        }

        private static bool ShouldSkipValidation(OpenAIRequest request, bool allowEmptyModel, bool allowEmptyMessages)
        {
            var isEmptyModel = string.IsNullOrEmpty(request.Model);
            var isMissingMessages = request.Messages == null || request.Messages.Count == 0;

            return (isEmptyModel && allowEmptyModel) || (isMissingMessages && allowEmptyMessages);
        }

        private static List<string> ValidateRequest(OpenAIRequest request)
        {
            var errors = new List<string>();
            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
            {
                foreach (var result in validationResults.Where(result => result != null))
                {
                    var fieldName = string.Join(", ", result!.MemberNames);
                    var errorMessage = result.ErrorMessage ?? "Validation failed";
                    errors.Add(!string.IsNullOrEmpty(fieldName) ? $"{fieldName}: {errorMessage}" : errorMessage);
                }
            }

            // Validate nested objects (messages)
            if (request.Messages != null)
            {
                for (int i = 0; i < request.Messages.Count; i++)
                {
                    var message = request.Messages[i];
                    var messageContext = new ValidationContext(message);
                    var messageResults = new List<ValidationResult>();

                    if (!Validator.TryValidateObject(message, messageContext, messageResults, validateAllProperties: true))
                    {
                        foreach (var result in messageResults.Where(result => result != null))
                        {
                            var fieldName = string.Join(", ", result!.MemberNames);
                            var errorMessage = result.ErrorMessage ?? "Validation failed";
                            errors.Add($"messages[{i}].{fieldName}: {errorMessage}");
                        }
                    }
                }
            }

            return errors;
        }
    }
}
