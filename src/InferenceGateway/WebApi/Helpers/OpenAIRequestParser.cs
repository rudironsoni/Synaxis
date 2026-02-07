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
            if (context == null) return null;

            // Resolve configured max request body size from DI. Fall back to 10 MB if not available.
            long MaxBodySize = 10L * 1024 * 1024; // 10 MB default
            try
            {
                var opts = context.RequestServices.GetService(typeof(Microsoft.Extensions.Options.IOptions<Synaxis.InferenceGateway.Application.Configuration.SynaxisConfiguration>)) as Microsoft.Extensions.Options.IOptions<Synaxis.InferenceGateway.Application.Configuration.SynaxisConfiguration>;
                if (opts?.Value != null)
                {
                    MaxBodySize = opts.Value.MaxRequestBodySize;
                }
            }
            catch
            {
                // swallow - use default
            }

            // If the client provided a Content-Length header, enforce it immediately.
            var contentLength = context.Request.ContentLength;
            if (contentLength.HasValue && contentLength.Value > MaxBodySize)
            {
                throw new BadHttpRequestException($"Request body too large. Limit is {MaxBodySize} bytes.");
            }

            // Enable buffering so we can read and then rewind for downstream middleware.
            context.Request.EnableBuffering();
            context.Request.Body.Position = 0; // Rewind just in case

            string body;

            if (contentLength.HasValue)
            {
                // Content-Length is known and already checked against the limit above.
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                body = await reader.ReadToEndAsync(cancellationToken);
            }
            else
            {
                // Content-Length is unknown. Read the stream in bounded chunks to avoid OOM/DoS.
                using var ms = new MemoryStream();
                var buffer = new byte[8192];
                int bytesRead;
                long totalRead = 0;

                while ((bytesRead = await context.Request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    totalRead += bytesRead;
                    if (totalRead > MaxBodySize)
                    {
                        // Reset position for downstream just in case, then reject.
                        context.Request.Body.Position = 0;
                        throw new BadHttpRequestException($"Request body too large. Limit is {MaxBodySize} bytes.");
                    }

                    ms.Write(buffer, 0, bytesRead);
                }

                ms.Position = 0;
                using var reader = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                body = await reader.ReadToEndAsync(cancellationToken);
            }

            // Reset the request body position so subsequent middleware can read it.
            context.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body)) return null;

            try
            {
                var request = JsonSerializer.Deserialize<OpenAIRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (request != null)
                {
                    // Special case: if the body is an empty JSON object "{}", allow it to pass validation
                    // and return the request with empty Model and Messages.
                    if (body.Trim() != "{}")
                    {
                        // Handle special cases based on endpoint requirements
                        bool isEmptyModel = string.IsNullOrEmpty(request.Model);
                        bool isMissingMessages = request.Messages == null || request.Messages.Count == 0;

                        bool shouldSkipValidation = (isEmptyModel && allowEmptyModel) || (isMissingMessages && allowEmptyMessages);

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
                }

                return request;
            }
            catch (JsonException ex)
            {
                // If the body is invalid JSON for an OpenAI request, throw a BadHttpRequestException
                // so middleware can convert it to a 400. Do not swallow other exceptions (OOM, etc.).
                throw new BadHttpRequestException("Invalid JSON body", ex);
            }
        }

        private static List<string> ValidateRequest(OpenAIRequest request)
        {
            var errors = new List<string>();
            var validationContext = new ValidationContext(request);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
            {
                foreach (var result in validationResults)
                {
                    if (result != null)
                    {
                        var fieldName = string.Join(", ", result.MemberNames);
                        var errorMessage = result.ErrorMessage ?? "Validation failed";
                        errors.Add(!string.IsNullOrEmpty(fieldName) ? $"{fieldName}: {errorMessage}" : errorMessage);
                    }
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
                        foreach (var result in messageResults)
                        {
                            if (result != null)
                            {
                                var fieldName = string.Join(", ", result.MemberNames);
                                var errorMessage = result.ErrorMessage ?? "Validation failed";
                                errors.Add($"messages[{i}].{fieldName}: {errorMessage}");
                            }
                        }
                    }
                }
            }

            return errors;
        }
    }
}
