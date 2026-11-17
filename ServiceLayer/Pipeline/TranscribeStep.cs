using Core.BusinessObject;
using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.Options;
using Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceLayer.Pipeline
{
    public sealed class TranscribeStep : IPipelineStep
    {
        private readonly AudioOptions _audio;
        private readonly StorageOptions _storage;
        private readonly TranscriptionOptions _options;
        private readonly HttpClient _http;
        private readonly ILogger<TranscribeStep> _logger;

        public TranscribeStep(IOptions<AudioOptions> audio, IOptions<StorageOptions> storage, IOptions<TranscriptionOptions> options, HttpClient http, ILogger<TranscribeStep> logger)
        {
            _audio = audio.Value;
            _storage = storage.Value;
            _options = options.Value;
            _http = http;
            _logger = logger;
        }

        public CallStatus Status => CallStatus.Transcribing;
        public string Name => ProcessingSteps.Transcribing;

        public async Task ExecuteAsync(Call call, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException(ErrorMessages.TranscriptionApiKeyMissing);

            if (string.IsNullOrWhiteSpace(_storage.TranscriptionsFolder))
                throw new InvalidOperationException(ErrorMessages.SettingMissing("Storage:TranscriptionsFolder"));

            var folder = WorkFolderHelper.GetCallFolder(_audio.WorkFolder, call.Id);
            var monoPath = Path.Combine(folder, WorkFileNames.Mono);
            var agentPath = Path.Combine(folder, WorkFileNames.Agent);
            var customerPath = Path.Combine(folder, WorkFileNames.Customer);
            var inputs = new List<(string Path, SpeakerType Speaker)>();

            if (File.Exists(agentPath) && File.Exists(customerPath))
            {
                inputs.Add((agentPath, SpeakerType.Agent));
                inputs.Add((customerPath, SpeakerType.Customer));
                call.SpeakerMethod = SpeakerMethod.Channel;
            }
            else if (File.Exists(monoPath))
            {
                inputs.Add((monoPath, SpeakerType.Unknown));
            }
            else
            {
                throw new InvalidOperationException(ErrorMessages.NoAudioToTranscribe);
            }

            var watch = Stopwatch.StartNew();
            var segments = new List<TranscriptionSegmentData>();
            var providerResponses = new List<TranscriptionProviderResponse>();
            string? detectedLanguage = null;
            double? totalDuration = null;
            double? inferenceSeconds = null;

            foreach (var (path, speaker) in inputs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (new FileInfo(path).Length == 0)
                    throw new InvalidOperationException(ErrorMessages.EmptyAudioFile);

                _logger.LogInformation("Sending {FileName} to hosted Whisper", Path.GetFileName(path));
                var result = await TranscribeFileAsync(path, cancellationToken);
                detectedLanguage ??= result.Language;
                totalDuration = Math.Max(totalDuration ?? 0, result.Duration ?? 0);
                inferenceSeconds = (inferenceSeconds ?? 0) + (result.InferenceSeconds ?? 0);

                providerResponses.Add(new TranscriptionProviderResponse
                {
                    SourceFile = Path.GetFileName(path),
                    Response = JsonSerializer.SerializeToElement(result, JsonOptions)
                });

                foreach (var segment in result.Segments)
                {
                    var text = segment.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(text) || !text.Any(char.IsLetterOrDigit))
                        continue;

                    var startMs = ToMilliseconds(segment.Start);
                    var endMs = Math.Max(startMs, ToMilliseconds(segment.End));
                    segments.Add(new TranscriptionSegmentData
                    {
                        Speaker = speaker.ToString(),
                        StartMs = startMs,
                        EndMs = endMs,
                        Text = text
                    });
                }

                _logger.LogInformation("Received {Count} usable segments from {FileName}", segments.Count, Path.GetFileName(path));
            }

            watch.Stop();
            segments = segments.OrderBy(s => s.StartMs).ToList();

            for (var i = 0; i < segments.Count; i++)
                segments[i].SequenceNo = i + 1;

            var document = new TranscriptionDocument
            {
                CallId = call.Id,
                Language = detectedLanguage,
                DurationSeconds = call.DurationSeconds ?? totalDuration,
                ProviderResponses = providerResponses,
                Segments = segments
            };

            var outputPath = TranscriptionFileHelper.GetCallFilePath(_storage.TranscriptionsFolder, call.Id);
            var outputFolder = Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException("Invalid transcription output path.");
            Directory.CreateDirectory(outputFolder);
            var temporaryPath = outputPath + ".tmp";

            try
            {
                await using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
                    await stream.FlushAsync(cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                File.Move(temporaryPath, outputPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }

            call.Language = detectedLanguage?.Truncate(FieldLengths.Language);
            var audioSeconds = call.DurationSeconds ?? totalDuration ?? 0;
            var speed = audioSeconds > 0 ? watch.Elapsed.TotalSeconds / audioSeconds : 0;

            call.AddEvent(ProcessingSteps.TranscriptionDetails,
                $"provider=hosted-whisper; language={detectedLanguage ?? "unknown"}; " +
                $"audio={audioSeconds:F0}s; took={watch.Elapsed.TotalSeconds:F1}s; " +
                $"inference={inferenceSeconds?.ToString("F1", CultureInfo.InvariantCulture) ?? "unknown"}s; " +
                $"speed={speed:F3}x realtime; segments={segments.Count}");

            _logger.LogInformation("Hosted transcription saved for call {CallId}: {SegmentCount} segments in {Elapsed:F1}s", call.Id, segments.Count, watch.Elapsed.TotalSeconds);
        }

        private async Task<WhisperResponse> TranscribeFileAsync(string path, CancellationToken cancellationToken)
        {
            await using var stream = File.OpenRead(path);
            using var form = new MultipartFormDataContent();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(fileContent, "file", Path.GetFileName(path));
            form.Add(new StringContent("verbose_json"), "response_format");
            form.Add(new StringContent(_options.Language), "language");

            if (!string.IsNullOrWhiteSpace(_options.Prompt))
                form.Add(new StringContent(_options.Prompt), "prompt");

            using var request = new HttpRequestMessage(HttpMethod.Post, "transcribe") { Content = form };
            request.Headers.Add("X-API-Key", _options.ApiKey);
            request.Headers.Add("ngrok-skip-browser-warning", "true");

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Hosted Whisper returned HTTP {StatusCode}: {Error}", (int)response.StatusCode, error.Length > 2000 ? error[..2000] : error);
                throw new HttpRequestException(ErrorMessages.TranscriptionServerRequestFailed, null, response.StatusCode);
            }

            var result = await response.Content.ReadFromJsonAsync<WhisperResponse>(cancellationToken: cancellationToken);
            if (result?.Segments is null)
                throw new InvalidOperationException(ErrorMessages.InvalidTranscriptionResponse);

            if (response.Headers.TryGetValues("X-Inference-Seconds", out var values) &&
                double.TryParse(values.FirstOrDefault(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                result.InferenceSeconds = seconds;

            return result;
        }

        private static int ToMilliseconds(double seconds)
        {
            if (!double.IsFinite(seconds) || seconds <= 0)
                return 0;

            var milliseconds = seconds * 1000d;
            return milliseconds >= int.MaxValue ? int.MaxValue : (int)Math.Round(milliseconds);
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private sealed class WhisperResponse
        {
            [JsonPropertyName("language")]
            public string? Language { get; set; }

            [JsonPropertyName("duration")]
            public double? Duration { get; set; }

            [JsonPropertyName("text")]
            public string? Text { get; set; }

            [JsonPropertyName("segments")]
            public List<WhisperSegment> Segments { get; set; } = [];

            [JsonIgnore]
            public double? InferenceSeconds { get; set; }
        }

        private sealed class WhisperSegment
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }

            [JsonPropertyName("start")]
            public double Start { get; set; }

            [JsonPropertyName("end")]
            public double End { get; set; }
        }
    }
}
