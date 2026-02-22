// <copyright file="SseOutputFormatter.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Transport.Http.Formatters
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.Net.Http.Headers;

    /// <summary>
    /// Output formatter for Server-Sent Events (SSE) streaming responses.
    /// Handles IAsyncEnumerable of string and writes each item as a raw SSE event.
    /// </summary>
    public class SseOutputFormatter : TextOutputFormatter
    {
        /// <summary>
        /// The content type for Server-Sent Events.
        /// </summary>
        public const string SseContentType = "text/event-stream";

        /// <summary>
        /// Initializes a new instance of the <see cref="SseOutputFormatter"/> class.
        /// </summary>
        public SseOutputFormatter()
        {
            this.SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse(SseContentType));
            this.SupportedEncodings.Add(Encoding.UTF8);
        }

        /// <inheritdoc />
        protected override bool CanWriteType(Type? type)
        {
            if (type == null)
            {
                return false;
            }

            // Check if the type is or implements IAsyncEnumerable of string.
            return IsAsyncEnumerableOfString(type);
        }

        private static bool IsAsyncEnumerableOfString(Type type)
        {
            // Direct match: IAsyncEnumerable<string>
            if (type == typeof(IAsyncEnumerable<string>))
            {
                return true;
            }

            // Check if type implements IAsyncEnumerable<string>
            foreach (var interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType)
                {
                    var genericDef = interfaceType.GetGenericTypeDefinition();
                    if (genericDef == typeof(IAsyncEnumerable<>))
                    {
                        var typeArgs = interfaceType.GetGenericArguments();
                        if (typeArgs.Length == 1 && typeArgs[0] == typeof(string))
                        {
                            return true;
                        }
                    }
                }
            }

            // Also check the type itself if it's a generic type definition
            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(IAsyncEnumerable<>))
                {
                    var typeArgs = type.GetGenericArguments();
                    if (typeArgs.Length == 1 && typeArgs[0] == typeof(string))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc />
        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            ArgumentNullException.ThrowIfNull(context);
            if (context.Object is not IAsyncEnumerable<string> asyncEnumerable)
            {
                return;
            }

            var response = context.HttpContext.Response;
            response.ContentType = SseContentType;

            // Ensure headers are sent immediately for streaming
            await response.Body.FlushAsync().ConfigureAwait(false);

            await foreach (var sseEvent in asyncEnumerable.WithCancellation(context.HttpContext.RequestAborted).ConfigureAwait(false))
            {
                await response.WriteAsync(sseEvent, selectedEncoding).ConfigureAwait(false);
                await response.Body.FlushAsync().ConfigureAwait(false);
            }
        }
    }
}
