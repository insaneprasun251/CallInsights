using Core.BusinessObject;
using Core.Constants;
using Core.Enums;
using Core.Helpers;
using Core.Options;
using Core.Services;
using FFMpegCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ServiceLayer.Pipeline
{
    /// <summary>
    /// Converts recordings to 16 kHz mono PCM WAV files.
    /// Different stereo channels are separated into agent.wav and customer.wav.
    /// Mono recordings and identical stereo channels produce mono.wav.
    /// No audio enhancement filters are applied by default.
    /// </summary>
    public class PreprocessStep : IPipelineStep
    {
        // Channels with a difference quieter than this are treated as identical.
        private const double IdenticalChannelsThresholdDb = -60;
        private static readonly Regex MaxVolumePattern = new(@"max_volume:\s*(?<value>-inf|-?\d+(?:\.\d+)?)\s*dB", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private readonly AudioOptions _options;
        private readonly ILogger<PreprocessStep> _logger;

        public PreprocessStep(IOptions<AudioOptions> options, ILogger<PreprocessStep> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public CallStatus Status => CallStatus.Preprocessing;
        public string Name => ProcessingSteps.Preprocessing;
        public async Task ExecuteAsync(Call call, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            EnsureSettingsAndTools();
            GlobalFFOptions.Configure(new FFOptions
            {
                BinaryFolder = _options.FfmpegFolder
            });

            if (!File.Exists(call.FilePath))
            {
                throw new FileNotFoundException(ErrorMessages.RecordingNotFound);
            }

            var analysis = await FFProbe.AnalyseAsync(call.FilePath);
            cancellationToken.ThrowIfCancellationRequested();
            var audio = analysis.PrimaryAudioStream ?? throw new InvalidOperationException(ErrorMessages.NoAudioStream);

            call.DurationSeconds = analysis.Duration.TotalSeconds;
            call.ChannelCount = audio.Channels;

            _logger.LogInformation("Preprocessing call {CallId}: {Channels} channel(s), {Seconds:F1}s", call.Id, audio.Channels, call.DurationSeconds);

            // Remove files from previous attempts.
            WorkFolderHelper.DeleteCallFolder(_options.WorkFolder, call.Id);
            var folder = WorkFolderHelper.GetCallFolder(_options.WorkFolder, call.Id);
            Directory.CreateDirectory(folder);
            var separateChannels = audio.Channels == 2 && !await ChannelsAreIdenticalAsync(call.FilePath, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (separateChannels)
            {
                var agentChannel = Math.Clamp(_options.AgentChannelIndex, 0, 1);
                var agentPath = Path.Combine(folder, WorkFileNames.Agent);
                var customerPath = Path.Combine(folder, WorkFileNames.Customer);

                _logger.LogInformation("Separating stereo channels for call {CallId}", call.Id);

                await ConvertAsync(call.FilePath, agentPath, $"pan=mono|c0=c{agentChannel}", cancellationToken);
                await ConvertAsync(call.FilePath, customerPath, $"pan=mono|c0=c{1 - agentChannel}", cancellationToken);
            }
            else
            {
                var monoPath = Path.Combine(folder, WorkFileNames.Mono);

                _logger.LogInformation("Producing mono audio for call {CallId}", call.Id);

                await ConvertAsync(call.FilePath, monoPath, null, cancellationToken);
            }
        }

        private void EnsureSettingsAndTools()
        {
            if (string.IsNullOrWhiteSpace(_options.FfmpegFolder))
            {
                throw new InvalidOperationException(ErrorMessages.SettingMissing("Audio:FfmpegFolder"));
            }

            if (string.IsNullOrWhiteSpace(_options.WorkFolder))
            {
                throw new InvalidOperationException(ErrorMessages.SettingMissing("Audio:WorkFolder"));
            }

            if (_options.SampleRate <= 0)
            {
                throw new InvalidOperationException("Audio:SampleRate must be greater than zero.");
            }

            var extension = OperatingSystem.IsWindows()
                ? ".exe"
                : string.Empty;

            foreach (var tool in new[] { "ffmpeg", "ffprobe" })
            {
                var path = Path.Combine(_options.FfmpegFolder, tool + extension);

                if (!File.Exists(path))
                {
                    throw new InvalidOperationException(ErrorMessages.FfmpegNotFound(path));
                }
            }
        }

        private async Task ConvertAsync(string input, string output, string? channelFilter, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var watch = Stopwatch.StartNew();

            // Apply a channel filter only when separating stereo channels.
            // Do not apply loudnorm or high-pass by default.
            var arguments = $"-vn -ar {_options.SampleRate} -ac 1 -c:a pcm_s16le";

            if (!string.IsNullOrWhiteSpace(channelFilter))
            {
                arguments = $"-vn -af \"{channelFilter}\" " + $"-ar {_options.SampleRate} -ac 1 -c:a pcm_s16le";
            }

            _logger.LogInformation("Converting {Input} to {Output}", Path.GetFileName(input), Path.GetFileName(output));

            await FFMpegArguments
                .FromFileInput(input)
                .OutputToFile(
                    output,
                    overwrite: true,
                    options => options.WithCustomArgument(arguments))
                .CancellableThrough(cancellationToken)
                .ProcessAsynchronously();

            watch.Stop();

            _logger.LogInformation("Converted {Output} in {Seconds:F2}s", Path.GetFileName(output), watch.Elapsed.TotalSeconds);
        }

        private async Task<bool> ChannelsAreIdenticalAsync(string input, CancellationToken cancellationToken)
        {
            try
            {
                var report = new StringBuilder();

                await FFMpegArguments
                    .FromFileInput(input)
                    .OutputToFile(
                        "-",
                        overwrite: true,
                        options => options.WithCustomArgument("-vn -af \"pan=mono|c0=c0-c1,volumedetect\" -f null"))
                    .NotifyOnError(line => report.AppendLine(line))
                    .CancellableThrough(cancellationToken)
                    .ProcessAsynchronously(throwOnError: false);

                cancellationToken.ThrowIfCancellationRequested();

                var match = MaxVolumePattern.Match(report.ToString());

                // If the measurement is unavailable, preserve both channels.
                if (!match.Success)
                {
                    return false;
                }

                var value = match.Groups["value"].Value;

                if (value.Equals("-inf", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return double.Parse(value, CultureInfo.InvariantCulture) <= IdenticalChannelsThresholdDb;
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not determine whether stereo channels are identical. " + "Preserving separate channels for {Input}", Path.GetFileName(input));
                return false;
            }
        }
    }
}
