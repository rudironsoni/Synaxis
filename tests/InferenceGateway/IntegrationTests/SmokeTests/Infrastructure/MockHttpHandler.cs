using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Synaxis.InferenceGateway.IntegrationTests.SmokeTests.Infrastructure
{
    /// <summary>
    /// Mock HTTP handler that intercepts requests to the Synaxis gateway endpoints
    /// and returns deterministic mock responses instead of hitting real providers.
    /// This replaces the real HTTP calls with mock data to eliminate flakiness.
    /// </summary>
    public class MockHttpHandler : HttpMessageHandler
    {
        private readonly MockProviderResponses _responses;

        public MockHttpHandler(MockProviderResponses? responses = null)
        {
            this._responses = responses ?? new MockProviderResponses();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var requestUri = request.RequestUri?.ToString() ?? "";
            var requestMethod = request.Method.Method;

            // Log the mock request for debugging
            Console.WriteLine($"[MOCK] {requestMethod} {requestUri}");

            // Handle chat completions endpoint
            if (requestUri.Contains("/openai/v1/chat/completions") && requestMethod == "POST")
            {
                return await this.HandleChatCompletionsRequest(request, cancellationToken);
            }

            // Handle legacy completions endpoint
            if (requestUri.Contains("/openai/v1/completions") && requestMethod == "POST")
            {
                return await this.HandleLegacyCompletionsRequest(request, cancellationToken);
            }

            // Handle models endpoint
            if (requestUri.Contains("/openai/v1/models") && requestMethod == "GET")
            {
                return this.HandleModelsRequest();
            }

            // Return 404 for unhandled endpoints
            return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
            {
                Content = new StringContent("{\"error\": {\"message\": \"Endpoint not found in mock\", \"type\": \"invalid_request_error\"}}"),
            };
        }

        private async Task<HttpResponseMessage> HandleChatCompletionsRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var content = request.Content != null
                    ? await request.Content.ReadAsStringAsync(cancellationToken)
                    : "";
                if (string.IsNullOrEmpty(content))
                {
                    return CreateErrorResponse(System.Net.HttpStatusCode.BadRequest, "Empty request body");
                }

                var model = ExtractModelFromChatRequest(content);
                var mockResponse = this._responses.GetChatCompletionResponse(model);

                if (IsStreamingRequest(content))
                {
                    return CreateStreamingResponse(mockResponse);
                }
                else
                {
                    return CreateJsonResponse(mockResponse, System.Net.HttpStatusCode.OK);
                }
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError,
                    $"Mock processing error: {ex.Message}");
            }
        }

        private async Task<HttpResponseMessage> HandleLegacyCompletionsRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var content = request.Content != null
                    ? await request.Content.ReadAsStringAsync(cancellationToken)
                    : "";
                if (string.IsNullOrEmpty(content))
                {
                    return CreateErrorResponse(System.Net.HttpStatusCode.BadRequest, "Empty request body");
                }

                var model = ExtractModelFromLegacyRequest(content);
                var mockResponse = this._responses.GetLegacyCompletionResponse(model);

                if (IsStreamingRequest(content))
                {
                    return CreateStreamingResponse(mockResponse);
                }
                else
                {
                    return CreateJsonResponse(mockResponse, System.Net.HttpStatusCode.OK);
                }
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(System.Net.HttpStatusCode.InternalServerError,
                    $"Mock processing error: {ex.Message}");
            }
        }

        private HttpResponseMessage HandleModelsRequest()
        {
            var models = this._responses.GetAvailableModels();
            return CreateJsonResponse(models, System.Net.HttpStatusCode.OK);
        }

        private static string ExtractModelFromChatRequest(string content)
        {
            // Better JSON parsing to extract model from request
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("model", out var modelElement))
                {
                    return modelElement.GetString() ?? "mock-model";
                }
            }
            catch
            {
                // Fallback to simple parsing if JSON.NET fails
            }

            // Simple regex fallback for "model": "value" pattern
            var match = System.Text.RegularExpressions.Regex.Match(content, "\"model\"\\s*:\\s*\"([^\"]+)\"");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return "mock-model";
        }

        private static string ExtractModelFromLegacyRequest(string content)
        {
            return ExtractModelFromChatRequest(content);
        }

        private static bool IsStreamingRequest(string content)
        {
            return content.Contains("\"stream\":true") || content.Contains("\"stream\": true");
        }

        private static HttpResponseMessage CreateJsonResponse(object content, System.Net.HttpStatusCode statusCode)
        {
            var response = new HttpResponseMessage(statusCode);
            response.Content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(content),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            return response;
        }

        private static HttpResponseMessage CreateStreamingResponse(object content)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(content);

            // Create SSE response format
            var sseContent = $"data: {jsonContent}\n\n";

            response.Content = new StringContent(sseContent, System.Text.Encoding.UTF8, "text/event-stream");
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");

            return response;
        }

        private static HttpResponseMessage CreateErrorResponse(System.Net.HttpStatusCode statusCode, string message)
        {
            var errorContent = System.Text.Json.JsonSerializer.Serialize(new
            {
                error = new
                {
                    message = message,
                    type = "mock_error",
                    code = "mock_error_code"
                },
            });

            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(errorContent, System.Text.Encoding.UTF8, "application/json"),
            };
        }
    }
}
