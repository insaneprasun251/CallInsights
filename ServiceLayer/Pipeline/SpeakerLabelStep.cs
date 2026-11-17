using Core.BusinessObject;
using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.Options;
using Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceLayer.Pipeline
{
    /// <summary>
    /// Decides who said each transcript line (Agent or Customer) and merges the lines into speaker turns.
    /// Stereo recordings already know it from the channel. Mono recordings are diarized on the hosted GPU server.
    /// </summary>
    public sealed class SpeakerLabelStep : IPipelineStep
    {
        private readonly AudioOptions _audio;
        private readonly StorageOptions _storage;
        private readonly TranscriptionOptions _server;
        private readonly DiarizationOptions _options;
        private readonly HttpClient _http;
        private readonly ILogger<SpeakerLabelStep> _logger;

        public SpeakerLabelStep(
            IOptions<AudioOptions> audio,
            IOptions<StorageOptions> storage,
            IOptions<TranscriptionOptions> server,
            IOptions<DiarizationOptions> options,
            HttpClient http,
            ILogger<SpeakerLabelStep> logger)
        {
            _audio = audio.Value;
            _storage = storage.Value;
            _server = server.Value;
            _options = options.Value;
            _http = http;
            _logger = logger;
        }

        public CallStatus Status => CallStatus.LabellingSpeakers;
        public string Name => ProcessingSteps.LabellingSpeakers;

        public async Task ExecuteAsync(Call call, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_storage.TranscriptionsFolder))
                throw new InvalidOperationException(ErrorMessages.SettingMissing("Storage:TranscriptionsFolder"));

            if (string.IsNullOrWhiteSpace(_audio.WorkFolder))
                throw new InvalidOperationException(ErrorMessages.SettingMissing("Audio:WorkFolder"));

            var watch = Stopwatch.StartNew();
            var path = TranscriptionFileHelper.GetCallFilePath(_storage.TranscriptionsFolder, call.Id);
            var document = await TranscriptionFileHelper.ReadAsync(path, cancellationToken)
                ?? throw new InvalidOperationException(ErrorMessages.TranscriptionNotFound);

            var folder = WorkFolderHelper.GetCallFolder(_audio.WorkFolder, call.Id);
            var splitByChannel = File.Exists(Path.Combine(folder, WorkFileNames.Agent))
                              && File.Exists(Path.Combine(folder, WorkFileNames.Customer));

            string details;
            if (splitByChannel)
            {
                // the transcription step already labelled every line from its channel
                call.SpeakerMethod = SpeakerMethod.Channel;
                document.Diarization = null;
                details = "method=channel";
            }
            else
            {
                call.SpeakerMethod = SpeakerMethod.Diarized;
                details = await DiarizeAsync(document, folder, cancellationToken);
            }

            document.Turns = SpeakerHelper.BuildTurns(document.Segments);
            await TranscriptionFileHelper.WriteAsync(path, document, cancellationToken);
            watch.Stop();

            var message = $"{details}; lines={document.Segments.Count}; turns={document.Turns.Count}; took={watch.Elapsed.TotalSeconds:F1}s";
            call.AddEvent(ProcessingSteps.SpeakerDetails, message.Truncate(FieldLengths.EventMessage));
            _logger.LogInformation("Speakers labelled for call {CallId}: {Details}", call.Id, message);
        }

        private async Task<string> DiarizeAsync(TranscriptionDocument document, string folder, CancellationToken cancellationToken)
        {
            if (document.Segments.Count == 0)
            {
                document.Diarization = null;
                return "method=diarization; skipped (no transcript lines)";
            }

            if (string.IsNullOrWhiteSpace(_server.ApiKey))
                throw new InvalidOperationException(ErrorMessages.TranscriptionApiKeyMissing);

            var monoPath = Path.Combine(folder, WorkFileNames.Mono);
            if (!File.Exists(monoPath))
                throw new InvalidOperationException(ErrorMessages.NoAudioToTranscribe);

            if (new FileInfo(monoPath).Length == 0)
                throw new InvalidOperationException(ErrorMessages.EmptyAudioFile);

            _logger.LogInformation("Sending {FileName} to hosted speaker identification", Path.GetFileName(monoPath));
            var result = await DiarizeFileAsync(monoPath, cancellationToken);

            var turns = result.Response.Segments!
                .Where(s => !string.IsNullOrWhiteSpace(s.Speaker) && s.End > s.Start)
                .Select(s => new DiarizedTurn
                {
                    Speaker = s.Speaker!,
                    StartMs = ToMilliseconds(s.Start),
                    EndMs = ToMilliseconds(s.End)
                })
                .ToList();

            var summary = SpeakerHelper.Assign(document.Segments, turns, _options.AgentSpeaksFirst);
            document.Diarization = result.Raw;

            return $"method=diarization; speakers={summary.DiarizedSpeakers}; unlabelled={summary.UnlabelledLines}; " +
                   $"inference={result.InferenceSeconds?.ToString("F1", CultureInfo.InvariantCulture) ?? "unknown"}s";
        }

        private async Task<(DiarizationResponse Response, JsonElement Raw, double? InferenceSeconds)> DiarizeFileAsync(string path, CancellationToken cancellationToken)
        {
            await using var stream = File.OpenRead(path);
            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(fileContent, "file", Path.GetFileName(path));

            if (_options.NumSpeakers > 0)
                form.Add(new StringContent(_options.NumSpeakers.ToString(CultureInfo.InvariantCulture)), "num_speakers");

            using var request = new HttpRequestMessage(HttpMethod.Post, "diarize") { Content = form };
            request.Headers.Add("X-API-Key", _server.ApiKey);
            request.Headers.Add("ngrok-skip-browser-warning", "true");

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Hosted speaker identification returned HTTP {StatusCode}: {Error}", (int)response.StatusCode, error.Length > 2000 ? error[..2000] : error);
                throw new HttpRequestException(ErrorMessages.DiarizationServerRequestFailed, null, response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            JsonElement raw;
            DiarizationResponse? parsed;
            try
            {
                using var document = JsonDocument.Parse(json);
                raw = document.RootElement.Clone();
                parsed = raw.Deserialize<DiarizationResponse>();
            }
            catch (JsonException)
            {
                throw new InvalidOperationException(ErrorMessages.InvalidDiarizationResponse);
            }

            if (parsed?.Segments is null)
                throw new InvalidOperationException(ErrorMessages.InvalidDiarizationResponse);

            double? inferenceSeconds = null;
            if (response.Headers.TryGetValues("X-Inference-Seconds", out var values) &&
                double.TryParse(values.FirstOrDefault(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                inferenceSeconds = seconds;

            return (parsed, raw, inferenceSeconds);
        }

        private static int ToMilliseconds(double seconds)
        {
            if (!double.IsFinite(seconds) || seconds <= 0)
                return 0;

            var milliseconds = seconds * 1000d;
            return milliseconds >= int.MaxValue ? int.MaxValue : (int)Math.Round(milliseconds);
        }

        private sealed class DiarizationResponse
        {
            [JsonPropertyName("segments")]
            public List<DiarizationSegment>? Segments { get; set; }
        }

        private sealed class DiarizationSegment
        {
            [JsonPropertyName("speaker")]
            public string? Speaker { get; set; }

            [JsonPropertyName("start")]
            public double Start { get; set; }

            [JsonPropertyName("end")]
            public double End { get; set; }
        }
    }
}
