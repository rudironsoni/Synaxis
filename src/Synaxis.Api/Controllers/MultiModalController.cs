// <copyright file="MultiModalController.cs" company="Synaxis">
// Copyright (c) Synaxis. All rights reserved.
// </copyright>

namespace Synaxis.Api.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Synaxis.Api.DTOs.MultiModal;

    /// <summary>
    /// Controller for multi-modal API endpoints.
    /// </summary>
    [ApiController]
    [Route("v1")]
    [Authorize]
    public class MultiModalController : ControllerBase
    {
        private readonly ILogger<MultiModalController> _logger;

        public MultiModalController(ILogger<MultiModalController> logger)
        {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Standard chat completions endpoint.
        /// </summary>
        /// <param name="request">The multi-modal chat request.</param>
        /// <returns>The chat completion response.</returns>
        [HttpPost("chat/completions")]
        [ProducesResponseType(typeof(MultiModalResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MultiModalResponse>> ChatCompletions([FromBody] MultiModalRequest request)
        {
            try
            {
                this._logger.LogInformation("Processing chat completion request for model: {Model}", request.Model ?? "default");

                // Validate request
                if (request.Messages.Count == 0)
                {
                    return this.BadRequest(new { error = "Messages are required" });
                }

                // Process the request and generate response
                var response = await this.ProcessChatCompletionAsync(request);

                return this.Ok(response);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing chat completion request");
                return this.StatusCode(500, new { error = "An error occurred while processing the request" });
            }
        }

        /// <summary>
        /// Streaming chat completions endpoint using Server-Sent Events (SSE).
        /// </summary>
        /// <param name="request">The multi-modal chat request.</param>
        /// <returns>A stream of chat completion chunks.</returns>
        [HttpPost("chat/completions/streaming")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task ChatCompletionsStreaming([FromBody] MultiModalRequest request)
        {
            this.Response.ContentType = "text/event-stream";
            this.Response.Headers.Append("Cache-Control", "no-cache");
            this.Response.Headers.Append("Connection", "keep-alive");

            try
            {
                this._logger.LogInformation("Processing streaming chat completion request for model: {Model}", request.Model ?? "default");

                // Validate request
                if (request.Messages.Count == 0)
                {
                    await this.SendSseErrorAsync("Messages are required");
                    return;
                }

                // Process and stream the response
                await this.ProcessChatCompletionStreamingAsync(request);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing streaming chat completion request");
                await this.SendSseErrorAsync("An error occurred while processing the request");
            }
        }

        /// <summary>
        /// Image analysis endpoint.
        /// </summary>
        /// <param name="request">The vision analysis request.</param>
        /// <returns>The image analysis response.</returns>
        [HttpPost("vision/analyze")]
        [ProducesResponseType(typeof(VisionAnalysisResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<VisionAnalysisResponse>> VisionAnalyze([FromBody] VisionAnalysisRequest request)
        {
            try
            {
                this._logger.LogInformation("Processing vision analysis request");

                // Validate request
                if (request.Image == null)
                {
                    return this.BadRequest(new { error = "Image is required" });
                }

                // Decode base64 image if provided
                byte[] imageData = Array.Empty<byte>();
                if (!string.IsNullOrEmpty(request.Image.Base64))
                {
                    imageData = this.DecodeBase64Image(request.Image.Base64);
                    if (imageData.Length == 0)
                    {
                        return this.BadRequest(new { error = "Invalid base64 image data" });
                    }
                }

                // Process the image analysis
                var response = await this.ProcessVisionAnalysisAsync(request, imageData);

                return this.Ok(response);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing vision analysis request");
                return this.StatusCode(500, new { error = "An error occurred while processing the request" });
            }
        }

        /// <summary>
        /// Audio transcription endpoint.
        /// </summary>
        /// <param name="request">The audio transcription request.</param>
        /// <returns>The transcription response.</returns>
        [HttpPost("audio/transcribe")]
        [ProducesResponseType(typeof(AudioTranscriptionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AudioTranscriptionResponse>> AudioTranscribe([FromBody] AudioTranscriptionRequest request)
        {
            try
            {
                this._logger.LogInformation("Processing audio transcription request");

                // Validate request
                if (request.Audio == null)
                {
                    return this.BadRequest(new { error = "Audio is required" });
                }

                // Decode base64 audio if provided
                byte[] audioData = Array.Empty<byte>();
                if (!string.IsNullOrEmpty(request.Audio.Base64))
                {
                    audioData = this.DecodeBase64Audio(request.Audio.Base64);
                    if (audioData.Length == 0)
                    {
                        return this.BadRequest(new { error = "Invalid base64 audio data" });
                    }
                }

                // Process the audio transcription
                var response = await this.ProcessAudioTranscriptionAsync(request, audioData);

                return this.Ok(response);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing audio transcription request");
                return this.StatusCode(500, new { error = "An error occurred while processing the request" });
            }
        }

        /// <summary>
        /// Text-to-speech synthesis endpoint.
        /// </summary>
        /// <param name="request">The audio synthesis request.</param>
        /// <returns>The synthesized audio.</returns>
        [HttpPost("audio/synthesize")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> AudioSynthesize([FromBody] AudioSynthesisRequest request)
        {
            try
            {
                this._logger.LogInformation("Processing audio synthesis request for voice: {Voice}", request.Voice ?? "default");

                // Validate request
                if (string.IsNullOrEmpty(request.Input))
                {
                    return this.BadRequest(new { error = "Input text is required" });
                }

                // Process the audio synthesis
                var (audioData, contentType) = await ProcessAudioSynthesisAsync(request);

                if (audioData.Length == 0)
                {
                    return this.StatusCode(500, new { error = "Failed to synthesize audio" });
                }

                return this.File(audioData, contentType);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error processing audio synthesis request");
                return this.StatusCode(500, new { error = "An error occurred while processing the request" });
            }
        }

        private async Task<MultiModalResponse> ProcessChatCompletionAsync(MultiModalRequest request)
        {
            // Simulate processing - in a real implementation, this would call an AI service
            await Task.Delay(100);

            var lastMessage = request.Messages[^1];
            var textContent = string.Join(" ", lastMessage.Content
                .Where(c => string.Equals(c.Type, "text", StringComparison.Ordinal))
                .Select(c => c.Content));

            var response = new MultiModalResponse
            {
                Model = string.IsNullOrEmpty(request.Model) ? "gpt-4-turbo" : request.Model,
                Choices = new List<Choice>
                {
                    new()
                    {
                        Index = 0,
                        Message = new ChoiceMessage
                        {
                            Role = "assistant",
                            Content = $"This is a simulated response to: {textContent}",
                        },
                        FinishReason = "stop",
                    },
                },
                Usage = new Usage
                {
                    PromptTokens = textContent.Length / 4,
                    CompletionTokens = 50,
                    TotalTokens = (textContent.Length / 4) + 50,
                },
            };

            return response;
        }

        private async Task ProcessChatCompletionStreamingAsync(MultiModalRequest request)
        {
            // Simulate streaming - in a real implementation, this would stream from an AI service
            var lastMessage = request.Messages[^1];
            var textContent = string.Join(" ", lastMessage.Content
                .Where(c => string.Equals(c.Type, "text", StringComparison.Ordinal))
                .Select(c => c.Content));

            var responseText = $"This is a simulated streaming response to: {textContent}";
            var words = responseText.Split(' ');

            for (int i = 0; i < words.Length; i++)
            {
                var chunk = new
                {
                    id = Guid.NewGuid().ToString(),
                    @object = "chat.completion.chunk",
                    created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    model = string.IsNullOrEmpty(request.Model) ? "gpt-4-turbo" : request.Model,
                    choices = new[]
                    {
                        new
                        {
                            index = 0,
                            delta = new
                            {
                                content = words[i] + " ",
                            },
                            finish_reason = i == words.Length - 1 ? "stop" : (object)null,
                        },
                    },
                };

                await this.SendSseEventAsync("data", JsonSerializer.Serialize(chunk));
                await Task.Delay(50); // Simulate streaming delay
            }

            await this.SendSseEventAsync("data", "[DONE]");
        }

        private async Task<VisionAnalysisResponse> ProcessVisionAnalysisAsync(VisionAnalysisRequest request, byte[] imageData)
        {
            _ = imageData; // Suppress unused parameter warning

            // Simulate processing - in a real implementation, this would call a vision AI service
            await Task.Delay(200);

            var response = new VisionAnalysisResponse
            {
                Model = string.IsNullOrEmpty(request.Model) ? "gpt-4-vision-preview" : request.Model,
                Content = $"This is a simulated analysis of the image. Prompt: {request.Prompt}",
                Usage = new Usage
                {
                    PromptTokens = request.Prompt.Length / 4,
                    CompletionTokens = 100,
                    TotalTokens = (request.Prompt.Length / 4) + 100,
                },
            };

            return response;
        }

        private async Task<AudioTranscriptionResponse> ProcessAudioTranscriptionAsync(AudioTranscriptionRequest request, byte[] audioData)
        {
            // Simulate processing - in a real implementation, this would call a transcription service
            await Task.Delay(300);

            var response = new AudioTranscriptionResponse
            {
                Text = "This is a simulated transcription of the audio.",
                Task = "transcribe",
                Language = string.IsNullOrEmpty(request.Language) ? "en" : request.Language,
                Duration = audioData.Length > 0 ? 10.5 : 0.0,
            };

            return response;
        }

        private static async Task<(byte[] AudioData, string ContentType)> ProcessAudioSynthesisAsync(AudioSynthesisRequest request)
        {
            // Simulate processing - in a real implementation, this would call a TTS service
            await Task.Delay(200);

            // Return a dummy audio file (in reality, this would be actual synthesized audio)
            var dummyAudio = Encoding.UTF8.GetBytes($"Simulated audio for: {request.Input}");
            var contentType = GetContentType(request.ResponseFormat);

            return (dummyAudio.Length > 0 ? dummyAudio : Array.Empty<byte>(), contentType);
        }

        private byte[] DecodeBase64Image(string base64Data)
        {
            try
            {
                // Remove data URL prefix if present
                var base64String = base64Data;
                if (base64Data.Contains(","))
                {
                    base64String = base64Data.Split(',')[1];
                }

                return Convert.FromBase64String(base64String);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error decoding base64 image");
                return Array.Empty<byte>();
            }
        }

        private byte[] DecodeBase64Audio(string base64Data)
        {
            try
            {
                // Remove data URL prefix if present
                var base64String = base64Data;
                if (base64Data.Contains(","))
                {
                    base64String = base64Data.Split(',')[1];
                }

                return Convert.FromBase64String(base64String);
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error decoding base64 audio");
                return Array.Empty<byte>();
            }
        }

        private static string GetContentType(string format)
        {
            return format.ToLowerInvariant() switch
            {
                "mp3" => "audio/mpeg",
                "opus" => "audio/opus",
                "aac" => "audio/aac",
                "flac" => "audio/flac",
                "wav" => "audio/wav",
                "pcm" => "audio/pcm",
                _ => "audio/mpeg",
            };
        }

        private async Task SendSseEventAsync(string eventType, string data)
        {
            await this.Response.WriteAsync($"{eventType}: {data}\n\n");
            await this.Response.Body.FlushAsync();
        }

        private Task SendSseErrorAsync(string error)
        {
            var errorData = JsonSerializer.Serialize(new { error });
            return this.SendSseEventAsync("data", errorData);
        }
    }
}
