using Core.BusinessObject;
using Core.Constants;
using Core.Enums;
using Core.Options;
using Core.Services;
using DataLayer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Core.Helpers;

namespace ServiceLayer.Pipeline
{
    /// <summary>
    /// Sends the prompt of the call to the hosted language model and saves its answer with the prompt.
    /// </summary>
    public sealed class InsightsStep : IPipelineStep
    {
        private readonly AppDbContext _db;
        private readonly TranscriptionOptions _server;
        private readonly LlmOptions _options;
        private readonly HttpClient _http;
        private readonly ILogger<InsightsStep> _logger;

        public InsightsStep(AppDbContext db, IOptions<TranscriptionOptions> server, IOptions<LlmOptions> options, HttpClient http, ILogger<InsightsStep> logger)
        {
            _db = db;
            _server = server.Value;
            _options = options.Value;
            _http = http;
            _logger = logger;
        }

        public CallStatus Status => CallStatus.GeneratingInsights;
        public string Name => ProcessingSteps.GeneratingInsights;

        public async Task ExecuteAsync(Call call, CancellationToken cancellationToken)
        {
            var prompts = await _db.CallPrompts.Where(p => p.CallId == call.Id).ToListAsync(cancellationToken);
            if (prompts.Count == 0)
            {
                call.AddEvent(ProcessingSteps.InsightsDetails, "skipped (no prompt)");
                return;
            }

            if (string.IsNullOrWhiteSpace(_server.ApiKey))
                throw new InvalidOperationException(ErrorMessages.TranscriptionApiKeyMissing);

            foreach (var prompt in prompts)
            {
                _logger.LogInformation("Sending prompt {TemplateKey} {TemplateVersion} of call {CallId} to the hosted language model", prompt.TemplateKey, prompt.TemplateVersion, call.Id);

                var watch = Stopwatch.StartNew();
                var answer = await AskAsync(prompt.PromptText, cancellationToken);
                watch.Stop();

                prompt.ResponseText = answer.Text;
                prompt.ResponseModel = answer.Model?.Truncate(FieldLengths.ModelName);
                prompt.RespondedAtUtc = DateTime.UtcNow;

                var message = $"template={prompt.TemplateKey} {prompt.TemplateVersion}; model={answer.Model ?? "unknown"}; " +
                              $"tokens={answer.Tokens?.ToString(CultureInfo.InvariantCulture) ?? "unknown"}; took={watch.Elapsed.TotalSeconds:F1}s";
                call.AddEvent(ProcessingSteps.InsightsDetails, message.Truncate(FieldLengths.EventMessage));
                _logger.LogInformation("Insights generated for call {CallId}: {Details}", call.Id, message);
            }
        }

        private async Task<(string Text, string? Model, int? Tokens)> AskAsync(string prompt, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "llm")
            {
                Content = JsonContent.Create(new LlmRequest
                {
                    Prompt = prompt,
                    Json = true,
                    Temperature = _options.Temperature,
                    MaxTokens = _options.MaxTokens
                })
            };
            request.Headers.Add("X-API-Key", _server.ApiKey);
            request.Headers.Add("ngrok-skip-browser-warning", "true");

            using var response = await _http.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Hosted language model returned HTTP {StatusCode}: {Error}", (int)response.StatusCode, error.Length > 2000 ? error[..2000] : error);
                throw new HttpRequestException(ErrorMessages.LlmServerRequestFailed, null, response.StatusCode);
            }

            LlmResponse? parsed;
            try
            {
                parsed = await response.Content.ReadFromJsonAsync<LlmResponse>(cancellationToken);
            }
            catch (JsonException)
            {
                throw new InvalidOperationException(ErrorMessages.InvalidLlmResponse);
            }

            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Response))
                throw new InvalidOperationException(ErrorMessages.InvalidLlmResponse);

            // the model stopped because it ran out of tokens, so the JSON would be cut off
            if (string.Equals(parsed.DoneReason, "length", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(ErrorMessages.LlmAnswerCutOff);

            if (!IsJsonObject(parsed.Response))
                throw new InvalidOperationException(ErrorMessages.InvalidLlmResponse);

            return (parsed.Response.Trim(), parsed.Model, parsed.EvalCount);
        }

        // the prompt asks for one JSON object, so anything else is not an answer we can use
        private static bool IsJsonObject(string text)
        {
            try
            {
                using var document = JsonDocument.Parse(text);
                return document.RootElement.ValueKind == JsonValueKind.Object;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private sealed class LlmRequest
        {
            [JsonPropertyName("prompt")]
            public string Prompt { get; set; } = string.Empty;

            [JsonPropertyName("json")]
            public bool Json { get; set; }

            [JsonPropertyName("temperature")]
            public double Temperature { get; set; }

            [JsonPropertyName("max_tokens")]
            public int MaxTokens { get; set; }
        }

        private sealed class LlmResponse
        {
            [JsonPropertyName("model")]
            public string? Model { get; set; }

            [JsonPropertyName("response")]
            public string? Response { get; set; }

            [JsonPropertyName("done_reason")]
            public string? DoneReason { get; set; }

            [JsonPropertyName("eval_count")]
            public int? EvalCount { get; set; }
        }
    }
}
